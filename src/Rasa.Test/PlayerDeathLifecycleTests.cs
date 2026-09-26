using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class PlayerDeathLifecycleTests
    {
        private MapChannel _map;
        private Client _owner;
        private Client _observer;
        private Actor _source;
        private PlayerDeathManager _deaths;
        private readonly List<(CharacterUpdate Update, object Value)> _persisted = new();

        private void Arrange(uint mapContextId)
        {
            _map = new MapChannel { MapInfo = new MapInfo(mapContextId, "test", 1, 0), ClientList = new List<Client>() };
            _owner = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            _observer = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            _owner.Player.Id = 101;
            _owner.Player.State = CharacterState.Normal;
            _owner.Player.MapChannel = _map;
            _owner.Player.MapContextId = mapContextId;
            _owner.Player.Cells = new uint[,] { { 0, 1 } };
            _owner.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 40, 40, 0, 0, 0);
            _owner.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 10, 0, 0);
            _map.ClientList.Add(_owner);
            _map.ClientList.Add(_observer);
            _map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { _owner } };
            _map.MapCellInfo.Cells[1] = new MapCell { ClientList = new List<Client> { _observer } };
            _source = new Actor { Cells = new uint[,] { { 0, 1 } } };
            EntityManager.Instance.RegisterEntity(_owner.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(_owner.Player.EntityId, _owner.Player);
            EntityManager.Instance.RegisterActor(_owner.Player.EntityId, _owner.Player);
            _persisted.Clear();
            _deaths = new PlayerDeathManager((client, update, value) => _persisted.Add((update, value)));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_owner == null)
                return;
            EntityManager.Instance.UnregisterEntity(_owner.Player.EntityId);
            EntityManager.Instance.UnregisterPlayer(_owner.Player.EntityId);
            EntityManager.Instance.UnregisterActor(_owner.Player.EntityId);
        }

        [TestMethod]
        public void LethalHitKillsOnceAndOffersTheBootCampHospitalAfterTheKillingRecovery()
        {
            Arrange(1985);
            var killingShot = Shot(20);
            var laterShot = Shot(20);
            _map.QueuedMissiles.Add(killingShot);
            _map.QueuedMissiles.Add(laterShot);
            MissileManager.Instance.DoWork(_map, 0);

            Assert.AreEqual(CharacterState.Dead, _owner.Player.State);
            Assert.AreEqual(0, _owner.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1, killingShot.Args.HitData.Single().DeathBlow);
            Assert.AreEqual(0, laterShot.Args.HitData.Count);
            Assert.IsFalse(WeaponActionManager.CanAct(_owner));

            var owner = Drain(_owner);
            var recovery = owner.FindIndex(message => message.MethodId == GameOpcode.PerformRecovery);
            var killed = owner.FindIndex(message => message.MethodId == GameOpcode.ActorKilled);
            var dead = owner.FindIndex(message => message.MethodId == GameOpcode.PlayerDead);
            Assert.IsTrue(recovery >= 0 && recovery < killed && killed < dead);
            Assert.AreEqual(1, owner.Count(message => message.MethodId == GameOpcode.PlayerDead));
            Assert.AreEqual(_owner.Player.EntityId, owner[dead].EntityId);

            var observer = Drain(_observer);
            Assert.AreEqual(1, observer.Count(message => message.MethodId == GameOpcode.ActorKilled));
            Assert.IsFalse(observer.Any(message => message.MethodId == GameOpcode.PlayerDead));

            Assert.AreEqual(_source.EntityId, _owner.Player.DeathOffer.SourceId);
            CollectionAssert.AreEqual(new[] { 20000001 }, _owner.Player.DeathOffer.Hospitals.Select(h => h.GraveyardId).ToArray());
        }

        [TestMethod]
        public void WildernessOffersOnlyHospitalsTheCharacterHasGained()
        {
            Arrange(1220);
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 103, (byte)WaypointType.Hospital));
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 105, (byte)WaypointType.Waypoint));
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 216, (byte)WaypointType.Hospital));

            CollectionAssert.AreEquivalent(new[] { 3, 136 },
                PlayerDeathManager.OfferedHospitals(_owner.Player).Select(hospital => hospital.GraveyardId).ToArray());
        }

        [TestMethod]
        public void RespawnMovesToTheChosenHospitalRestoresHealthAndArmorAndCannotRepeat()
        {
            Arrange(1985);
            Kill();
            var hospital = HospitalCatalog.Entries.Single(entry => entry.GraveyardId == 20000001);

            _deaths.ReviveMe(_owner, Revive(20000001));

            Assert.AreEqual(CharacterState.Normal, _owner.Player.State);
            Assert.AreEqual(hospital.Position, _owner.Player.Position);
            Assert.AreEqual(100, _owner.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(40, _owner.Player.Attributes[Attributes.Armor].Current);
            Assert.IsNull(_owner.Player.DeathOffer);
            Assert.AreEqual(1, _persisted.Count(entry => entry.Update == CharacterUpdate.Position));

            // The body travels on the movement channel, not on CallMethod: the client's Recv_Teleport only
            // blocks movement and runs the fade. The owner has to be told where they now are, or they stand at
            // the place they died while everyone else sees them at the hospital (live report 2026-09-17).
            var ownerMoves = DrainMoves(_owner);
            Assert.AreEqual(1, ownerMoves.Count, "the respawning player is told their own new position");
            Assert.AreEqual(hospital.Position, ownerMoves[0].Movement.Position);
            Assert.AreEqual(_owner.Player.EntityId, ownerMoves[0].EntityId);
            Assert.AreEqual(1, DrainMoves(_observer).Count, "and so is everyone who can see them");

            var owner = Drain(_owner);
            var begin = owner.FindIndex(message => message.MethodId == GameOpcode.BeginTeleport);
            var teleport = owner.FindIndex(message => message.MethodId == GameOpcode.Teleport);
            var revived = owner.FindIndex(message => message.MethodId == GameOpcode.Revived);
            Assert.IsTrue(begin >= 0 && begin < teleport && teleport < revived);
            var health = (UpdateHealthPacket)owner.Last(message => message.MethodId == GameOpcode.UpdateHealth).Packet;
            Assert.AreEqual(100, health.Health.Current);
            Assert.IsTrue(Drain(_observer).Any(message => message.MethodId == GameOpcode.Revived));

            _deaths.ReviveMe(_owner, Revive(20000001));
            _deaths.ReviveMe(_owner, Revive(null));
            Assert.AreEqual(1, _persisted.Count);
            Assert.IsFalse(Drain(_owner).Any(message => message.MethodId == GameOpcode.Revived));
        }

        [TestMethod]
        public void UnofferedHospitalKeepsThePlayerDeadAndNoneTakesTheNearestOffered()
        {
            Arrange(1220);
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 103, (byte)WaypointType.Hospital));
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 106, (byte)WaypointType.Hospital));
            var ranja = HospitalCatalog.Entries.Single(entry => entry.GraveyardId == 5);
            _owner.Player.Position = ranja.Position + new Vector3(30, 0, 30);
            Kill();

            _deaths.ReviveMe(_owner, Revive(20000001)); // another map's hospital
            _deaths.ReviveMe(_owner, Revive(6));        // on this map, never gained
            Assert.AreEqual(CharacterState.Dead, _owner.Player.State);
            Assert.IsNotNull(_owner.Player.DeathOffer);
            Assert.AreEqual(0, _persisted.Count);

            _deaths.ReviveMe(_owner, Revive(null));
            Assert.AreEqual(CharacterState.Normal, _owner.Player.State);
            Assert.AreEqual(ranja.Position, _owner.Player.Position);
        }

        [TestMethod]
        public void RevivalRequiresADeathOfferAndAnInGameClient()
        {
            Arrange(1985);
            _deaths.ReviveMe(_owner, Revive(20000001)); // alive
            _owner.Player.State = CharacterState.Dead;  // dead without an offer
            _deaths.ReviveMe(_owner, Revive(20000001));
            Assert.AreEqual(CharacterState.Dead, _owner.Player.State);
            Kill();
            _owner.State = ClientState.Loading;
            _deaths.ReviveMe(_owner, Revive(20000001));
            Assert.AreEqual(CharacterState.Dead, _owner.Player.State);
            Assert.AreEqual(0, _persisted.Count);
        }

        [TestMethod]
        public void HospitalsAreGainedOnceWithinTheDiscoveryRadiusAndNeverWhileDead()
        {
            Arrange(1220);
            var aliaDas = HospitalCatalog.Entries.Single(entry => entry.GraveyardId == 3);

            _owner.Player.Position = aliaDas.Position + new Vector3(101, 0, 0);
            _deaths.DiscoverHospitals(_map);
            Assert.AreEqual(0, _persisted.Count);

            _owner.Player.State = CharacterState.Dead;
            _owner.Player.Position = aliaDas.Position + new Vector3(60, 0, 79);
            _deaths.DiscoverHospitals(_map);
            Assert.AreEqual(0, _persisted.Count);

            _owner.Player.State = CharacterState.Normal;
            _deaths.DiscoverHospitals(_map);
            _deaths.DiscoverHospitals(_map);

            var gained = (CharacterTeleporterEntry)_persisted.Single(entry => entry.Update == CharacterUpdate.Teleporter).Value;
            Assert.AreEqual(103u, gained.WaypointId);
            Assert.AreEqual((byte)WaypointType.Hospital, gained.WaypointType);
            var message = Drain(_owner).Single(entry => entry.MethodId == GameOpcode.GraveyardGained);
            Assert.AreEqual(_owner.Player.EntityId, message.EntityId);
            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(Serialize(message.Packet)))))
            {
                Assert.AreEqual(1, reader.ReadTuple());
                Assert.AreEqual(103u, reader.ReadUInt());
            }
            CollectionAssert.AreEquivalent(new[] { 3 }, PlayerDeathManager.OfferedHospitals(_owner.Player).Select(h => h.GraveyardId).ToArray());
        }

        [TestMethod]
        public void TheBootCampHospitalIsKnownWithoutAGainMessage()
        {
            Arrange(1985);
            _owner.Player.Position = HospitalCatalog.Entries.Single(entry => entry.GraveyardId == 20000001).Position;
            _deaths.DiscoverHospitals(_map);
            Assert.AreEqual(0, _persisted.Count);
            Assert.IsFalse(Drain(_owner).Any(message => message.MethodId == GameOpcode.GraveyardGained));
        }

        [TestMethod]
        public void CatalogMatchesItsEvidenceRecord()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("hospital-catalog.json")));
            var root = document.RootElement;
            Assert.AreEqual((double)HospitalCatalog.DiscoveryRadius, root.GetProperty("discovery_radius_m").GetProperty("value").GetDouble());

            var records = root.GetProperty("hospitals").EnumerateArray().ToList();
            Assert.AreEqual(HospitalCatalog.Entries.Count, records.Count);
            var tiers = new HashSet<string> { "original", "observed", "measured", "inferred", "analogue" };

            foreach (var record in records)
            {
                int Int(string field) => record.GetProperty(field).GetProperty("value").GetInt32();
                bool Bool(string field) => record.GetProperty(field).GetProperty("value").GetBoolean();

                // A graveyard id is unique per map, not across the world: the five instance entrances carry
                // graveyard 36, and ReviveMe only ever chooses from the hospitals of the map the player died on.
                var hospital = HospitalCatalog.Entries.Single(entry => entry.GraveyardId == Int("graveyard_id") &&
                    entry.MapContextId == (uint)Int("map_context_id"));
                Assert.AreEqual((uint)Int("waypoint_id"), hospital.WaypointId);
                Assert.AreEqual(record.GetProperty("marker_entity_id").GetProperty("value").GetUInt64(), hospital.MarkerEntityId);
                var position = record.GetProperty("position").GetProperty("value").EnumerateArray().Select(value => (float)value.GetDouble()).ToArray();
                Assert.AreEqual(new Vector3(position[0], position[1], position[2]), hospital.Position);
                Assert.AreEqual(Bool("is_safe"), hospital.IsSafe);
                Assert.AreEqual(Bool("is_control_point"), hospital.IsControlPoint);
                Assert.AreEqual(Bool("known_without_discovery"), hospital.KnownWithoutDiscovery);

                foreach (var field in record.EnumerateObject().Where(property => property.Value.ValueKind == JsonValueKind.Object))
                {
                    Assert.IsTrue(tiers.Contains(field.Value.GetProperty("tier").GetString()), $"{field.Name} of {hospital.GraveyardId} has no valid tier");
                    Assert.IsTrue(field.Value.GetProperty("citations").GetArrayLength() > 0, $"{field.Name} of {hospital.GraveyardId} has no citation");
                }
            }
        }

        [TestMethod]
        public void TraumaFollowsAnNpcDeathFromLevelFiveAndStacksToSixtyPercentAndSixMinutes()
        {
            Arrange(1985);
            _owner.Player.Level = 5;
            _owner.Player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 1000, 1000, 800, 0, 0);
            var refreshed = 0;
            _deaths = new PlayerDeathManager((client, update, value) => _persisted.Add((update, value)), _ => refreshed++);

            var expected = new[] { (1, 120000), (2, 240000), (3, 360000), (3, 360000) };
            foreach (var (stacks, duration) in expected)
            {
                Kill();
                _deaths.ReviveMe(_owner, Revive(null));
                var trauma = _owner.Player.ActiveEffects.Values.Single(effect => effect.TypeId == DeathPenaltyRules.RezSicknessEffectType);
                var noHeal = _owner.Player.ActiveEffects.Values.Single(effect => effect.TypeId == DeathPenaltyRules.RezSicknessNoHealEffectType);
                Assert.AreEqual(stacks, _owner.Player.TraumaStacks);
                Assert.AreEqual((uint)stacks, trauma.EffectLevel);
                Assert.AreEqual(duration, trauma.Duration);
                Assert.AreEqual(DeathPenaltyRules.NoHealDurationMs, noHeal.Duration);
                Assert.AreEqual(0, _owner.Player.Attributes[Attributes.Chi].Current);
            }
            Assert.AreEqual(4, refreshed);

            Assert.AreEqual(0, ActorManager.Instance.Heal(_owner.Player, 5));
            var detached = Drain(_owner).Count(message => message.MethodId == GameOpcode.GameEffectDetached);
            Assert.AreEqual(2, detached); // the last revival replaced the trauma and no-heal effects (Kill drains earlier ones)

            _deaths.OnTraumaEnded(_map, _owner.Player);
            Assert.AreEqual(0, _owner.Player.TraumaStacks);
            Assert.AreEqual(5, refreshed);
        }

        [TestMethod]
        public void NoTraumaBelowLevelFiveOrAfterAPlayerKill()
        {
            Arrange(1985);
            _owner.Player.Level = 4;
            Kill();
            _deaths.ReviveMe(_owner, Revive(null));
            Assert.AreEqual(0, _owner.Player.TraumaStacks);

            _owner.Player.Level = 10;
            _source = new Manifestation { Cells = new uint[,] { { 0, 1 } } };
            Kill();
            Assert.IsFalse(_owner.Player.DeathOffer.PenaltyApplies);
            _deaths.ReviveMe(_owner, Revive(null));
            Assert.AreEqual(0, _owner.Player.TraumaStacks);
            Assert.IsFalse(_owner.Player.ActiveEffects.Any());
        }

        [TestMethod]
        public void TraumaLowersTheThreeAttributesAndExpiryRestoresThem()
        {
            Arrange(1985);
            var player = _owner.Player;
            player.Level = 10;
            player.Race = Race.Human;
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                if (!player.Attributes.ContainsKey(attribute))
                    player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
            player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            ManifestationManager.Instance.UpdateStatsValues(_owner, true);
            var body = player.Attributes[Attributes.Body].CurrentMax;
            var mind = player.Attributes[Attributes.Mind].CurrentMax;

            player.TraumaStacks = 2;
            ManifestationManager.Instance.UpdateStatsValues(_owner, false);
            Assert.AreEqual(body - DeathPenaltyRules.AttributePenalty(body, 2), player.Attributes[Attributes.Body].CurrentMax);
            Assert.AreEqual(mind - DeathPenaltyRules.AttributePenalty(mind, 2), player.Attributes[Attributes.Mind].CurrentMax);

            // Expiry through the effect lifecycle ends the trauma on the server singleton.
            GameEffectManager.Instance.AttachTimedDebuff(_map, player, DeathPenaltyRules.RezSicknessEffectType, 2, 1000);
            _map.ClientList.Remove(_observer);
            GameEffectManager.Instance.DoWork(_map, 1000);
            Assert.AreEqual(0, player.TraumaStacks);
            Assert.AreEqual(body, player.Attributes[Attributes.Body].CurrentMax);
        }

        [DataTestMethod]
        [DataRow(50, 1, 10)]
        [DataRow(49, 1, 9)]
        [DataRow(50, 3, 30)]
        [DataRow(50, 4, 30)]
        public void TraumaPenaltyIsTwentyPercentAStackUpToThree(int total, int stacks, int penalty)
            => Assert.AreEqual(penalty, DeathPenaltyRules.AttributePenalty(total, stacks));

        [TestMethod]
        public void EnteringTheWildernessGainsTheNearestHospitalOnce()
        {
            Arrange(1220);
            _owner.Player.Position = new Vector3(884.11f, 294f, 347.81f); // footage arrival at Alia Das (B2-015)
            _deaths.OnPlayerEnteredMap(_owner);
            _deaths.OnPlayerEnteredMap(_owner);
            var gained = _persisted.Select(entry => entry.Value).OfType<CharacterTeleporterEntry>().Single();
            Assert.AreEqual(103u, gained.WaypointId);

            Arrange(1220);
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 106, (byte)WaypointType.Hospital));
            _owner.Player.Position = new Vector3(884.11f, 294f, 347.81f);
            _deaths.OnPlayerEnteredMap(_owner);
            Assert.AreEqual(0, _persisted.Count);
        }

        /// <summary>
        /// SEG3-HOSPITALS: the Palisades control-point hospital carried the Wilderness Landing Zone's waypoint 216, and
        /// gained waypoints are keyed by id alone, so gaining the Wilderness one gained both and skipped the Palisades
        /// zone-entry gain. The Palisades one is Fort Dew's (graveyard 221, waypoint 226).
        /// </summary>
        [TestMethod]
        public void TheWildernessLandingZoneHospitalNoLongerCountsOnThePalisades()
        {
            var wilderness = HospitalCatalog.ForMap(1220).Single(entry => entry.MarkerEntityId == 133079561962699UL);
            var fortDew = HospitalCatalog.ForMap(1244).Single(entry => entry.MarkerEntityId == 133182640964490UL);
            Assert.AreEqual(216u, wilderness.WaypointId);
            Assert.AreEqual((221, 226u), (fortDew.GraveyardId, fortDew.WaypointId));

            Arrange(1244);
            _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, 216, (byte)WaypointType.Hospital));
            Assert.AreEqual(0, PlayerDeathManager.OfferedHospitals(_owner.Player).Count);
            _owner.Player.Position = fortDew.Position + new Vector3(20f, 0f, 20f);
            _deaths.OnPlayerEnteredMap(_owner);
            var gained = _persisted.Select(entry => entry.Value).OfType<CharacterTeleporterEntry>().Single();
            Assert.AreEqual(226u, gained.WaypointId);
            CollectionAssert.AreEqual(new[] { 221 }, PlayerDeathManager.OfferedHospitals(_owner.Player).Select(h => h.GraveyardId).ToArray());
        }

        /// <summary>
        /// A hospital's waypoint id is what the character keeps, so one id on two maps is one hospital to the server.
        /// The only sharing left is the Manhattan shared-instance copy (2375 of 2327, the same markers) and the
        /// instance entrances' "AFS Field Medic" 120 (GAP-HOSPITAL-SHARED-WAYPOINT).
        /// </summary>
        [TestMethod]
        public void NoHospitalWaypointIdIsSharedBetweenMapsBeyondTheRecordedCases()
        {
            var shared = HospitalCatalog.Entries.GroupBy(entry => entry.WaypointId)
                .Where(group => group.Select(entry => entry.MapContextId).Distinct().Count() > 1)
                .ToDictionary(group => group.Key, group => group.Select(entry => entry.MapContextId).OrderBy(id => id).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 120, 517, 518, 519 }, shared.Keys.ToArray());
            CollectionAssert.AreEqual(new uint[] { 1823, 1977, 1988, 2055, 2111 }, shared[120]);
            foreach (var id in new uint[] { 517, 518, 519 })
                CollectionAssert.AreEqual(new uint[] { 2327, 2375 }, shared[id]);
        }

        /// <summary>
        /// The Divide's Foreas Base, the Palisades' Cumbria Research Facility and Devil's Den hospitals are offered
        /// once gained: gained within the discovery radius as every catalogued hospital is.
        /// </summary>
        [DataTestMethod]
        [DataRow(1148u, 202, 93u)]
        [DataRow(1244u, 219, 112u)]
        [DataRow(1394u, 41, 388u)]
        public void TheNewlyResolvedHospitalsAreGainedAndOffered(uint map, int graveyardId, uint waypointId)
        {
            Arrange(map);
            var hospital = HospitalCatalog.ForMap(map).Single(entry => entry.GraveyardId == graveyardId);
            Assert.AreEqual(waypointId, hospital.WaypointId);
            _owner.Player.Position = hospital.Position + new Vector3(3f, 0f, 3f);
            _deaths.DiscoverHospitals(_map);
            Assert.IsTrue(_persisted.Select(entry => entry.Value).OfType<CharacterTeleporterEntry>().Any(entry => entry.WaypointId == waypointId));
            Assert.IsTrue(PlayerDeathManager.OfferedHospitals(_owner.Player).Any(entry => entry.GraveyardId == graveyardId));
        }

        /// <summary>
        /// 2026-09-27 (instance travel): instance and battlefield hospitals the maps revived their dead in place without
        /// are gained within the discovery radius and offered, one row per map (docs/evidence/hospital-catalog.json).
        /// </summary>
        [DataTestMethod]
        [DataRow(1349u, 43, 119u)]      // Torcastra Prison AFS Medical Officer
        [DataRow(1384u, 48, 130u)]      // Warnet Caverns Entrance
        [DataRow(1384u, 49, 131u)]      // Warnet Caverns Research Area
        [DataRow(1429u, 232, 579u)]     // Turpis Forward Medical Unit (emulator waypoint id)
        [DataRow(1694u, 114, 586u)]     // Retread Medical Area
        [DataRow(1763u, 153, 351u)]     // Live Target Pens Entrance
        [DataRow(1763u, 154, 591u)]     // Live Target Pens Guard Station
        [DataRow(1773u, 125, 383u)]     // Kardash Atta Colony
        [DataRow(2029u, 86, 387u)]      // Temporal Chamber
        [DataRow(2034u, 87, 382u)]      // Phanin Research Facility
        [DataRow(2138u, 130, 271u)]     // Staal Junkyard
        [DataRow(2162u, 134, 273u)]     // AFS Rat Hole
        [DataRow(2162u, 135, 274u)]     // Cuthah Base Entrance
        [DataRow(2368u, 281, 529u)]     // Epic Caves of Donn Entrance
        [DataRow(2374u, 273, 524u)]     // Edmund Range West Control Point
        [DataRow(2374u, 276, 523u)]     // Edmund Range Blue Base
        [DataRow(20000009u, 243, 480u)] // CELLAR Arena Medic
        public void TheInstanceHospitalsAreGainedAndOffered(uint map, int graveyardId, uint waypointId)
        {
            Arrange(map);
            var hospital = HospitalCatalog.ForMap(map).Single(entry => entry.GraveyardId == graveyardId);
            Assert.AreEqual(waypointId, hospital.WaypointId);
            _owner.Player.Position = hospital.Position + new Vector3(3f, 0f, 3f);
            _deaths.DiscoverHospitals(_map);
            Assert.IsTrue(_persisted.Select(entry => entry.Value).OfType<CharacterTeleporterEntry>().Any(entry => entry.WaypointId == waypointId));
            Assert.IsTrue(PlayerDeathManager.OfferedHospitals(_owner.Player).Any(entry => entry.GraveyardId == graveyardId));
        }

        /// <summary>
        /// D10.5: "Players will now only be able to access the Hospital point for the section of Eloh Temples they are currently
        /// in." With all three temple hospitals gained, a player who dies in the Proud Patriarch's temple is offered its
        /// hospital alone; one who has not gained the current section's hospital is offered none, not another temple's.
        /// </summary>
        [TestMethod]
        public void ElohTemplesOffersOnlyTheCurrentSectionsHospital()
        {
            Arrange(1803);
            var temples = HospitalCatalog.ForMap(1803).ToList();
            CollectionAssert.AreEquivalent(new[] { 80, 81, 82 }, temples.Select(entry => entry.GraveyardId).ToArray());
            foreach (var temple in temples)
                _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, temple.WaypointId, (byte)WaypointType.Hospital));

            var proud = temples.Single(entry => entry.GraveyardId == 80);
            _owner.Player.Position = proud.Position + new Vector3(40f, 0f, -30f);
            CollectionAssert.AreEqual(new[] { 80 }, PlayerDeathManager.OfferedHospitals(_owner.Player).Select(h => h.GraveyardId).ToArray());

            var bowed = temples.Single(entry => entry.GraveyardId == 82);
            _owner.Player.Position = bowed.Position + new Vector3(-20f, 0f, 20f);
            CollectionAssert.AreEqual(new[] { 82 }, PlayerDeathManager.OfferedHospitals(_owner.Player).Select(h => h.GraveyardId).ToArray());

            _owner.Player.GainedWaypoints.RemoveAll(entry => entry.WaypointId == bowed.WaypointId);
            Assert.AreEqual(0, PlayerDeathManager.OfferedHospitals(_owner.Player).Count);

            // Elsewhere every gained hospital is still offered.
            Arrange(2374);
            foreach (var hospital in HospitalCatalog.ForMap(2374))
                _owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(101, hospital.WaypointId, (byte)WaypointType.Hospital));
            Assert.AreEqual(4, PlayerDeathManager.OfferedHospitals(_owner.Player).Count);
        }

        private Missile Shot(int damage) => new Missile
        {
            Source = _source, ActionId = ActionId.WeaponAttack, ActionArgId = 1,
            TargetEntityId = _owner.Player.EntityId, TargetActor = _owner.Player, DamageA = damage
        };

        private void Kill()
        {
            _map.QueuedMissiles.Add(Shot(1000));
            MissileManager.Instance.DoWork(_map, 0);
            Assert.AreEqual(CharacterState.Dead, _owner.Player.State);
            Drain(_owner);
            Drain(_observer);
        }

        private static ReviveMePacket Revive(int? graveyardId)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                if (graveyardId is int id)
                    writer.WriteInt(id);
                else
                    writer.WriteNoneStruct();
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new ReviveMePacket();
            packet.Read(reader);
            return packet;
        }

        private static byte[] Serialize(PythonPacket packet)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            return stream.ToArray();
        }

        private static List<CallMethodMessage> Drain(Client client) => DrainQueue<CallMethodMessage>(client);

        /// <summary>The movement channel, which Drain used to throw away with everything that was not a CallMethod.</summary>
        private static List<MoveObjectMessage> DrainMoves(Client client) => DrainQueue<MoveObjectMessage>(client);

        private static List<T> DrainQueue<T>(Client client) where T : class
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<T>();
            var kept = new List<ProtocolPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is T wanted)
                    messages.Add(wanted);
                else
                    kept.Add(protocol);

            foreach (var protocol in kept)
                queue.EnqueueOutgoing(protocol);

            return messages;
        }
    }
}

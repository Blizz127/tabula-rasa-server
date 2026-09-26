using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Content placement behaviours: a stationary placement never looks for enemies, a creature-AI placement
    /// guards its spot - it engages an enemy in aggro range and walks back to its post, but never strolls.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlacedCreatureBehaviorTests
    {
        private static readonly Vector3 Post = new(100f, 0f, 100f);
        private MapChannel _map;
        private uint _seed;

        [TestInitialize]
        public void Initialize()
        {
            _map = new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 783, 4) };
            _seed = CellManager.Instance.GetCellSeed(Post);
            CellManager.Instance.CreateCellMatrix(_map, _seed & 0xFFFF, _seed >> 16);
        }

        private Creature Place(ContentPlacementBehavior behavior, Vector3 position)
        {
            var creature = new Creature { Faction = Factions.Bane, WalkSpeed = 2f, RunSpeed = 4f, State = CharacterState.Normal, Cells = Cells(), MapChannel = _map };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            CreatureManager.Instance.SetLocation(creature, Post, 0, 1985);
            CreatureManager.ApplyPlacementBehavior(creature, new ContentPlacementEntry { Behavior = (byte)behavior });
            creature.Position = position;
            _map.MapCellInfo.Cells[_seed].CreatureList.Add(creature);
            return creature;
        }

        private uint[,] Cells()
        {
            var cells = new uint[5, 5];
            for (var x = 0; x < 5; x++)
                for (var z = 0; z < 5; z++)
                    cells[x, z] = _seed;
            return cells;
        }

        private void Think(int seconds)
        {
            for (var tick = 0; tick < seconds * 4; tick++)
                BehaviorManager.Instance.MapChannelThink(_map, 250);
        }

        [TestMethod]
        public void AGuardWalksBackToItsPostAndNeverStrolls()
        {
            var guard = Place(ContentPlacementBehavior.CreatureAi, Post + new Vector3(8f, 0f, 0f));
            Assert.IsTrue(guard.HoldsPosition);

            Think(10);
            Assert.IsTrue(Vector3.Distance(guard.Position, Post) <= BehaviorManager.HoldPositionSlack, $"{guard.Position}");

            // A spawned creature would have picked a wander destination by now (rest 12-40 s).
            Think(60);
            Assert.IsTrue(Vector3.Distance(guard.Position, Post) <= BehaviorManager.HoldPositionSlack, $"{guard.Position}");
            Assert.AreEqual(BehaviorManager.BehaviorActionWander, guard.Controller.CurrentAction);
        }

        [TestMethod]
        public void OnlyTheGuardEngagesAPlayerInAggroRange()
        {
            var guard = Place(ContentPlacementBehavior.CreatureAi, Post);
            var officer = Place(ContentPlacementBehavior.Stationary, Post + new Vector3(0f, 0f, 1.2f));
            Assert.IsFalse(officer.HoldsPosition);

            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player = new Manifestation { Position = Post + new Vector3(10f, 0f, 0f), MapContextId = 1985, MapChannel = _map };
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(client.Player.EntityId, client.Player);
            _map.MapCellInfo.Cells[_seed].ClientList.Add(client);

            try
            {
                Think(4);

                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guard.Controller.CurrentAction);
                Assert.AreEqual(client.Player.EntityId, guard.Controller.ActionFighting.TargetEntityId);
                Assert.AreEqual(BehaviorManager.BehaviorActionIdle, officer.Controller.CurrentAction);
            }
            finally
            {
                EntityManager.Instance.UnregisterPlayer(client.Player.EntityId);
                EntityManager.Instance.UnregisterEntity(client.Player.EntityId);
            }
        }

        [TestMethod]
        public void EscortCanKeepFightingBesideItsPlayerFarFromItsCampSpawn()
        {
            var courtyard = Post + new Vector3(130f, 0f, 0f);
            var escort = Place(ContentPlacementBehavior.CombatCompanion, courtyard);
            escort.Faction = Factions.AFS;
            escort.IsEscort = true;
            var enemy = Place(ContentPlacementBehavior.Stationary, courtyard + new Vector3(5f, 0f, 0f));

            var player = new Manifestation
            {
                Position = courtyard,
                MapContextId = 1985,
                MapChannel = _map
            };
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterActor(player.EntityId, player);
            EntityManager.Instance.RegisterPlayer(player.EntityId, player);
            EntityManager.Instance.RegisterEntity(enemy.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterActor(enemy.EntityId, enemy);
            EntityManager.Instance.RegisterCreature(enemy);

            try
            {
                BehaviorManager.Instance.SetActionFollow(escort, player.EntityId);
                BehaviorManager.Instance.SetActionFighting(escort, enemy.EntityId);
                Think(1);

                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, escort.Controller.CurrentAction,
                    "A camp-spawned escort should not abandon a courtyard target beside its player.");
                Assert.AreEqual(enemy.EntityId, escort.Controller.ActionFighting.TargetEntityId);

                // The map owner may disconnect mid-fight. Its entity has then been removed;
                // the escort should lose the fight against a target far from camp cleanly.
                EntityManager.Instance.UnregisterActor(player.EntityId);
                BehaviorManager.Instance.MapChannelThink(_map, 250);
                Assert.AreEqual(BehaviorManager.BehaviorActionWander, escort.Controller.CurrentAction);
            }
            finally
            {
                EntityManager.Instance.UnregisterCreature(enemy.EntityId);
                EntityManager.Instance.UnregisterActor(enemy.EntityId);
                EntityManager.Instance.UnregisterEntity(enemy.EntityId);
                EntityManager.Instance.UnregisterPlayer(player.EntityId);
                EntityManager.Instance.UnregisterActor(player.EntityId);
                EntityManager.Instance.UnregisterEntity(player.EntityId);
            }
        }

        [TestMethod]
        public void EscortAssistsAgainstSelectedEnemyButNeverASelectedPlayer()
        {
            var escort = Place(ContentPlacementBehavior.CombatCompanion, Post);
            escort.Faction = Factions.AFS;
            escort.IsEscort = true;
            var enemy = Place(ContentPlacementBehavior.Stationary, Post + new Vector3(25f, 0f, 0f));

            var player = new Manifestation { Position = Post, MapContextId = 1985, MapChannel = _map };
            var friendly = new Manifestation { Position = Post + new Vector3(2f, 0f, 0f), MapContextId = 1985, MapChannel = _map };
            foreach (var actor in new[] { player, friendly })
            {
                actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
                EntityManager.Instance.RegisterEntity(actor.EntityId, EntityType.Character);
                EntityManager.Instance.RegisterActor(actor.EntityId, actor);
                EntityManager.Instance.RegisterPlayer(actor.EntityId, actor);
            }
            EntityManager.Instance.RegisterEntity(enemy.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterActor(enemy.EntityId, enemy);
            EntityManager.Instance.RegisterCreature(enemy);

            try
            {
                BehaviorManager.Instance.SetActionFollow(escort, player.EntityId);
                escort.Controller.ActionFollow.AssistTargetId = player.EntityId;
                player.Target = friendly.EntityId;
                Think(1);
                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction);

                // At 25 m the enemy is outside this creature's 18 m autonomous scan.
                player.Target = enemy.EntityId;
                Think(1);
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, escort.Controller.CurrentAction);
                Assert.AreEqual(enemy.EntityId, escort.Controller.ActionFighting.TargetEntityId);
            }
            finally
            {
                EntityManager.Instance.UnregisterCreature(enemy.EntityId);
                EntityManager.Instance.UnregisterActor(enemy.EntityId);
                EntityManager.Instance.UnregisterEntity(enemy.EntityId);
                foreach (var actor in new[] { player, friendly })
                {
                    EntityManager.Instance.UnregisterPlayer(actor.EntityId);
                    EntityManager.Instance.UnregisterActor(actor.EntityId);
                    EntityManager.Instance.UnregisterEntity(actor.EntityId);
                }
            }
        }
    }
}

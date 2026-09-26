using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// 682 Childhood's End on the shared Wilderness map, from the migrated world: the official live notes 1.6
    /// (2008-03-26, "it no longer matters who kills the Xanx, just that they are killed") and D8 (2008-05-19,
    /// credit "even if they advanced to this objective at the same time as another player") make the Xanx kill
    /// count for everyone with 682/3 active. Other kill bindings keep killer-only credit on shared maps.
    /// </summary>
    public partial class BootcampOpeningTests
    {
        private const uint ChildhoodsEnd = 682;
        private const uint AriochXanx = 77;
        private const uint ProctorFulgor = 76;
        private const uint Wilderness = 1220;

        /// <summary>A character in the world on the Wilderness map, with the given mission rows saved and loaded.</summary>
        private Client WildernessCharacter(uint characterId, string name, uint missionId, params (uint Objective, MissionObjectiveState State)[] objectives)
        {
            var slot = (byte)(characterId - CharacterId + 1);
            using (var context = CharContext(_charConnection))
            {
                if (!context.CharacterEntries.Any(row => row.Id == characterId))
                {
                    context.CharacterEntries.Add(new CharacterEntry
                    {
                        Id = characterId, AccountId = AccountId, Slot = slot, Name = name, Level = 5,
                        MapContextId = Wilderness, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                    });
                    context.SaveChanges();
                }
            }

            if (missionId != 0)
                using (var unit = _factory.CreateChar())
                {
                    unit.CharacterMissions.Add(new CharacterMissionEntry(characterId, missionId, (uint)MissionState.Active),
                        objectives.Select(objective => new CharacterMissionObjectiveEntry(characterId, missionId, objective.Objective, (uint)objective.State)).ToArray());
                    unit.Complete();
                }

            var client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player = new Manifestation { Id = characterId, Level = 5, MapContextId = Wilderness };
            using (var unit = _factory.CreateChar())
                client.Player.Missions = _missions.LoadPlayerMissions(unit.CharacterMissions, AccountId, slot, characterId);
            return client;
        }

        private static MapChannel SharedWilderness(params Client[] clients) => new MapChannel
        {
            MapInfo = new MapInfo(Wilderness, "adv_foreas_concordia_wilderness", 1556, 0),
            ClientList = new List<Client>(clients)
        };

        private MissionObjectiveState? SavedObjective(uint characterId, uint missionId, uint objectiveId)
        {
            using var unit = _factory.CreateChar();
            var row = unit.CharacterMissions.GetObjectives(characterId).SingleOrDefault(entry => entry.MissionId == missionId && entry.ObjectiveId == objectiveId);
            return row == null ? null : (MissionObjectiveState)row.Status;
        }

        [TestMethod]
        public void ChildhoodsEndBindingIsTheSharedKillBinding()
        {
            var binding = _missions.LoadedMissions[ChildhoodsEnd].Bindings.Single(row => row.ObjectiveId == 3);
            Assert.AreEqual((byte)ObjectiveBindingKind.Kill, binding.Kind);
            Assert.AreEqual(AriochXanx, binding.CreatureId);
            Assert.IsTrue(binding.SharedKillCredit, "682/3: the 1.6 and D8 live notes credit the Xanx kill whoever kills it");
            Assert.AreEqual(1, _missions.LoadedMissions.Values.SelectMany(definition => definition.Bindings).Count(row => row.SharedKillCredit),
                "only 682/3 is described by the notes; every other kill binding keeps killer-only credit");
            Assert.IsTrue(_content.Content.LiveBindings.Any(row => row.MissionId == ChildhoodsEnd && row.ObjectiveId == 3 && row.SharedKillCredit),
                string.Join(" | ", _content.Content.Gaps));
        }

        [TestMethod]
        public void XanxKilledByAnotherPlayerCreditsEveryoneOnTheChannelWithTheObjective()
        {
            var killer = WildernessCharacter(CharacterId, "Recruit", ChildhoodsEnd,
                (2, MissionObjectiveState.Completed), (3, MissionObjectiveState.Incomplete));
            var partner = WildernessCharacter(CharacterId + 1, "Partner", ChildhoodsEnd,
                (2, MissionObjectiveState.Completed), (3, MissionObjectiveState.Incomplete));
            // Still speaking to Ranger Anjuhi: the kill is not yet this character's objective.
            var early = WildernessCharacter(CharacterId + 2, "Early", ChildhoodsEnd, (2, MissionObjectiveState.Incomplete));
            var bystander = WildernessCharacter(CharacterId + 3, "Bystander", 0);
            var channel = SharedWilderness(killer, partner, early, bystander);
            var xanx = new Creature { DbId = AriochXanx, MapContextId = Wilderness, State = CharacterState.Dead };

            // CreatureManager.HandleCreatureKill's order for a player kill: the killer first, then the shared credit.
            _missions.OnCreatureKilled(killer, xanx);
            _missions.OnSharedKillCredit(channel, xanx, killer);

            foreach (var (client, id) in new[] { (killer, CharacterId), (partner, CharacterId + 1) })
            {
                Assert.AreEqual(MissionObjectiveState.Completed, client.Player.Missions[ChildhoodsEnd].Objectives[3], $"character {id}");
                // QuestWiringFixes' chain: the kill reveals Ranger Tirna (5) before Anjuhi (4).
                Assert.AreEqual(MissionObjectiveState.Incomplete, client.Player.Missions[ChildhoodsEnd].Objectives[5], $"character {id}");
                Assert.AreEqual(MissionObjectiveState.Completed, SavedObjective(id, ChildhoodsEnd, 3), $"character {id}");
                var packets = Drain(client);
                Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count(), $"character {id} is credited exactly once");
                Assert.AreEqual(1, packets.OfType<ObjectiveRevealedPacket>().Count(), $"character {id}");
            }

            Assert.IsFalse(early.Player.Missions[ChildhoodsEnd].Objectives.ContainsKey(3), "an objective the character has not reached is not credited");
            Assert.AreNotEqual(MissionObjectiveState.Completed, SavedObjective(CharacterId + 2, ChildhoodsEnd, 3));
            Assert.IsFalse(Drain(early).Any(packet => packet is ObjectiveCompletedPacket));
            Assert.IsFalse(bystander.Player.Missions.ContainsKey(ChildhoodsEnd));
            Assert.IsFalse(Drain(bystander).Any(packet => packet is ObjectiveCompletedPacket));

            // A second notification of the same death advances nobody again.
            _missions.OnSharedKillCredit(channel, xanx, killer);
            Assert.IsFalse(new[] { killer, partner }.SelectMany(Drain).Any(packet => packet is ObjectiveCompletedPacket || packet is ObjectiveRevealedPacket));
        }

        [TestMethod]
        public void XanxKilledByAnNpcOnTheSharedMapCreditsEveryoneWithTheObjective()
        {
            var first = WildernessCharacter(CharacterId, "Recruit", ChildhoodsEnd,
                (2, MissionObjectiveState.Completed), (3, MissionObjectiveState.Incomplete));
            var second = WildernessCharacter(CharacterId + 1, "Partner", ChildhoodsEnd,
                (2, MissionObjectiveState.Completed), (3, MissionObjectiveState.Incomplete));
            _instance = SharedWilderness(first, second);
            var xanx = new Creature { DbId = AriochXanx, MapContextId = Wilderness, State = CharacterState.Normal };

            // An NPC Ranger lands the blow: no player killer, and the Wilderness is a shared context.
            var ranger = new Creature { Faction = Factions.AFS, Cells = new uint[0, 0] };
            CreatureManager.Instance.HandleCreatureKill(_instance, xanx, ranger);

            Assert.AreEqual(CharacterState.Dead, xanx.State);
            foreach (var (client, id) in new[] { (first, CharacterId), (second, CharacterId + 1) })
            {
                Assert.AreEqual(MissionObjectiveState.Completed, client.Player.Missions[ChildhoodsEnd].Objectives[3], $"character {id}");
                Assert.AreEqual(MissionObjectiveState.Completed, SavedObjective(id, ChildhoodsEnd, 3), $"character {id}");
                Assert.AreEqual(0u, client.Player.Experience, "the kill rewards stay with a player killer; only the objective is shared");
            }
        }

        [TestMethod]
        public void KillBindingsWithoutTheFlagKeepKillerOnlyCreditOnSharedMaps()
        {
            // 427/6 Proctor Fulgor has no note like 682's: another player's or an NPC's kill does not count.
            var killer = WildernessCharacter(CharacterId, "Recruit", LurkingInTheShadows,
                (1, MissionObjectiveState.Completed), (6, MissionObjectiveState.Incomplete));
            var other = WildernessCharacter(CharacterId + 1, "Partner", LurkingInTheShadows,
                (1, MissionObjectiveState.Completed), (6, MissionObjectiveState.Incomplete));
            var channel = SharedWilderness(killer, other);
            var fulgor = new Creature { DbId = ProctorFulgor, MapContextId = Wilderness, State = CharacterState.Dead };

            _missions.OnCreatureKilled(killer, fulgor);
            _missions.OnSharedKillCredit(channel, fulgor, killer);

            Assert.AreEqual(MissionObjectiveState.Completed, killer.Player.Missions[LurkingInTheShadows].Objectives[6]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, other.Player.Missions[LurkingInTheShadows].Objectives[6]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, SavedObjective(CharacterId + 1, LurkingInTheShadows, 6));

            _instance = SharedWilderness(other);
            CreatureManager.Instance.HandleCreatureKill(_instance, new Creature { DbId = ProctorFulgor, MapContextId = Wilderness },
                new Creature { Faction = Factions.AFS, Cells = new uint[0, 0] });
            Assert.AreEqual(MissionObjectiveState.Incomplete, other.Player.Missions[LurkingInTheShadows].Objectives[6]);
        }
    }
}

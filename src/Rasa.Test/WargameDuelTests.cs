using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Packets.Wargame.Client;
using Rasa.Packets.Wargame.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// The player-to-player duel, against the 1.16.5.0 client's own protocol
    /// (verify/dis/trpython-client-wargame.pyo.dis, trpython-client-augmentations-actor.pyo.dis,
    /// trpython-client-communicator.pyo.dis, trpython-shared-teamdefs.pyo.dis,
    /// trpython-shared-gameconstants.pyo.dis and generated/client/methodid.pyo).
    ///
    /// Packet shapes first, then the state machine: challenge, accept, fight, end.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WargameDuelTests
    {
        private MapChannel _map;
        private Client _challenger;
        private Client _target;

        [TestInitialize]
        public void Setup()
        {
            ResetManager();

            _map = new MapChannel { MapInfo = new MapInfo(1220, "duel", 1, 1), ClientList = new List<Client>() };
            _map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client>() };

            _challenger = MakeClient(10, "Challenger", new Vector3(100f, 10f, 100f));
            _target = MakeClient(11, "Target", new Vector3(105f, 10f, 100f));
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var client in new[] { _challenger, _target })
            {
                if (client?.Player == null)
                    continue;

                Server.Clients.Remove(client);
                EntityManager.Instance.UnregisterEntity(client.Player.EntityId);
                EntityManager.Instance.UnregisterPlayer(client.Player.EntityId);
                EntityManager.Instance.UnregisterActor(client.Player.EntityId);
            }

            ResetManager();
        }

        #region Packet shapes

        [TestMethod]
        public void TheChallengeRequestCarriesNameLengthAndScoreAndToleratesTheNameAlone()
        {
            // client/wargame.py SendWargameChallenge -> ('ChallengeUserToWargameByName', (targetName, timeMins, maxKills))
            var full = ReadChallenge(writer =>
            {
                writer.WriteTuple(3);
                writer.WriteUnicodeString("Target");
                writer.WriteInt(7);
                writer.WriteInt(3);
            });

            Assert.AreEqual(626, (int)full.Opcode);
            Assert.AreEqual("Target", full.TargetName);
            Assert.AreEqual(7, full.TimeMins);
            Assert.AreEqual(3, full.MaxKills);

            // The typed command only appends what was typed (communicator.py ChallengeWargameDuel).
            var bare = ReadChallenge(writer =>
            {
                writer.WriteTuple(1);
                writer.WriteUnicodeString("Target");
            });

            Assert.AreEqual("Target", bare.TargetName);
            Assert.AreEqual(0, bare.TimeMins, "an unnamed length is 0, which means the server decides");
            Assert.AreEqual(0, bare.MaxKills);
        }

        [TestMethod]
        public void TheChallengeResponseReadsAcceptWithoutAMessageAndDeclineWithOne()
        {
            // OnAcceptWargameDuelChallenge sends the integer 1 with no pmMsg at all.
            var accept = ReadResponse(writer =>
            {
                writer.WriteTuple(1);
                writer.WriteInt(1);
            });

            Assert.AreEqual(606, (int)accept.Opcode);
            Assert.IsTrue(accept.Accepted);
            Assert.IsFalse(accept.PmMsg.HasValue);

            // OnRefuseWargameDuelChallenge sends (0, PM_WARGAME_REFUSED).
            var decline = ReadResponse(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteInt(0);
                writer.WriteUInt((uint)PlayerMessage.PmWargameRefused);
            });

            Assert.IsFalse(decline.Accepted);
            Assert.AreEqual(PlayerMessage.PmWargameRefused, decline.PmMsg);

            var none = ReadResponse(writer =>
            {
                writer.WriteTuple(2);
                writer.WriteInt(0);
                writer.WriteNoneStruct();
            });

            Assert.IsFalse(none.PmMsg.HasValue, "pmMsg has a default of None");
        }

        [TestMethod]
        public void TheRevokeRequestCarriesNothing()
        {
            var packet = new WargameChallengeRevokedPacket();
            Assert.AreEqual(638, (int)packet.Opcode);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                writer.WriteTuple(0);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void TheTwoChallengeDialogsCarryTheIdAndTheOtherPlayersName()
        {
            var challenged = Shape(new ChallengedToWargameDuelPacket(4, "Challenger"));
            Assert.AreEqual(603, (int)new ChallengedToWargameDuelPacket(4, "x").Opcode);
            Assert.AreEqual(2, challenged.Tuple);
            Assert.AreEqual(4u, challenged.UInts[0]);
            Assert.AreEqual("Challenger", challenged.Strings[0]);

            var challenging = Shape(new ChallengingToWargameDuelPacket(4, "Target"));
            Assert.AreEqual(635, (int)new ChallengingToWargameDuelPacket(4, "x").Opcode);
            Assert.AreEqual(2, challenging.Tuple);
            Assert.AreEqual(4u, challenging.UInts[0]);
            Assert.AreEqual("Target", challenging.Strings[0]);
        }

        [TestMethod]
        public void WargameStartedCarriesTheIdAndAListOfEnemyUserIds()
        {
            var packet = new WargameStartedPacket(9, new List<uint> { 11 });
            Assert.AreEqual(633, (int)packet.Opcode);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(9u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(11u, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void TheScoreboardCarriesOrientedScoresAndOptionalUserIds()
        {
            var packet = new WargameScoreboardPacket(9, 1, 0, 11, 10);
            Assert.AreEqual(632, (int)packet.Opcode);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual(9u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(11u, reader.ReadUInt());
            Assert.AreEqual(10u, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);

            // WargameStatus.UpdateScores guards both ids for None.
            using var empty = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(empty, System.Text.Encoding.UTF8, true)))
                new WargameScoreboardPacket(9, 0, 0, null, null).Write(writer);
            empty.Position = 0;
            using var emptyReader = new PythonReader(new BinaryReader(empty));
            Assert.AreEqual(5, emptyReader.ReadTuple());
            emptyReader.ReadUInt();
            emptyReader.ReadInt();
            emptyReader.ReadInt();
            emptyReader.ReadNoneStruct();
            emptyReader.ReadNoneStruct();
            Assert.AreEqual(empty.Length, empty.Position);
        }

        [TestMethod]
        public void WargameDataIsADictionaryOfWargameIdToSideToken()
        {
            // actor.py GetWargameParticipantStatus intersects two actors' key sets and compares
            // the tokens; shared/teamdefs.py numbers teamShirt 0 and teamSkin 1.
            var packet = new WargameDataPacket(new Dictionary<uint, int> { { 9, WargameManager.TeamSkin } });
            Assert.AreEqual(696, (int)packet.Opcode);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual(9u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);

            // The empty dict is what takes a duel off a body again: IsWargaming() is bool(dict).
            using var cleared = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(cleared, System.Text.Encoding.UTF8, true)))
                new WargameDataPacket(new Dictionary<uint, int>()).Write(writer);
            cleared.Position = 0;
            using var clearedReader = new PythonReader(new BinaryReader(cleared));
            Assert.AreEqual(1, clearedReader.ReadTuple());
            Assert.AreEqual(0, clearedReader.ReadDictionary());
            Assert.AreEqual(cleared.Length, cleared.Position);
        }

        [TestMethod]
        public void AWargameMessageCarriesAnIdAndItsPlaceholderArguments()
        {
            var packet = new DisplayWargameMessagePacket(PlayerMessage.PmWargameRefused,
                new Dictionary<string, string> { { "target", "Target" } });
            Assert.AreEqual(604, (int)packet.Opcode);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(10000062u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual("target", reader.ReadString());
            Assert.AreEqual("Target", reader.ReadString());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void EveryEndingOpcodeIsTheClientsOwn()
        {
            Assert.AreEqual(608, (int)new WargameVictoryPacket(1).Opcode);
            Assert.AreEqual(607, (int)new WargameDefeatPacket(1).Opcode);
            Assert.AreEqual(634, (int)new WargameTiedPacket(1).Opcode);
            Assert.AreEqual(605, (int)new WargameCancelledPacket(1).Opcode);
            Assert.AreEqual(715, (int)new RemoveFromWargamePacket(1).Opcode);
            Assert.AreEqual(630, (int)new RevokeWargameChallengePacket(1).Opcode);
            Assert.AreEqual(637, (int)new WargameChallengeRefusedPacket(1).Opcode);
            Assert.AreEqual(631, (int)new SetWargameMaxKillsPacket(1, 1).Opcode);
            Assert.AreEqual(628, (int)new DisplayWargameTimerPacket(1, 1).Opcode);
        }

        #endregion

        #region Challenge

        [TestMethod]
        public void AChallengePutsADialogOnEachScreenAndALineInEachChatWindow()
        {
            Challenge();

            var mine = Drain(_challenger);
            var theirs = Drain(_target);

            var challenging = mine.Single(m => m.MethodId == GameOpcode.ChallengingToWargameDuel);
            Assert.AreEqual((ulong)SysEntity.ClientWargameManagerId, challenging.EntityId,
                "the wargame manager's own entity, not the actor's");
            Assert.AreEqual("Target", ((ChallengingToWargameDuelPacket)challenging.Packet).TargetName);

            var challenged = theirs.Single(m => m.MethodId == GameOpcode.ChallengedToWargameDuel);
            Assert.AreEqual("Challenger", ((ChallengedToWargameDuelPacket)challenged.Packet).AgressorName);

            Assert.AreEqual(((ChallengingToWargameDuelPacket)challenging.Packet).WargameId,
                ((ChallengedToWargameDuelPacket)challenged.Packet).WargameId, "both sides hold the same id");

            Assert.AreEqual(PlayerMessage.PmWargameDuelPlayerChallenged, MessageIn(mine));
            Assert.AreEqual(PlayerMessage.PmWargameDuelPlayerChallengeReceived, MessageIn(theirs));
        }

        [TestMethod]
        public void ANameNobodyIsWearingIsRefusedWithTheClientsOwnLine()
        {
            WargameManager.Instance.ChallengeUserToWargameByName(_challenger, ChallengePacket("Nobody"));

            var mine = Drain(_challenger);
            Assert.AreEqual(PlayerMessage.PmWargameNoTargetByName, MessageIn(mine));
            Assert.IsFalse(mine.Any(m => m.MethodId == GameOpcode.ChallengingToWargameDuel));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void ChallengingYourselfIsRefused()
        {
            WargameManager.Instance.ChallengeUserToWargameByName(_challenger, ChallengePacket("Challenger"));

            Assert.AreEqual(PlayerMessage.PmWargameCannotChallengeYourself, MessageIn(Drain(_challenger)));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void ASecondChallengeWhileOneIsOutstandingIsRefused()
        {
            Challenge();
            Drain(_challenger);

            WargameManager.Instance.ChallengeUserToWargameByName(_challenger, ChallengePacket("Target"));

            Assert.AreEqual(PlayerMessage.PmWargameFailWaitForResponse, MessageIn(Drain(_challenger)));
        }

        [TestMethod]
        public void AChallengedPlayerMustAnswerBeforeChallengingSomeoneElse()
        {
            Challenge();
            Drain(_target);

            WargameManager.Instance.ChallengeUserToWargameByName(_target, ChallengePacket("Challenger"));

            Assert.AreEqual(PlayerMessage.PmWargameAcceptOrDeclineFirst, MessageIn(Drain(_target)));
        }

        [TestMethod]
        public void DecliningClosesBothWindowsAndRelaysTheRefusalTheClientNamed()
        {
            Challenge();
            var id = PendingId();
            Drain(_challenger);
            Drain(_target);

            WargameManager.Instance.WargameChallengeResponse(_target, Response(false, PlayerMessage.PmWargameRefused));

            var mine = Drain(_challenger);
            var theirs = Drain(_target);

            var refused = mine.Single(m => m.MethodId == GameOpcode.WargameChallengeRefused);
            Assert.AreEqual(id, ((WargameChallengeRefusedPacket)refused.Packet).WargameId);
            Assert.IsFalse(((WargameChallengeRefusedPacket)refused.Packet).YourPartyRefused, "a duel has no party");

            Assert.AreEqual(PlayerMessage.PmWargameRefused, MessageIn(mine));
            Assert.AreEqual(PlayerMessage.PmWargameYouRefused, MessageIn(theirs));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void ARefusalIdTheClientInventedIsNotRelayed()
        {
            Challenge();
            Drain(_challenger);

            // The refusing client names the message; the server only relays its own vocabulary.
            WargameManager.Instance.WargameChallengeResponse(_target, Response(false, PlayerMessage.PmYouAreNowPartyLeader));

            Assert.AreEqual(PlayerMessage.PmWargameRefused, MessageIn(Drain(_challenger)));
        }

        [TestMethod]
        public void RevokingTellsBothSidesToForgetTheId()
        {
            Challenge();
            var id = PendingId();
            Drain(_challenger);
            Drain(_target);

            WargameManager.Instance.WargameChallengeRevoked(_challenger);

            var mine = Drain(_challenger);
            var theirs = Drain(_target);

            // Only Recv_RevokeWargameChallenge deletes the client's WargameStatus, and the
            // challenger's own client never does it for itself - so both get the packet.
            Assert.AreEqual(id, ((RevokeWargameChallengePacket)mine.Single(m => m.MethodId == GameOpcode.RevokeWargameChallenge).Packet).WargameId);
            Assert.AreEqual(id, ((RevokeWargameChallengePacket)theirs.Single(m => m.MethodId == GameOpcode.RevokeWargameChallenge).Packet).WargameId);

            Assert.AreEqual(PlayerMessage.PmWargameChallengeRevoked, MessageIn(mine));
            Assert.AreEqual(PlayerMessage.PmWargameRevoked, MessageIn(theirs));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void AnUnansweredChallengeTimesOut()
        {
            Challenge();
            var created = WargameManager.Instance.Duels.Values.First().CreatedTick;
            Drain(_challenger);
            Drain(_target);

            WargameManager.Instance.Worker(created + WargameManager.ChallengeTimeoutMs - 1);
            Assert.AreEqual(2, WargameManager.Instance.Duels.Count, "still standing a millisecond early");

            WargameManager.Instance.Worker(created + WargameManager.ChallengeTimeoutMs);

            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
            Assert.AreEqual(PlayerMessage.PmWargameChallengeTimedOut, MessageIn(Drain(_challenger)));
            Assert.AreEqual(PlayerMessage.PmWargameChallengeTimedOut, MessageIn(Drain(_target)));
        }

        #endregion

        #region The duel

        [TestMethod]
        public void AcceptingStartsTheDuelInTheOrderTheClientNeeds()
        {
            Challenge();
            var id = PendingId();
            Drain(_challenger);
            Drain(_target);

            Accept();

            foreach (var (client, other) in new[] { (_challenger, _target), (_target, _challenger) })
            {
                var sent = Drain(client);
                var manager = sent.Where(m => m.EntityId == (ulong)SysEntity.ClientWargameManagerId).ToList();

                var started = manager.FindIndex(m => m.MethodId == GameOpcode.WargameStarted);
                var kills = manager.FindIndex(m => m.MethodId == GameOpcode.SetWargameMaxKills);
                var timer = manager.FindIndex(m => m.MethodId == GameOpcode.DisplayWargameTimer);

                Assert.IsTrue(started >= 0 && kills > started && timer > kills,
                    "WargameStarted first: SetWargameMaxKills and DisplayWargameTimer go through _GetWargameStatusEnsured");

                Assert.AreEqual(id, ((WargameStartedPacket)manager[started].Packet).WargameId);
                CollectionAssert.AreEqual(new List<uint> { other.AccountEntry.Id },
                    ((WargameStartedPacket)manager[started].Packet).EnemyUserIds);
                Assert.AreEqual(WargameManager.DefaultMaxKills, ((SetWargameMaxKillsPacket)manager[kills].Packet).MaxKills);
                Assert.AreEqual(WargameManager.DefaultTimeMins * 60 * 1000, ((DisplayWargameTimerPacket)manager[timer].Packet).TimeMs);

                // The body, and the opponent's client being told it may shoot it.
                var data = sent.Where(m => m.MethodId == GameOpcode.WargameData).ToList();
                Assert.AreEqual(2, data.Count, "both duellists share a cell, so each sees both bodies");

                var hostile = sent.Single(m => m.MethodId == GameOpcode.TargetCategory);
                Assert.AreEqual(other.Player.EntityId, hostile.EntityId);
                Assert.AreEqual(TargetCategory.Hostile, ((TargetCategoryPacket)hostile.Packet).TargetCategory);
            }
        }

        [TestMethod]
        public void TheTwoSidesGetOppositeTokensSoTheClientCanTellThemApart()
        {
            Challenge();
            Accept();

            var seen = Drain(_challenger)
                .Where(m => m.MethodId == GameOpcode.WargameData)
                .ToDictionary(m => m.EntityId, m => ((WargameDataPacket)m.Packet).WargameData.Values.Single());

            Assert.AreEqual(WargameManager.TeamShirt, seen[_challenger.Player.EntityId]);
            Assert.AreEqual(WargameManager.TeamSkin, seen[_target.Player.EntityId]);
        }

        [TestMethod]
        public void OnlyTheTwoDuellistsMayShootEachOther()
        {
            var bystander = MakeClient(12, "Bystander", new Vector3(101f, 10f, 100f));

            Assert.IsFalse(WargameManager.Instance.AreDuelOpponents(_challenger.Player, _target.Player),
                "no duel, no friendly fire");

            Challenge();
            Accept();

            Assert.IsTrue(WargameManager.Instance.AreDuelOpponents(_challenger.Player, _target.Player));
            Assert.IsTrue(WargameManager.Instance.AreDuelOpponents(_target.Player, _challenger.Player));
            Assert.IsFalse(WargameManager.Instance.AreDuelOpponents(_challenger.Player, bystander.Player));
            Assert.IsFalse(WargameManager.Instance.AreDuelOpponents(_challenger.Player, _challenger.Player));

            WargameManager.Instance.SurrenderWargame(_target);

            Assert.IsFalse(WargameManager.Instance.AreDuelOpponents(_challenger.Player, _target.Player),
                "and none once it is over");

            Server.Clients.Remove(bystander);
            EntityManager.Instance.UnregisterEntity(bystander.Player.EntityId);
            EntityManager.Instance.UnregisterPlayer(bystander.Player.EntityId);
            EntityManager.Instance.UnregisterActor(bystander.Player.EntityId);
        }

        [TestMethod]
        public void TheLosingBlowScoresTheDuelAndLeavesTheLoserStanding()
        {
            Challenge();
            Accept();
            var id = PendingId();
            Drain(_challenger);
            Drain(_target);

            // Everything a weapon attack would do, through the real damage path.
            _target.Player.Attributes[Attributes.Health].Current = 5;
            MissileManager.Instance.MissileTrigger(_map, new Missile
            {
                Source = _challenger.Player,
                ActionId = ActionId.WeaponAttack,
                TargetActor = _target.Player,
                TargetEntityId = _target.Player.EntityId,
                DamageA = 500,
                DamageType = DamageType.Physical
            });

            Assert.AreEqual(CharacterState.Normal, _target.Player.State, "a duel loss is not a death");
            Assert.AreEqual(1, _target.Player.Attributes[Attributes.Health].Current,
                "left standing on one point: the damage dealt stays dealt, only the death is refused");

            var mine = Drain(_challenger).Where(m => m.EntityId == (ulong)SysEntity.ClientWargameManagerId).ToList();
            var theirs = Drain(_target).Where(m => m.EntityId == (ulong)SysEntity.ClientWargameManagerId).ToList();

            var myBoard = mine.FindIndex(m => m.MethodId == GameOpcode.WargameScoreboard);
            var myEnd = mine.FindIndex(m => m.MethodId == GameOpcode.WargameVictory);
            Assert.IsTrue(myBoard >= 0 && myEnd > myBoard,
                "Recv_WargameScoreboard only records a score while bActive, which the victory clears");

            var board = (WargameScoreboardPacket)mine[myBoard].Packet;
            Assert.AreEqual(id, board.WargameId);
            Assert.AreEqual(1, board.YourKills);
            Assert.AreEqual(0, board.TheirKills);
            Assert.AreEqual(_target.AccountEntry.Id, board.VictimId);
            Assert.AreEqual(_challenger.AccountEntry.Id, board.KillerId);

            var mirrored = (WargameScoreboardPacket)theirs[theirs.FindIndex(m => m.MethodId == GameOpcode.WargameScoreboard)].Packet;
            Assert.AreEqual(0, mirrored.YourKills, "yourKills is oriented to the recipient");
            Assert.AreEqual(1, mirrored.TheirKills);
            Assert.AreEqual(_target.AccountEntry.Id, mirrored.VictimId, "the ids are not");

            Assert.IsTrue(theirs.Any(m => m.MethodId == GameOpcode.WargameDefeat));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void OnePlayersDamageToAnotherIsHalvedAndACreaturesIsNot()
        {
            // shared/gameconstants.pyo PVP_DAMAGE_MODIFIER = 0.5, beside the
            // PVP_CRITICAL_DAMAGE_MODIFIER DamageModifiers.Crit already honours.
            Challenge();
            Accept();

            _target.Player.Attributes[Attributes.Health].Current = 100;
            MissileManager.Instance.MissileTrigger(_map, new Missile
            {
                Source = _challenger.Player,
                ActionId = ActionId.WeaponAttack,
                TargetActor = _target.Player,
                TargetEntityId = _target.Player.EntityId,
                DamageA = 40,
                DamageType = DamageType.Physical
            });

            Assert.AreEqual(80, _target.Player.Attributes[Attributes.Health].Current);

            var beast = new Creature { Cells = new uint[,] { { 0 } }, MapContextId = 1220 };
            MissileManager.Instance.MissileTrigger(_map, new Missile
            {
                Source = beast,
                ActionId = ActionId.WeaponAttack,
                TargetActor = _target.Player,
                TargetEntityId = _target.Player.EntityId,
                DamageA = 40,
                DamageType = DamageType.Physical
            });

            Assert.AreEqual(40, _target.Player.Attributes[Attributes.Health].Current,
                "a creature hitting a player is not PvP");
        }

        [TestMethod]
        public void TheEndTakesTheDuelBackOffBothBodies()
        {
            Challenge();
            Accept();
            Drain(_challenger);
            Drain(_target);

            WargameManager.Instance.SurrenderWargame(_target);

            var mine = Drain(_challenger);

            Assert.IsTrue(mine.Any(m => m.MethodId == GameOpcode.WargameVictory), "the surrendering side loses");

            foreach (var entityId in new[] { _challenger.Player.EntityId, _target.Player.EntityId })
            {
                var cleared = mine.Last(m => m.MethodId == GameOpcode.WargameData && m.EntityId == entityId);
                Assert.AreEqual(0, ((WargameDataPacket)cleared.Packet).WargameData.Count,
                    "an empty dict is what makes Actor.IsWargaming() False again");
            }

            var friendly = mine.Last(m => m.MethodId == GameOpcode.TargetCategory);
            Assert.AreEqual(_target.Player.EntityId, friendly.EntityId);
            Assert.AreEqual(TargetCategory.Friendly, ((TargetCategoryPacket)friendly.Packet).TargetCategory);
        }

        [TestMethod]
        public void SurrenderingWithoutADuelSaysSo()
        {
            WargameManager.Instance.SurrenderWargame(_challenger);

            Assert.AreEqual(PlayerMessage.PmWargameNotInSquadOrDuel, MessageIn(Drain(_challenger)));
        }

        [TestMethod]
        public void TheBoutClockEndsItOnScoreAndTiesWhenTheScoreIsLevel()
        {
            Challenge();
            Accept();
            Drain(_challenger);
            Drain(_target);

            var duel = WargameManager.Instance.Duels.Values.First();

            WargameManager.Instance.Worker(duel.EndsAtTick);

            Assert.IsTrue(Drain(_challenger).Any(m => m.MethodId == GameOpcode.WargameTied));
            Assert.IsTrue(Drain(_target).Any(m => m.MethodId == GameOpcode.WargameTied));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void WalkingAwayStopsTheDuelWithoutDecidingIt()
        {
            Challenge();
            Accept();
            Drain(_challenger);
            Drain(_target);

            _target.Player.Position = new Vector3(100f + WargameManager.SeparationRange + 1f, 10f, 100f);
            WargameManager.Instance.Worker();

            foreach (var client in new[] { _challenger, _target })
            {
                var sent = Drain(client);
                Assert.IsTrue(sent.Any(m => m.MethodId == GameOpcode.WargameCancelled));
                Assert.IsFalse(sent.Any(m => m.MethodId == GameOpcode.WargameVictory || m.MethodId == GameOpcode.WargameDefeat),
                    "nothing surviving says a duellist who walks away forfeits");
                Assert.AreEqual(PlayerMessage.PmWargameCancelled, MessageIn(sent));
            }

            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        [TestMethod]
        public void LeavingTheWorldTakesTheLeaverOutAndCancelsItForTheOther()
        {
            Challenge();
            Accept();
            Drain(_challenger);
            Drain(_target);

            WargameManager.Instance.RemovePlayer(_target);

            var theirs = Drain(_target);
            var mine = Drain(_challenger);

            Assert.IsTrue(theirs.Any(m => m.MethodId == GameOpcode.RemoveFromWargame),
                "PM_WARGAME_YOU_LEFT is what RemoveFromWargame says, and it is what the leaver did");
            Assert.IsTrue(mine.Any(m => m.MethodId == GameOpcode.WargameCancelled));
            Assert.AreEqual(PlayerMessage.PmWargamePlayerLeft, MessageIn(mine));
            Assert.AreEqual(0, WargameManager.Instance.Duels.Count);
        }

        #endregion

        #region Harness

        private void Challenge() =>
            WargameManager.Instance.ChallengeUserToWargameByName(_challenger, ChallengePacket("Target"));

        private void Accept() =>
            WargameManager.Instance.WargameChallengeResponse(_target, Response(true, null));

        private uint PendingId() => WargameManager.Instance.Duels.Values.First().WargameId;

        private Client MakeClient(uint accountId, string familyName, Vector3 position)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };

            client.Player.Id = accountId;
            client.Player.Name = familyName;
            client.Player.FamilyName = familyName;
            client.Player.State = CharacterState.Normal;
            client.Player.MapChannel = _map;
            client.Player.MapContextId = 1220;
            client.Player.Position = position;
            client.Player.Cells = new uint[,] { { 0 } };
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 0, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = accountId });

            _map.ClientList.Add(client);
            _map.MapCellInfo.Cells[0].ClientList.Add(client);
            Server.Clients.Add(client);

            EntityManager.Instance.RegisterEntity(client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(client.Player.EntityId, client.Player);
            EntityManager.Instance.RegisterActor(client.Player.EntityId, client.Player);

            return client;
        }

        private static void ResetManager() =>
            typeof(WargameManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);

        private static ChallengeUserToWargameByNamePacket ChallengePacket(string name) =>
            new ChallengeUserToWargameByNamePacket { TargetName = name };

        private static WargameChallengeResponsePacket Response(bool accepted, PlayerMessage? pmMsg) =>
            new WargameChallengeResponsePacket { Accepted = accepted, PmMsg = pmMsg };

        private static PlayerMessage? MessageIn(IEnumerable<CallMethodMessage> sent)
        {
            var message = sent.LastOrDefault(m => m.MethodId == GameOpcode.DisplayWargameMessage);
            return message == null ? null : ((DisplayWargameMessagePacket)message.Packet).MsgId;
        }

        private static ChallengeUserToWargameByNamePacket ReadChallenge(System.Action<PythonWriter> body)
        {
            var packet = new ChallengeUserToWargameByNamePacket();
            Feed(packet, body);
            return packet;
        }

        private static WargameChallengeResponsePacket ReadResponse(System.Action<PythonWriter> body)
        {
            var packet = new WargameChallengeResponsePacket();
            Feed(packet, body);
            return packet;
        }

        private static void Feed(ClientPythonPacket packet, System.Action<PythonWriter> body)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                body(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
        }

        private readonly struct Read
        {
            public Read(int tuple, List<uint> uints, List<string> strings)
            {
                Tuple = tuple;
                UInts = uints;
                Strings = strings;
            }

            public int Tuple { get; }
            public List<uint> UInts { get; }
            public List<string> Strings { get; }
        }

        /// <summary>An (id, name) packet: the shape both challenge dialogs share.</summary>
        private static Read Shape(ServerPythonPacket packet)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            var tuple = reader.ReadTuple();
            var uints = new List<uint> { reader.ReadUInt() };
            var strings = new List<string> { reader.ReadUnicodeString() };

            Assert.AreEqual(stream.Length, stream.Position);
            return new Read(tuple, uints, strings);
        }

        private static List<CallMethodMessage> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<CallMethodMessage>();
            var kept = new List<ProtocolPacket>();

            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    messages.Add(call);
                else
                    kept.Add(protocol);

            foreach (var protocol in kept)
                queue.EnqueueOutgoing(protocol);

            return messages;
        }

        #endregion
    }
}

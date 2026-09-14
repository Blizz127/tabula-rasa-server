using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Communicator.Server;
    using Structures;

    /// <summary>Per-character kill streak; lives with the session only.</summary>
    public sealed class KillStreakState
    {
        public int Kills { get; set; }
        public int Value { get; set; }
        public long LastKillMs { get; set; }
        public bool MaxRewarded { get; set; }
    }

    /// <summary>
    /// Experience, credits and kill streak for a player's creature kill (docs/evidence/kill-rewards.json).
    /// Group experience, crit kills, shared-damage partial credit and level-difference modifiers
    /// are recorded gaps.
    /// </summary>
    public sealed class KillRewardManager
    {
        private static readonly Lazy<KillRewardManager> Singleton = new(() => new KillRewardManager(
            () => Environment.TickCount64,
            (client, experience, baseGained, streakMod) => ManifestationManager.Instance.GainExperience(client, experience, baseGained, streakMod),
            (client, update, value) => CharacterManager.Instance.UpdateCharacter(client, update, value)));
        public static KillRewardManager Instance => Singleton.Value;

        private readonly Func<long> _clock;
        private readonly Action<Client, uint, uint, int> _gainExperience;
        private readonly Action<Client, CharacterUpdate, object> _update;

        public KillRewardManager(Func<long> clock, Action<Client, uint, uint, int> gainExperience, Action<Client, CharacterUpdate, object> update)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _gainExperience = gainExperience ?? throw new ArgumentNullException(nameof(gainExperience));
            _update = update ?? throw new ArgumentNullException(nameof(update));
        }

        public void AwardKill(Client client, Creature creature)
        {
            var player = client?.Player;
            if (player == null || creature == null)
                return;

            var now = _clock();
            var streak = player.KillStreak;

            if (streak.Kills > 0 && now - streak.LastKillMs > KillRewardRules.StreakWindowMs)
                Reset(client, streak);

            streak.Kills++;
            streak.LastKillMs = now;

            var value = Math.Min(KillRewardRules.MaxStreak(player.Level), streak.Kills / KillRewardRules.StreakKillsPerStep);
            if (value > streak.Value)
            {
                streak.Value = value;
                client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(value));

                // A4-10, B1-041: the prestige line comes before the doubled experience line.
                if (value == KillRewardRules.MaxStreak(player.Level) && !streak.MaxRewarded)
                {
                    streak.MaxRewarded = true;
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(
                        PlayerMessage.PmPrestigePointsReceivedKillstreakmax,
                        new Dictionary<string, string> { ["amount"] = KillRewardRules.MaxStreakPrestige.ToString() },
                        MsgFilterId.PrestigeGainLose));
                    _update(client, CharacterUpdate.Prestige, KillRewardRules.MaxStreakPrestige);
                }
            }

            var streakMod = 1 + streak.Value;
            _gainExperience(client, KillRewardRules.Experience((int)creature.Level, streakMod),
                KillRewardRules.BaseExperience((int)creature.Level), streakMod);

            // Paid at the kill, not from the corpse: the credit line arrives with the experience line
            // (A3-081, B1-018). Recv_GotLoot with no items prints "You received %(amount)s credits.".
            var credits = KillRewardRules.Credits((int)creature.Level);
            _update(client, CharacterUpdate.Credits, credits);
            client.CallMethod(SysEntity.ClientMethodId, new GotLootPacket(creature.EntityId, credits));
        }

        /// <summary>Ends streaks whose window has passed. Run once per second from the map worker.</summary>
        public void ExpireStreaks(MapChannel mapChannel)
        {
            if (mapChannel?.ClientList == null)
                return;

            var now = _clock();
            foreach (var client in mapChannel.ClientList)
            {
                var streak = client?.Player?.KillStreak;
                if (streak != null && streak.Kills > 0 && now - streak.LastKillMs > KillRewardRules.StreakWindowMs)
                    Reset(client, streak);
            }
        }

        private static void Reset(Client client, KillStreakState streak)
        {
            if (streak.Value > 0)
                client.CallMethod(SysEntity.ClientMethodId, new SetKillStreakPacket(0));

            streak.Kills = 0;
            streak.Value = 0;
            streak.MaxRewarded = false;
        }
    }
}

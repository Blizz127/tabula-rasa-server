using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.ClientMethod.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// What a dead player was offered: the hospitals advertised in PlayerDead, which ReviveMe
    /// must choose from.
    /// </summary>
    public sealed class PlayerDeathOffer
    {
        public ulong SourceId { get; }
        public IReadOnlyList<HospitalData> Hospitals { get; }

        /// <summary>Whether revival brings Resuscitation Trauma (level and cause of this death).</summary>
        public bool PenaltyApplies { get; }

        public PlayerDeathOffer(ulong sourceId, IReadOnlyList<HospitalData> hospitals, bool penaltyApplies = false)
        {
            SourceId = sourceId;
            Hospitals = hospitals;
            PenaltyApplies = penaltyApplies;
        }
    }

    /// <summary>
    /// Player death, hospital choice and hospital revival, from the 1.16.5.0 client contract
    /// (docs/player-death-client-evidence.md) and the final-live footage (A4-35..39, B3-042..059,
    /// B2-019). Trauma, equipment wear, ally revival and death persistence are separate gaps
    /// (docs/player-death-implementation.md).
    /// </summary>
    public sealed class PlayerDeathManager
    {
        private static readonly Lazy<PlayerDeathManager> Singleton = new(() =>
            new PlayerDeathManager((client, update, value) => CharacterManager.Instance.UpdateCharacter(client, update, value)));
        public static PlayerDeathManager Instance => Singleton.Value;

        private readonly Action<Client, CharacterUpdate, object> _persist;
        private readonly Action<Client> _refreshAttributes;

        public PlayerDeathManager(Action<Client, CharacterUpdate, object> persist, Action<Client> refreshAttributes = null)
        {
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
            _refreshAttributes = refreshAttributes ?? (client =>
            {
                ManifestationManager.Instance.UpdateStatsValues(client, false);
                client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
            });
        }

        /// <summary>
        /// The hospitals a player on this map may respawn at: known (gained, or known without
        /// discovery) and, for control-point hospitals, AFS-held. No control-point state exists
        /// on this server, so control points are treated as held (GAP-CONTROL-POINTS).
        /// </summary>
        public static List<HospitalData> OfferedHospitals(Manifestation player)
        {
            if (player?.MapChannel?.MapInfo == null)
                return new List<HospitalData>();

            return HospitalCatalog.ForMap(player.MapChannel.MapInfo.MapContextId)
                .Where(hospital => hospital.KnownWithoutDiscovery || HasGained(player, hospital))
                .ToList();
        }

        private static bool HasGained(Manifestation player, HospitalData hospital)
            => player.GainedWaypoints.Any(waypoint => waypoint.WaypointId == hospital.WaypointId &&
                waypoint.WaypointType == (byte)WaypointType.Hospital);

        /// <summary>
        /// Called after the killing hit's recovery (deathBlow set) has gone to the source's
        /// observers. The player is already Dead, so later hits and actions skip it.
        /// </summary>
        public void AnnounceDeath(MapChannel mapChannel, Manifestation player, Actor source)
        {
            // Observers who could not see the source get the victim-side fallback.
            CellManager.Instance.CellCallMethod(mapChannel, player, new ActorKilledPacket());

            var client = mapChannel.ClientList?.FirstOrDefault(candidate => candidate?.Player == player);
            if (client == null)
                return;

            var hospitals = OfferedHospitals(player);
            // Deaths are not otherwise logged, and a player whose respawn does nothing cannot be told apart
            // from one who never asked: every death records the map and what was offered.
            Logger.WriteLog(LogType.Debug,
                $"{player.Name} died on map {mapChannel.MapInfo?.MapContextId} at ({player.Position.X:0.#}, {player.Position.Y:0.#}, {player.Position.Z:0.#}); offering {hospitals.Count} hospital(s).");
            // Help text 5687: trauma follows a death to a non-player character or to a player of a
            // clan at war, never a duel. Player kills are not implemented, so only NPC deaths count;
            // gameconstants DEATH_PENALTY_MIN_LEVEL keeps it from characters below level 5.
            var penalty = player.Level >= DeathPenaltyRules.MinLevel && !(source is Manifestation);
            player.DeathOffer = new PlayerDeathOffer(source?.EntityId ?? 0, hospitals, penalty);

            // Owner only. A non-empty list opens Hospital Selection (A4-36, B3-043); canRevive
            // stays 0 because no ally revival exists here.
            client.CallMethod(player.EntityId, new PlayerDeadPacket(player.DeathOffer.SourceId,
                hospitals.Select(hospital => new GraveyardInfo(hospital.GraveyardId, hospital.Position, hospital.IsSafe))));
        }

        /// <summary>
        /// ReviveMe(graveyardId): the Respawn button sends the selected id; closing the window or
        /// the death dialog's "Go To Hospital" sends None.
        /// </summary>
        public void ReviveMe(Client client, ReviveMePacket packet)
        {
            var player = client?.Player;
            if (client == null || client.State != ClientState.Ingame || player?.MapChannel == null)
                return;

            if (player.State != CharacterState.Dead || player.DeathOffer == null)
            {
                // The death dialog can be up on the client while this server has the player alive (a repeat
                // request after a respawn, or a client-side death the server never saw). Both must be ignored,
                // but they were indistinguishable in the logs from a request that was acted on, so the reason
                // is recorded: GAP-DEATH-DIVERGENCE.
                Logger.WriteLog(LogType.Error,
                    $"{player.Name} asked to respawn at {(packet.GraveyardId is int ignoredId ? ignoredId.ToString() : "None")} while state was {player.State} with {(player.DeathOffer == null ? "no" : "a")} death offer; ignored.");
                return;
            }

            var offer = player.DeathOffer;
            HospitalData hospital;

            if (packet.GraveyardId is int requested)
            {
                hospital = offer.Hospitals.FirstOrDefault(candidate => candidate.GraveyardId == requested);
                if (hospital == null)
                {
                    // Not what this death offered: the player stays dead and can choose again.
                    Logger.WriteLog(LogType.Security, $"{player.Name} asked to respawn at hospital {requested}, which was not offered.");
                    return;
                }
            }
            else
            {
                // None: the nearest offered hospital by squared X/Y/Z distance, the rule the client's
                // own waypointwindow._RespawnPlayer uses when its timer runs out (inferred for the server).
                hospital = offer.Hospitals
                    .OrderBy(candidate => Vector3.DistanceSquared(candidate.Position, player.Position))
                    .FirstOrDefault();
            }

            player.DeathOffer = null;

            if (hospital != null)
                MoveToHospital(client, hospital);
            else
                // No hospital on this map (none of the starting areas): revive in place rather than
                // leave the character dead with no way out (emulator fallback, GAP-NO-HOSPITAL).
                Logger.WriteLog(LogType.Error, $"{player.Name} died on map {player.MapChannel.MapInfo?.MapContextId} with no hospital to offer; reviving in place.");

            // "The medical techs will revive and restore you to full health" (tutorial 1582).
            // Armor is restored with it (inferred). "Upon resuscitation after death, the entire
            // adrenaline bar is drained" (TaRapedia Adrenaline, 2007-11-07, fan-observed).
            player.State = CharacterState.Normal;
            if (player.Attributes.TryGetValue(Attributes.Health, out var health))
                health.Current = health.CurrentMax;
            if (player.Attributes.TryGetValue(Attributes.Armor, out var armor))
                armor.Current = armor.CurrentMax;
            if (player.Attributes.TryGetValue(Attributes.Chi, out var adrenaline))
                adrenaline.Current = 0;

            var mapChannel = player.MapChannel;
            CellManager.Instance.CellCallMethod(mapChannel, player, new RevivedPacket(0));
            if (health != null)
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateHealthPacket(health, 0));
            if (armor != null)
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateArmorPacket(armor, 0));
            if (adrenaline != null)
                CellManager.Instance.CellCallMethod(mapChannel, player, new UpdateChiPacket(adrenaline, player.EntityId));

            if (offer.PenaltyApplies)
                ApplyTrauma(client);

            if (hospital != null)
                _persist(client, CharacterUpdate.Position, null);
        }

        /// <summary>
        /// Resuscitation Trauma after a revival: 20% of the attributes for two minutes, and each
        /// further resuscitation while traumatized adds 20% and two minutes, up to 60% and six minutes
        /// (tooltip 506, help 5687, D10.6 live notes). The no-heal effect runs 30 s from each one.
        /// </summary>
        private void ApplyTrauma(Client client)
        {
            var player = client.Player;
            var mapChannel = player.MapChannel;

            var existing = player.ActiveEffects.Values.FirstOrDefault(effect => effect.TypeId == DeathPenaltyRules.RezSicknessEffectType);
            var stacks = existing == null ? 1 : Math.Min(DeathPenaltyRules.MaxStacks, player.TraumaStacks + 1);
            var duration = existing == null
                ? DeathPenaltyRules.DurationPerStackMs
                : DeathPenaltyRules.NextDurationMs(existing.Duration - existing.EffectTime);

            // Replace rather than detach: detaching would end the trauma and restore the attributes.
            ReplaceEffect(mapChannel, player, existing);
            ReplaceEffect(mapChannel, player, player.ActiveEffects.Values.FirstOrDefault(effect => effect.TypeId == DeathPenaltyRules.RezSicknessNoHealEffectType));

            player.TraumaStacks = stacks;
            GameEffectManager.Instance.AttachTimedDebuff(mapChannel, player, DeathPenaltyRules.RezSicknessEffectType, (uint)stacks, duration);
            GameEffectManager.Instance.AttachTimedDebuff(mapChannel, player, DeathPenaltyRules.RezSicknessNoHealEffectType, 1, DeathPenaltyRules.NoHealDurationMs);
            _refreshAttributes(client);
        }

        private static void ReplaceEffect(MapChannel mapChannel, Manifestation player, GameEffect effect)
        {
            if (effect == null || !player.ActiveEffects.Remove(effect.EffectId))
                return;
            CellManager.Instance.CellCallMethod(mapChannel, player, new GameEffectDetachedPacket { EffectId = effect.EffectId });
        }

        /// <summary>The RezSickness effect ran out: the attributes come back.</summary>
        public void OnTraumaEnded(MapChannel mapChannel, Manifestation player)
        {
            player.TraumaStacks = 0;
            var client = mapChannel?.ClientList?.FirstOrDefault(candidate => candidate?.Player == player);
            if (client != null)
                _refreshAttributes(client);
        }

        /// <summary>
        /// "Upon first entering a zone, a character is given the nearest hospital point" (TaRapedia
        /// Hospital, December 2007, fan-observed); consistent with "You just gained Alia Das Hospital."
        /// on arrival from the boot camp (B2-019). Only when the character has none of this map's yet.
        /// </summary>
        public void OnPlayerEnteredMap(Client client)
        {
            var player = client?.Player;
            if (player?.MapChannel?.MapInfo == null)
                return;

            var hospitals = HospitalCatalog.ForMap(player.MapChannel.MapInfo.MapContextId)
                .Where(hospital => !hospital.KnownWithoutDiscovery).ToList();
            if (hospitals.Count == 0 || hospitals.Any(hospital => HasGained(player, hospital)))
                return;

            Gain(client, hospitals.OrderBy(hospital => Vector3.DistanceSquared(hospital.Position, player.Position)).First());
        }

        private void Gain(Client client, HospitalData hospital)
        {
            var player = client.Player;
            var entry = new CharacterTeleporterEntry(player.Id, hospital.WaypointId, (byte)WaypointType.Hospital);
            player.GainedWaypoints.Add(entry);
            client.CallMethod(player.EntityId, new GraveyardGainedPacket(hospital.WaypointId));
            _persist(client, CharacterUpdate.Teleporter, entry);
        }

        /// <summary>BuryMe is the Revive button, which canRevive = 0 keeps disabled.</summary>
        public void BuryMe(Client client)
        {
            if (client?.Player?.State == CharacterState.Dead)
                Logger.WriteLog(LogType.Security, $"{client.Player.Name} asked to revive in place, which was not offered.");
        }

        private static void MoveToHospital(Client client, HospitalData hospital)
        {
            var player = client.Player;
            Logger.WriteLog(LogType.Debug,
                $"{player.Name} respawns at hospital {hospital.GraveyardId} ({hospital.Position.X:0.#}, {hospital.Position.Y:0.#}, {hospital.Position.Z:0.#}).");
            player.Position = hospital.Position;

            // Actor.BeginTeleport queues the acknowledgement that Recv_Teleport then sends, so it
            // goes first. The hospital's facing is unrecovered: the player keeps their own.
            client.CellCallMethod(client, player.EntityId, new PreTeleportPacket(TeleportType.Default));
            client.CallMethod(SysEntity.ClientMethodId, new BeginTeleportPacket());
            client.CallMethod(player.EntityId, new TeleportPacket(hospital.Position, player.Rotation, TeleportType.Default, 0));
            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId,
                new Movement(hospital.Position, 0f, 0, new Vector2((float)player.Rotation, 0f))), true);
        }

        /// <summary>
        /// Gains the hospitals a living player has come within DiscoveryRadius of (horizontal).
        /// Run once per second from the map worker.
        /// </summary>
        public void DiscoverHospitals(MapChannel mapChannel)
        {
            if (mapChannel?.MapInfo == null || mapChannel.ClientList == null)
                return;

            var hospitals = HospitalCatalog.ForMap(mapChannel.MapInfo.MapContextId)
                .Where(hospital => !hospital.KnownWithoutDiscovery).ToList();
            if (hospitals.Count == 0)
                return;

            foreach (var client in mapChannel.ClientList)
            {
                var player = client?.Player;
                if (player == null || client.State != ClientState.Ingame || player.Disconected ||
                    player.RemoveFromMap || player.State == CharacterState.Dead)
                    continue;

                foreach (var hospital in hospitals)
                {
                    if (HasGained(player, hospital))
                        continue;

                    var dx = player.Position.X - hospital.Position.X;
                    var dz = player.Position.Z - hospital.Position.Z;
                    if (dx * dx + dz * dz > HospitalCatalog.DiscoveryRadius * HospitalCatalog.DiscoveryRadius)
                        continue;

                    Gain(client, hospital);
                }
            }
        }
    }
}

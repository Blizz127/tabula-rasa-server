using System;
using System.Collections.Concurrent;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Petition.Client;
    using Packets.Petition.Server;
    using Repositories.UnitOfWork;
    using Structures.Char;

    /// <summary>
    ///     Petition packets:
    ///     -- WorldMsg
    /// - CreateBugReport                     => implemented
    /// - CreateHelpRequest                   => implemented
    /// - AddToPetition                       => no client caller
    /// - CancelPetition                      => no client caller
    /// - RetrievePetition                    => no client caller
    /// - SearchPetitions                     => no client caller
    /// - SearchKB                            => no client caller
    /// - RetrieveKBArticle                   => no client caller
    ///
    ///     Petition handlers (client/petitionmanager.py):
    /// - CreatePetitionAck                   => implemented
    /// - AddToPetitionAck                    => empty body in the client
    /// - CancelPetitionAck                   => empty body in the client
    /// - RetrievePetitionAck                 => empty body in the client
    /// - SearchPetitionsAck                  => empty body in the client
    /// - SearchKBAck                         => empty body in the client
    /// - RetrieveKBArticleAck                => empty body in the client
    ///
    /// Only the two create calls are reachable: helpwindow.py wires Send on the Report Bug and
    /// Petition pages, and /bug and /petition open those same pages. The rest were the GM and
    /// knowledge-base half of the feature and nothing in the shipped client calls them, so
    /// their opcodes stay unhandled.
    ///
    /// Both create calls used to disconnect the player. An opcode with no registered packet
    /// leaves its payload unread, CallServerMethodMessage.ReadPacket then fails the 0x66
    /// terminator check, and Client.Close(true) runs - so pressing Send dropped you to the
    /// login screen with no message.
    /// </summary>
    public class PetitionManager
    {
        /// <summary>
        /// petition.summary is varchar(255) and petition.body is TEXT. The client caps neither
        /// edit box, so both are trimmed to fit rather than refused: a report that arrives long
        /// is still worth reading.
        /// </summary>
        private const int MaxSummaryLength = 255;
        private const int MaxBodyLength = 8000;

        /// <summary>
        /// One filing per account per half minute. The window hides itself on Send and has to be
        /// reopened by hand, so this never gets in a real player's way, but it does stop a
        /// scripted client from filling the table as fast as the socket allows.
        /// </summary>
        private const int CooldownSeconds = 30;

        private readonly ConcurrentDictionary<uint, DateTime> _lastFiled = new ConcurrentDictionary<uint, DateTime>();

        #region Singleton

        private static PetitionManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public static PetitionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new PetitionManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private PetitionManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        #endregion

        #region Handlers

        internal void CreateBugReport(Client client, CreateBugReportPacket packet)
        {
            File(client, PetitionType.BugReport, packet.Summary, packet.Body);
        }

        internal void CreateHelpRequest(Client client, CreateHelpRequestPacket packet)
        {
            File(client, PetitionType.HelpRequest, packet.Summary, packet.Body);
        }

        #endregion

        /// <summary>
        /// Drops the cooldown when the player leaves the world. Called from
        /// MapChannelManager.RemovePlayer alongside the other per-player manager cleanup.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (client.AccountEntry == null)
                return;

            _lastFiled.TryRemove(client.AccountEntry.Id, out _);
        }

        private void File(Client client, PetitionType type, string summary, string body)
        {
            var accountId = client.AccountEntry.Id;

            summary = Clamp(summary, MaxSummaryLength);
            body = Clamp(body, MaxBodyLength);

            // The description always ends up non-empty - petitionmanager.py appends the client
            // version to it before sending - so the subject is the only field that tells a real
            // filing from someone closing the window with the Send button.
            if (summary.Length == 0)
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} filed a {type} with no subject. Refused.");
                Ack(client, false, 0);
                return;
            }

            if (OnCooldown(accountId))
            {
                Logger.WriteLog(LogType.Debug,
                    $"Account {accountId} filed a {type} inside the {CooldownSeconds}s cooldown. Refused.");
                Ack(client, false, 0);
                return;
            }

            var position = client.Player.Position;

            var entry = new PetitionEntry(
                accountId,
                client.Player.Id,
                (byte)type,
                summary,
                body,
                client.Player.MapContextId,
                position.X,
                position.Y,
                position.Z);

            uint petitionId;

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                petitionId = unitOfWork.Petitions.AddPetition(entry);

            if (petitionId == 0)
            {
                // The row did not go in, so nothing was filed and the cooldown is not spent.
                Ack(client, false, 0);
                return;
            }

            _lastFiled[accountId] = DateTime.UtcNow;

            // Logged as well as stored: whoever is watching the console is the person a help
            // request is waiting on, and they should not have to query the table to notice one.
            Logger.WriteLog(LogType.Command,
                $"Petition #{petitionId} ({type}) from {client.Player.FamilyName} "
                + $"[account {accountId}, character {client.Player.Id}] on map {client.Player.MapContextId}: {summary}");

            Ack(client, true, petitionId);
        }

        private static void Ack(Client client, bool success, uint petitionId)
        {
            client.CallMethod(SysEntity.ClientPetitionManagerId, new CreatePetitionAckPacket(success, petitionId));
        }

        private bool OnCooldown(uint accountId)
        {
            return _lastFiled.TryGetValue(accountId, out var last)
                   && (DateTime.UtcNow - last).TotalSeconds < CooldownSeconds;
        }

        /// <summary>
        /// A null arrives when the client marshals an empty edit box as None rather than as a
        /// zero-length string, and both columns are NOT NULL.
        /// </summary>
        private static string Clamp(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}

namespace Rasa.Structures
{
    using Game;

    /// <summary>
    /// One trade between two players, from the invite until it completes or is cancelled.
    /// </summary>
    public class TradeSession
    {
        public Client Initiator { get; }
        public Client Target { get; }

        /// <summary>False while the invite is pending; true once the trade window is open.</summary>
        public bool Accepted { get; set; }

        /// <summary>
        /// Bumped on every change to the terms. The client echoes the last one it saw in
        /// RequestConfirmTrade, so a confirmation that raced a change is refused rather than
        /// accepted for terms the player never looked at.
        /// </summary>
        public int ConfigurationId { get; set; }

        public int InitiatorCredits { get; set; }
        public int TargetCredits { get; set; }
        public bool InitiatorConfirmed { get; set; }
        public bool TargetConfirmed { get; set; }

        /// <summary>Environment.TickCount64 when the invite was sent.</summary>
        public long CreatedTick { get; } = System.Environment.TickCount64;

        public TradeSession(Client initiator, Client target)
        {
            Initiator = initiator;
            Target = target;
        }

        public bool IsInitiator(Client client) => client == Initiator;

        public Client PartnerOf(Client client) => client == Initiator ? Target : Initiator;

        public int CreditsOf(Client client) => client == Initiator ? InitiatorCredits : TargetCredits;

        public void SetCredits(Client client, int amount)
        {
            if (client == Initiator)
                InitiatorCredits = amount;
            else
                TargetCredits = amount;
        }

        public bool IsConfirmed(Client client) => client == Initiator ? InitiatorConfirmed : TargetConfirmed;

        public void SetConfirmed(Client client, bool confirmed)
        {
            if (client == Initiator)
                InitiatorConfirmed = confirmed;
            else
                TargetConfirmed = confirmed;
        }
    }
}

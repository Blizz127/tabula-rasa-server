using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The auction house: listing, browsing, buying out, cancelling and expiry.
    ///
    /// Every reply is the one the client's own handlers expect (client/auctionhouse.pyo, recovered in the client
    /// protocol inventory): AuctionCreationSuccess(itemId) / CreationFailed(itemId, message), QuerySuccess(itemList) /
    /// QueryFailed(message), AuctionStatusSuccess(itemList) / StatusFailed(message), AuctionBuyoutSuccess(itemId) /
    /// BuyoutFailed(itemId, message), CancelAuctionSuccess(itemId) / CancelFailed(itemId, message),
    /// AuctionSold(itemId, price) and AuctionExpired(itemId).
    ///
    /// The listing parameters are the client's own data as well (<see cref="AuctionHouseData"/>): the four durations
    /// with the deposit each costs (5%, 10%, 20%, 25%) and the 83 category rows the browse window filters by, matched
    /// to an item through its class name.
    ///
    /// Listings live in memory for the session. The original kept them in tables of its own and none survived
    /// (GAP-W3-AUCTION-PERSISTENCE), so a restart ends every auction - stated rather than hidden. The same gap covers
    /// the two things the original surely also had and no source shows: the commission a seller pays, and the pickup
    /// window for a sold or expired item. This implementation pays the seller the full buyout price and puts a sold or
    /// expired item straight into its owner's inventory.
    /// </summary>
    public class AuctionHouseManager
    {
        private static AuctionHouseManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// How long a listing lasts, by the index the create window sends. Four entries, matching the client's four
        /// duration rows (client/auctionhouse.pyo durationdata).
        /// </summary>
        private static readonly uint[] DurationHours = { 12u, 24u, 48u, 72u };

        /// <summary>Auctions and vendors are both used from arm's length; shared/gameconstants.py INTERACTION_RANGE.</summary>
        public const float InteractionRange = 5.0f;

        /// <summary>The listing a uint id names.</summary>
        private readonly Dictionary<uint, Listing> _listings = new Dictionary<uint, Listing>();

        /// <summary>The listing an item entity id belongs to, so cancel (which names the item) can find it.</summary>
        private readonly Dictionary<ulong, uint> _listingByItem = new Dictionary<ulong, uint>();

        private uint _nextListingId = 1;

        private sealed class Listing
        {
            public uint Id;
            public ulong ItemEntityId;
            public uint SellerCharacterId;
            public string SellerName;
            public uint ItemTemplateId;
            public uint StackSize;
            public uint Price;
            public uint DepositPaid;
            public long ExpiresAtMs;
            public uint QualityId;
            public uint LevelRequirement;
            public uint CategoryBit;
            public uint[] ModuleIds = new uint[4];
        }

        public static AuctionHouseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new AuctionHouseManager();
                    }
                }

                return _instance;
            }
        }

        private AuctionHouseManager()
        {
        }

        #region Handlers

        /// <summary>The player's own listings, which the window shows in its "your auctions" tab.</summary>
        public void RequestAuctionStatus(Client client, RequestAuctionStatusPacket packet)
        {
            if (!IsAuctioneer(client, packet.EntityId))
                return;

            var player = client.Player;
            var mine = _listings.Values.Where(listing => listing.SellerCharacterId == player.Id).OrderBy(listing => listing.Id);

            var reply = new AuctionStatusSuccessPacket();
            foreach (var listing in mine)
                reply.AuctionItemList.Add(ToWire(listing));

            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, reply);
        }

        /// <summary>Browsing: RequestQueryAuctions(auctioneerId, minLevel, maxLevel, qualityId, categoryId).</summary>
        public void RequestQueryAuctions(Client client, RequestQueryAuctionsPacket packet)
        {
            if (!IsAuctioneer(client, packet.EntityId))
                return;

            ExpireDue();

            var reply = new QuerySuccessPacket();

            foreach (var listing in _listings.Values.OrderBy(listing => listing.Id))
            {
                // 0 in any field is the client's "no filter".
                if (packet.CategoryId != 0 && listing.CategoryBit != packet.CategoryId)
                    continue;

                if (packet.QualityId != 0 && listing.QualityId != packet.QualityId)
                    continue;

                if (packet.MinLevel != 0 && listing.LevelRequirement < packet.MinLevel)
                    continue;

                if (packet.MaxLevel != 0 && listing.LevelRequirement > packet.MaxLevel)
                    continue;

                reply.AuctionItemList.Add(ToWire(listing));
            }

            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, reply);
        }

        /// <summary>
        /// Lists an item: RequestCreateAuction(auctioneerId, itemId, price, duration). The duration is the window's
        /// index into the client's duration rows, and its deposit is a percentage of the price.
        /// </summary>
        public void RequestCreateAuction(Client client, RequestCreateAuctionPacket packet)
        {
            if (!IsAuctioneer(client, packet.EntityId))
                return;

            var player = client.Player;

            if (packet.Duration >= DurationHours.Length)
            {
                CreationFailed(client, packet.ItemEntityId, PlayerMessage.PmAuctionInternalError);
                return;
            }

            if (packet.Price == 0)
            {
                CreationFailed(client, packet.ItemEntityId, PlayerMessage.PmAuctionInvalidPriceSet);
                return;
            }

            var item = EntityManager.Instance.GetItem(packet.ItemEntityId);

            // The item has to be one this player is carrying. An entity id the client was merely shown - another
            // player's rifle, a corpse's loot - must not be listable, which is the same test the vendor purchase makes.
            if (item?.ItemTemplate == null || item.OwnerId != player.Id || SlotOf(player, packet.ItemEntityId) < 0)
            {
                CreationFailed(client, packet.ItemEntityId, PlayerMessage.PmAuctionCouldNotFindItem);
                return;
            }

            var template = item.ItemTemplate;

            if (!template.HasSellableFlag || template.BoundToCharacter || template.HasCharacterUniqueFlag)
            {
                CreationFailed(client, packet.ItemEntityId, PlayerMessage.PmAuctionItemCannotBeAuctioned);
                return;
            }

            if (_listingByItem.ContainsKey(packet.ItemEntityId))
            {
                CreationFailed(client, packet.ItemEntityId, PlayerMessage.PmAuctionPendingTransaction);
                return;
            }

            var deposit = DepositFor(packet.Price, packet.Duration);

            if (player.Credits[CurencyType.Credits] < deposit)
            {
                CreationFailed(client, packet.ItemEntityId, PlayerMessage.PmAuctionNotEnoughCreditsForDeposit);
                return;
            }

            var slot = SlotOf(player, packet.ItemEntityId);

            // The item leaves the inventory and the deposit is taken before the listing exists: a failure after this
            // point cannot leave a listing for an item the player still holds.
            InventoryManager.Instance.RemoveItemBySlot(client, InventoryType.Personal, (uint)slot);
            ManifestationManager.Instance.LossCredits(client, -(int)deposit);

            var listing = new Listing
            {
                Id = _nextListingId++,
                ItemEntityId = packet.ItemEntityId,
                SellerCharacterId = player.Id,
                SellerName = player.Name,
                ItemTemplateId = template.ItemTemplateId,
                StackSize = item.StackSize,
                Price = packet.Price,
                DepositPaid = deposit,
                ExpiresAtMs = NowMs() + DurationHours[packet.Duration] * 3600000L,
                QualityId = (uint)template.QualityId,
                LevelRequirement = template.ItemInfo != null && template.ItemInfo.Requirements.TryGetValue(RequirementsType.ReqXpLevel, out var level)
                    ? (uint)level
                    : 0u,
                CategoryBit = AuctionHouseData.CategoryOf(ClassNameOf(template)),
                ModuleIds = new uint[4]
            };

            _listings[listing.Id] = listing;
            _listingByItem[listing.ItemEntityId] = listing.Id;

            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionCreationSuccessPacket(packet.ItemEntityId));
        }

        /// <summary>
        /// Buys a listing out: RequestAuctionBuyout(auctioneerId, itemId, price). The price the client sent has to be
        /// the listing's own, so a client that understates it is refused rather than obeyed.
        /// </summary>
        public void RequestAuctionBuyout(Client client, RequestAuctionBuyoutPacket packet)
        {
            if (!IsAuctioneer(client, packet.EntityId))
                return;

            ExpireDue();

            var player = client.Player;

            if (!_listings.TryGetValue(packet.ItemId, out var listing))
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new AuctionBuyoutFailedPacket(packet.ItemId, PlayerMessage.PmAuctionItemNotFound));
                return;
            }

            if (listing.SellerCharacterId == player.Id)
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new AuctionBuyoutFailedPacket(packet.ItemId, PlayerMessage.PmAuctionItemCannotBeAuctioned));
                return;
            }

            if (packet.Price != listing.Price)
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new AuctionBuyoutFailedPacket(packet.ItemId, PlayerMessage.PmAuctionInvalidPriceSet));
                return;
            }

            if (player.Credits[CurencyType.Credits] < listing.Price)
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new AuctionBuyoutFailedPacket(packet.ItemId, PlayerMessage.PmAuctionInsufficientFunds));
                return;
            }

            var item = EntityManager.Instance.GetItem(listing.ItemEntityId);

            if (item == null)
            {
                // The goods are gone: the listing is void and the buyer pays nothing.
                RemoveListing(listing);
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new AuctionBuyoutFailedPacket(packet.ItemId, PlayerMessage.PmAuctionItemNotFound));
                return;
            }

            // Hand the item over first: if the buyer has no room the sale does not happen and nobody is charged.
            var placed = InventoryManager.Instance.AddItemToInventory(client, item);

            if (placed == null)
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new AuctionBuyoutFailedPacket(packet.ItemId, PlayerMessage.PmAuctionNoBuyoutInboxFull));
                return;
            }

            ManifestationManager.Instance.LossCredits(client, -(int)listing.Price);

            // The seller is paid if they are online to be paid; the client shows the sale through AuctionSold.
            var seller = FindClient(listing.SellerCharacterId);
            if (seller != null)
            {
                ManifestationManager.Instance.GainCredits(seller, (int)listing.Price);
                seller.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionSoldPacket(listing.Id, listing.Price));
            }

            RemoveListing(listing);
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionBuyoutSuccessPacket(listing.Id));
        }

        /// <summary>
        /// Takes a listing back: RequestCancelAuction(auctioneerId, itemId) names the item, not the listing, so the
        /// lookup goes through the item.
        /// </summary>
        public void RequestCancelAuction(Client client, RequestCancelAuctionPacket packet)
        {
            if (!IsAuctioneer(client, packet.EntityId))
                return;

            var player = client.Player;

            if (!_listingByItem.TryGetValue(packet.ItemEntityId, out var listingId) || !_listings.TryGetValue(listingId, out var listing))
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new CancelAuctionFailedPacket((uint)packet.ItemEntityId, PlayerMessage.PmAuctionItemNotFound));
                return;
            }

            if (listing.SellerCharacterId != player.Id)
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new CancelAuctionFailedPacket(listing.Id, PlayerMessage.PmAuctionItemNotFound));
                return;
            }

            var item = EntityManager.Instance.GetItem(listing.ItemEntityId);

            if (item == null || InventoryManager.Instance.AddItemToInventory(client, item) == null)
            {
                client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                    new CancelAuctionFailedPacket(listing.Id, PlayerMessage.PmAuctionNoCreationInboxFull));
                return;
            }

            RemoveListing(listing);
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new CancelAuctionSuccessPacket(listing.Id));
        }

        /// <summary>
        /// The client asks to cancel every one of its auctions when the window closes (RequestCancelAuctioneer), which
        /// cancels nothing: it is how the client leaves the house, and the original's own auctions survive it.
        /// </summary>
        public void RequestCancelAuctioneer(Client client)
        {
            if (!MissionManager.IsInWorld(client))
                return;

            Logger.WriteLog(LogType.Debug, $"{client.Player.Name} left the auction house; listings are unaffected.");
        }

        #endregion

        #region Worker

        /// <summary>
        /// Expires the listings whose duration has run out and returns each item to its seller when they are online.
        /// Called by the map channel worker and before every browse, so an expired listing is never sold.
        /// </summary>
        public void ExpireDue()
        {
            var now = NowMs();

            foreach (var listing in _listings.Values.Where(listing => listing.ExpiresAtMs <= now).ToList())
            {
                var item = EntityManager.Instance.GetItem(listing.ItemEntityId);
                var seller = FindClient(listing.SellerCharacterId);

                RemoveListing(listing);

                if (seller == null || item == null)
                    continue;

                if (InventoryManager.Instance.AddItemToInventory(seller, item) != null)
                    seller.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionExpiredPacket(listing.Id));
            }
        }

        /// <summary>How many listings a character currently has, for the GM command and the tests.</summary>
        public int ListingCount(uint characterId)
            => _listings.Values.Count(listing => listing.SellerCharacterId == characterId);

        /// <summary>The four durations' deposits, from the client's own duration rows.</summary>
        public static uint DepositFor(uint price, uint durationIndex)
        {
            if (durationIndex >= AuctionHouseData.Durations.Length)
                return 0;

            return (uint)((long)price * AuctionHouseData.Durations[durationIndex].DepositPercent / 100);
        }

        #endregion

        #region Helper Functions

        private void RemoveListing(Listing listing)
        {
            _listings.Remove(listing.Id);
            _listingByItem.Remove(listing.ItemEntityId);
        }

        private AuctionItem ToWire(Listing listing) => new AuctionItem
        {
            ItemId = listing.Id,
            Sellername = listing.SellerName,
            BidPrice = 0,
            BuyoutPrice = listing.Price,
            RemainingDuration = (uint)Math.Max(0, (listing.ExpiresAtMs - NowMs()) / 3600000L),
            ItemTemplateId = listing.ItemTemplateId,
            StackSize = listing.StackSize,
            LootModuleId1 = listing.ModuleIds[0],
            LootModuleId2 = listing.ModuleIds[1],
            LootModuleId3 = listing.ModuleIds[2],
            LootModuleId4 = listing.ModuleIds[3],
            QualitiId = listing.QualityId,
            LevelRequirement = listing.LevelRequirement
        };

        private static void CreationFailed(Client client, ulong itemId, PlayerMessage message)
            => client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionCreationFailedPacket((uint)itemId, message));

        /// <summary>
        /// The class name an item is categorised by. The loaded entity classes carry no name string, but the
        /// entity class enum members are written the way the client's category names are
        /// ("Weapon_Avatar_Pistol_Physical_UNC_01_to_04" starts with "Weapon_Pistol"), which is what
        /// AuctionHouseData.CategoryOf matches on.
        /// </summary>
        private static string ClassNameOf(ItemTemplate template) => template.Class.ToString();

        /// <summary>An auctioneer, standing close enough to use - the same test a vendor gets.</summary>
        public static bool IsAuctioneer(Client client, ulong entityId)
        {
            if (client?.Player == null || !MissionManager.IsInWorld(client))
                return false;

            var creature = EntityManager.Instance.GetCreature(entityId);

            if (creature?.Npc == null || !creature.Npc.NpcIsAuctioneer)
                return false;

            var delta = creature.Position - client.Player.Position;
            return delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z <= InteractionRange * InteractionRange;
        }

        private static int SlotOf(Manifestation player, ulong entityId)
        {
            for (var slot = 0; slot < player.Inventory.PersonalInventory.Count; slot++)
                if (player.Inventory.PersonalInventory[slot] == entityId)
                    return slot;

            return -1;
        }

        private static Client FindClient(uint characterId)
            => Server.Clients.FirstOrDefault(other => other?.Player != null && other.Player.Id == characterId);

        private static long NowMs() => Environment.TickCount64;

        #endregion
    }
}

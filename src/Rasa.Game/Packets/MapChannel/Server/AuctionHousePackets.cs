using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// The auction house replies the client's own handlers expect, with the argument lists recovered from
    /// client/auctionhouse.pyo (client-protocol-inventory.json):
    ///
    ///   AuctionCreationSuccess(itemId)                       AuctionCreationFailed(itemId, playerMessageId)
    ///   QuerySuccess(dataList)                               QueryFailed(playerMessageId)
    ///   AuctionStatusSuccess(itemList)                       AuctionStatusFailed(playerMessageId)
    ///   AuctionBuyoutSuccess(itemId)                         AuctionBuyoutFailed(itemId, playerMessageId)
    ///   CancelAuctionSuccess(itemId)                         CancelAuctionFailed(itemId, playerMessageId)
    ///   AuctionSold(itemId, price)                           AuctionExpired(itemId)
    ///
    /// CreationSuccess, CreationFailed, QuerySuccess and QueryFailed already exist; these are the rest. Only the reply
    /// the client was written against is sent, rather than a guess at what an auction house "should" say.
    /// </summary>
    public class AuctionBuyoutSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionBuyoutSuccess;

        public uint ItemId { get; }

        public AuctionBuyoutSuccessPacket(uint itemId) => ItemId = itemId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ItemId);
        }
    }

    public class AuctionBuyoutFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionBuyoutFailed;

        public uint ItemId { get; }
        public PlayerMessage PlayerMessageId { get; }

        public AuctionBuyoutFailedPacket(uint itemId, PlayerMessage playerMessageId)
        {
            ItemId = itemId;
            PlayerMessageId = playerMessageId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(ItemId);
            pw.WriteUInt((uint)PlayerMessageId);
        }
    }

    public class CancelAuctionSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CancelAuctionSuccess;

        public uint ItemId { get; }

        public CancelAuctionSuccessPacket(uint itemId) => ItemId = itemId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ItemId);
        }
    }

    public class CancelAuctionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CancelAuctionFailed;

        public uint ItemId { get; }
        public PlayerMessage PlayerMessageId { get; }

        public CancelAuctionFailedPacket(uint itemId, PlayerMessage playerMessageId)
        {
            ItemId = itemId;
            PlayerMessageId = playerMessageId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(ItemId);
            pw.WriteUInt((uint)PlayerMessageId);
        }
    }

    public class AuctionStatusSuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionStatusSuccess;

        public List<AuctionItem> AuctionItemList = new List<AuctionItem>();

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(AuctionItemList.Count);
            foreach (var auctionItem in AuctionItemList)
                pw.WriteStruct(auctionItem);
        }
    }

    public class AuctionStatusFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionStatusFailed;

        public PlayerMessage PlayerMessageId { get; }

        public AuctionStatusFailedPacket(PlayerMessage playerMessageId) => PlayerMessageId = playerMessageId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt((uint)PlayerMessageId);
        }
    }

    public class AuctionSoldPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionSold;

        public uint ItemId { get; }
        public uint Price { get; }

        public AuctionSoldPacket(uint itemId, uint price)
        {
            ItemId = itemId;
            Price = price;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(ItemId);
            pw.WriteUInt(Price);
        }
    }

    public class AuctionExpiredPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AuctionExpired;

        public uint ItemId { get; }

        public AuctionExpiredPacket(uint itemId) => ItemId = itemId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ItemId);
        }
    }
}

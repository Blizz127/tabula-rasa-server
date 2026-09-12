namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class RequestPerformAbilityPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestPerformAbility;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public ulong? Target { get; set; }
        public (double X, double Y, double Z)? TargetLocation { get; set; }
        public ulong? ItemId { get; set; }
        public double? ClientYaw { get; set; }

        public override void Read(PythonReader pr)
        {
            var count = pr.ReadTuple();
            if (count != 4 && count != 5)
                throw new InvalidClientMessageException();
            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = pr.ReadInt();
            Target = null;
            TargetLocation = null;
            ClientYaw = null;
            if (pr.PeekType() == PythonType.Tuple || pr.PeekType() == PythonType.List)
            {
                var coordinates = pr.PeekType() == PythonType.Tuple ? pr.ReadTuple() : pr.ReadList();
                if (coordinates != 3)
                    throw new InvalidClientMessageException();
                TargetLocation = (ReadCoordinate(pr), ReadCoordinate(pr), ReadCoordinate(pr));
            }
            else
                Target = ReadOptionalId(pr);
            ItemId = ReadOptionalId(pr);
            if (count == 5)
                ClientYaw = ReadCoordinate(pr);
        }

        private static ulong? ReadOptionalId(PythonReader pr)
        {
            if (pr.PeekType() == PythonType.Structs)
            {
                pr.ReadNoneStruct();
                return null;
            }
            if (pr.PeekType() == PythonType.Long)
                return pr.ReadULong();
            if (pr.PeekType() == PythonType.Int)
                return pr.ReadUInt();
            throw new InvalidClientMessageException();
        }

        private static double ReadCoordinate(PythonReader pr)
        {
            var value = pr.PeekType() == PythonType.Int ? pr.ReadInt() : pr.ReadDouble();
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidClientMessageException();
            return value;
        }
    }
}

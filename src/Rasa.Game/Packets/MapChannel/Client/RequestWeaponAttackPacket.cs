namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class RequestWeaponAttackPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestWeaponAttack;

        public ActionId ActionId { get; set; }
        public int ActionArgId { get; set; }
        public ulong? TargetId { get; set; }
        public (double X, double Y, double Z)? TargetLocation { get; set; }
        public bool IsAltAction { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 4)
                throw new InvalidClientMessageException();
            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = pr.ReadInt();
            TargetId = null;
            TargetLocation = null;
            switch (pr.PeekType())
            {
                case PythonType.Structs:
                    pr.ReadNoneStruct();
                    break;
                case PythonType.Int:
                    TargetId = pr.ReadUInt();
                    break;
                case PythonType.Long:
                    TargetId = pr.ReadULong();
                    break;
                case PythonType.Tuple:
                case PythonType.List:
                    var count = pr.PeekType() == PythonType.Tuple ? pr.ReadTuple() : pr.ReadList();
                    if (count != 3)
                        throw new InvalidClientMessageException();
                    TargetLocation = (ReadCoordinate(pr), ReadCoordinate(pr), ReadCoordinate(pr));
                    break;
                default:
                    throw new InvalidClientMessageException();
            }
            IsAltAction = pr.ReadBool();
        }

        private static double ReadCoordinate(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Int && pr.PeekType() != PythonType.Double)
                throw new InvalidClientMessageException();
            var value = pr.PeekType() == PythonType.Int ? pr.ReadInt() : pr.ReadDouble();
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidClientMessageException();
            return value;
        }
    }
}

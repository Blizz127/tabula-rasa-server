using System.Numerics;

namespace Rasa.Structures
{
    using Memory;

    // Original shared/graveyardinfo.py dictionary. Id is the client hospital
    // identifier, not the primary key of an emulator teleporter row.
    public sealed class GraveyardInfo
    {
        public int Id { get; }
        public Vector3 Position { get; }
        public bool IsSafe { get; }

        public GraveyardInfo(int id, Vector3 position, bool isSafe)
        {
            Id = id;
            Position = position;
            IsSafe = isSafe;
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteDictionary(4);
            pw.WriteString("Id");
            pw.WriteInt(Id);
            pw.WriteString("pos");
            pw.WriteTuple(3);
            pw.WriteDouble(Position.X);
            pw.WriteDouble(Position.Y);
            pw.WriteDouble(Position.Z);
            pw.WriteString("isSafe");
            pw.WriteInt(IsSafe ? 1 : 0);
            pw.WriteString("name");
            // The constructor permits None; Actor.OnShowReviveAbort replaces
            // this value with graveyardlanguage[Id] before displaying it.
            pw.WriteNoneStruct();
        }
    }
}

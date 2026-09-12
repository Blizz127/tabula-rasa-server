using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public sealed class PlayerDeadPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PlayerDead;

        public ulong SourceId { get; }
        public bool CanRevive { get; }
        private readonly GraveyardInfo[] _graveyards;

        public PlayerDeadPacket(ulong sourceId, IEnumerable<GraveyardInfo> graveyards, bool canRevive = false)
        {
            if (graveyards == null)
                throw new ArgumentNullException(nameof(graveyards));
            _graveyards = graveyards.ToArray();
            if (_graveyards.Any(graveyard => graveyard == null))
                throw new ArgumentException("Hospital entries must be dictionaries, not None.", nameof(graveyards));
            SourceId = sourceId;
            CanRevive = canRevive;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteULong(SourceId);
            // Recv_PlayerDead iterates this argument even when it is empty.
            pw.WriteList(_graveyards.Length);
            foreach (var graveyard in _graveyards)
                graveyard.Write(pw);
            pw.WriteInt(CanRevive ? 1 : 0);
        }
    }
}

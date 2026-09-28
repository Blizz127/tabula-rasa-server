namespace Rasa.Packets.Party
{
    using Memory;

    internal static class PartyArgs
    {
        /// <summary>
        /// A user id from the party window. partystatuswindow.py wraps it in long() before
        /// sending, so it normally arrives as 0x2_; a console call would send an int.
        /// Anything that is not a valid uint reads as 0, which matches no member.
        /// </summary>
        public static uint ReadUserId(PythonReader pr)
        {
            long value;

            switch (pr.PeekType())
            {
                case PythonType.Long:
                    value = pr.ReadLong();
                    break;
                case PythonType.Structs:
                    pr.ReadUnkStruct();
                    return 0;
                default:
                    value = pr.ReadInt();
                    break;
            }

            // The client keeps the id as a signed 32-bit int (WriteUInt sends ids above int.MaxValue as 0x1F), so
            // an account id above 2147483647 - the DIT bots' 42000000xx - comes back negative, even after long().
            if (value < 0 && value >= int.MinValue)
                value = unchecked((uint)(int)value);

            return value > 0 && value <= uint.MaxValue ? (uint)value : 0;
        }

        /// <summary>A name typed into a slash command (str) or a text box (unicode).</summary>
        public static string ReadName(PythonReader pr)
        {
            var name = pr.PeekType() == PythonType.UnicodeString ? pr.ReadUnicodeString() : pr.ReadString();

            return name?.Trim() ?? string.Empty;
        }
    }
}

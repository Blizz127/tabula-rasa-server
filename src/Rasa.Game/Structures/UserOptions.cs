namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class UserOptions : IPythonDataStruct
    {
        public UserOption OptionId { get; set; }
        public string Value { get; set; }

        public UserOptions()
        {
        }

        public UserOptions(UserOption optionId, string value)
        {
            OptionId = optionId;
            Value = value;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            OptionId = (UserOption)pr.ReadUInt();
            // The client sends an unbound key (and any cleared option) as the empty unicode string, 0x50,
            // which ReadUnicodeString returns as null; user_option.value is NOT NULL, so keep it empty.
            Value = pr.ReadUnicodeString() ?? string.Empty;
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt((uint)OptionId);
            pw.WriteUnicodeString(Value);
        }
    }
}

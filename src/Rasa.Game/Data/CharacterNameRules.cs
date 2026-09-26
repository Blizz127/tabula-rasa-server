namespace Rasa.Data
{
    /// <summary>Creation error text from the original client; see docs/character-name-evidence.md.</summary>
    public static class CharacterNameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 20;

        public static CreateCharacterResult Validate(string name)
        {
            if (name == null)
                return CreateCharacterResult.InvalidEncoding;
            if (name.Length < MinLength)
                return CreateCharacterResult.NameTooShort;
            if (name.Length > MaxLength)
                return CreateCharacterResult.NameTooLong;
            if (!char.IsUpper(name[0]))
                return CreateCharacterResult.NameFormatInvalid;

            for (var i = 0; i < name.Length; i++)
            {
                if (!char.IsLetter(name[i]))
                    return CreateCharacterResult.NameFormatInvalid;
                if (i >= 2 && char.ToUpperInvariant(name[i]) == char.ToUpperInvariant(name[i - 1]) &&
                    char.ToUpperInvariant(name[i]) == char.ToUpperInvariant(name[i - 2]))
                    return CreateCharacterResult.NameFormatInvalid;
            }
            return CreateCharacterResult.Success;
        }
    }
}

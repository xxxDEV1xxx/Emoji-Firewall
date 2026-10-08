using System.Text;

public sealed class EmojiFirewallEngine
{
    private readonly EmojiCatalog _catalog;

    public EmojiFirewallEngine(EmojiCatalog catalog)
    {
        _catalog = catalog;
    }

    public string Transform(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var output = new StringBuilder();

        int index = 0;

        while (index < input.Length)
        {
            if (TryMatchEmoji(input, index, out EmojiEntry? entry, out int length))
            {
                output.Append('[');
                output.Append(entry!.Name);
                output.Append(']');

                index += length;
                continue;
            }

            Rune rune = Rune.GetRuneAt(input, index);

            if (IsEmojiLike(rune))
            {
                output.Append("[UNRECOGNIZED EMOJI]");
                index += rune.Utf16SequenceLength;
                continue;
            }

            output.Append(rune.ToString());
            index += rune.Utf16SequenceLength;
        }

        return output.ToString();
    }

    private bool TryMatchEmoji(
        string input,
        int start,
        out EmojiEntry? entry,
        out int length)
    {
        entry = null;
        length = 0;

        int remaining = input.Length - start;

        /*
         * Longest-match-first is mandatory.
         *
         * This prevents:
         *
         *   👨‍👩‍👧‍👦
         *
         * from being decomposed into smaller emoji.
         */
        for (int candidateLength = remaining;
             candidateLength > 0;
             candidateLength--)
        {
            string candidate =
                input.Substring(start, candidateLength);

            if (_catalog.TryGet(candidate, out EmojiEntry found))
            {
                entry = found;
                length = candidateLength;
                return true;
            }
        }

        return false;
    }

    private static bool IsEmojiLike(Rune rune)
    {
        int value = rune.Value;

        /*
         * Regional indicators.
         */
        if (value >= 0x1F1E6 && value <= 0x1F1FF)
            return true;

        /*
         * Supplemental Symbols and Pictographs.
         */
        if (value >= 0x1F900 && value <= 0x1FAFF)
            return true;

        /*
         * Miscellaneous Symbols and Dingbats.
         */
        if (value >= 0x2600 && value <= 0x27BF)
            return true;

        /*
         * Miscellaneous Symbols.
         */
        if (value >= 0x2300 && value <= 0x23FF)
            return true;

        /*
         * Enclosed alphanumerics / symbols frequently used
         * by emoji sequences.
         */
        if (value >= 0x2B00 && value <= 0x2BFF)
            return true;

        /*
         * Keycap combining mark.
         */
        if (value == 0x20E3)
            return true;

        /*
         * Variation selector-16.
         */
        if (value == 0xFE0F)
            return true;

        /*
         * Emoji skin-tone modifiers.
         */
        if (value >= 0x1F3FB && value <= 0x1F3FF)
            return true;

        /*
         * Zero-width joiner.
         */
        if (value == 0x200D)
            return true;

        return false;
    }
}

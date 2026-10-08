using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

internal static class BuildEmojiCatalog
{
    private sealed record EmojiEntry(
        [property: JsonPropertyName("sequence")]
        string Sequence,

        [property: JsonPropertyName("name")]
        string Name,

        [property: JsonPropertyName("status")]
        string Status,

        [property: JsonPropertyName("type")]
        string Type,

        [property: JsonPropertyName("emojiVersion")]
        string EmojiVersion
    );

    private sealed record SourceRecord(
        string Sequence,
        string Name,
        string Status,
        string Type,
        string EmojiVersion
    );

    private static void Main(string[] args)
    {
        Console.WriteLine("UNICODE EMOJI CATALOG BUILDER");
        Console.WriteLine("============================");
        Console.WriteLine();

        string inputDirectory =
            args.Length > 0
                ? Path.GetFullPath(args[0])
                : Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "list");

        Console.WriteLine($"Input directory: {inputDirectory}");
        Console.WriteLine();

        string[] required =
        {
            "emoji-test.txt",
            "emoji-sequences.txt",
            "emoji-zwj-sequences.txt",
            "emoji-data.txt"
        };

        foreach (string file in required)
        {
            string path = Path.Combine(inputDirectory, file);

            if (!File.Exists(path))
            {
                Console.WriteLine($"ERROR: Missing {file}");
                Environment.Exit(1);
            }
        }

        string emojiTest =
            File.ReadAllText(
                Path.Combine(inputDirectory, "emoji-test.txt"),
                Encoding.UTF8);

        string emojiSequences =
            File.ReadAllText(
                Path.Combine(inputDirectory, "emoji-sequences.txt"),
                Encoding.UTF8);

        string emojiZwj =
            File.ReadAllText(
                Path.Combine(inputDirectory, "emoji-zwj-sequences.txt"),
                Encoding.UTF8);

        string emojiData =
            File.ReadAllText(
                Path.Combine(inputDirectory, "emoji-data.txt"),
                Encoding.UTF8);

        string testVersion = ExtractVersion(emojiTest);
        string sequenceVersion = ExtractVersion(emojiSequences);
        string zwjVersion = ExtractVersion(emojiZwj);
        string dataVersion = ExtractVersion(emojiData);

        Console.WriteLine($"emoji-test.txt           : {testVersion}");
        Console.WriteLine($"emoji-sequences.txt     : {sequenceVersion}");
        Console.WriteLine($"emoji-zwj-sequences.txt : {zwjVersion}");
        Console.WriteLine($"emoji-data.txt           : {dataVersion}");
        Console.WriteLine();

        if (!string.Equals(
                testVersion,
                sequenceVersion,
                StringComparison.Ordinal) ||
            !string.Equals(
                testVersion,
                zwjVersion,
                StringComparison.Ordinal))
        {
            Console.WriteLine(
                "ERROR: Core Emoji data files have mismatched versions.");

            Environment.Exit(2);
        }

        Console.WriteLine(
            $"Catalog Unicode Emoji version: {testVersion}");

        Console.WriteLine();

        var entries =
            new Dictionary<string, EmojiEntry>(
                StringComparer.Ordinal);

        ParseEmojiTest(
            emojiTest,
            testVersion,
            entries);

        int afterTest = entries.Count;

        ParseSequenceFile(
            emojiSequences,
            testVersion,
            entries);

        int afterSequences = entries.Count;

        ParseZwjFile(
            emojiZwj,
            testVersion,
            entries);

        int afterZwj = entries.Count;

        Console.WriteLine(
            $"emoji-test entries       : {afterTest:N0}");

        Console.WriteLine(
            $"sequence additions       : {(afterSequences - afterTest):N0}");

        Console.WriteLine(
            $"ZWJ additions             : {(afterZwj - afterSequences):N0}");

        Console.WriteLine();

        Console.WriteLine(
            $"TOTAL UNIQUE CATALOG ENTRIES: {entries.Count:N0}");

        Console.WriteLine();

        string projectDirectory =
            Directory.GetParent(
                Directory.GetCurrentDirectory())!.FullName;

        string outputDirectory =
            Path.Combine(
                projectDirectory,
                "EmojiFirewall",
                "Catalog");

        Directory.CreateDirectory(outputDirectory);

        string output =
            Path.Combine(
                outputDirectory,
                "emoji-catalog.json");

        var catalog = new
        {
            unicodeVersion = testVersion,
            generatedUtc = DateTime.UtcNow,
            sourceFiles = new
            {
                emojiTestVersion = testVersion,
                emojiSequencesVersion = sequenceVersion,
                emojiZwjSequencesVersion = zwjVersion,
                emojiDataVersion = dataVersion
            },
            sequenceCount = entries.Count,
            entries = entries.Values
                .OrderBy(
                    x => x.Sequence,
                    StringComparer.Ordinal)
                .ToArray()
        };

        var options =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

        string json =
            JsonSerializer.Serialize(
                catalog,
                options);

        File.WriteAllText(
            output,
            json,
            new UTF8Encoding(false));

        Console.WriteLine("Catalog written:");
        Console.WriteLine(output);
        Console.WriteLine();
        Console.WriteLine("BUILD COMPLETE.");
    }

    private static void ParseEmojiTest(
        string contents,
        string version,
        Dictionary<string, EmojiEntry> entries)
    {
        foreach (string raw in contents.Split('\n'))
        {
            string line = raw.Trim();

            if (line.Length == 0 ||
                line.StartsWith('#'))
            {
                continue;
            }

            int hash = line.IndexOf('#');

            string comment =
                hash >= 0
                    ? line[(hash + 1)..].Trim()
                    : "";

            string data =
                hash >= 0
                    ? line[..hash].Trim()
                    : line;

            string[] fields =
                data.Split(
                    ';',
                    StringSplitOptions.TrimEntries);

            if (fields.Length < 2)
                continue;

            string sequence =
                CodePointsToString(fields[0]);

            if (sequence.Length == 0)
                continue;

            string status = fields[1];

            string name =
                ExtractEmojiTestName(comment);

            string type =
                DetermineEmojiTestType(
                    sequence,
                    status);

            AddOrReplace(
                entries,
                new SourceRecord(
                    sequence,
                    name,
                    status,
                    type,
                    version));
        }
    }

    private static void ParseSequenceFile(
        string contents,
        string version,
        Dictionary<string, EmojiEntry> entries)
    {
        foreach (string raw in contents.Split('\n'))
        {
            string line = raw.Trim();

            if (line.Length == 0 ||
                line.StartsWith('#'))
            {
                continue;
            }

            int hash = line.IndexOf('#');

            string comment =
                hash >= 0
                    ? line[(hash + 1)..].Trim()
                    : "";

            string data =
                hash >= 0
                    ? line[..hash].Trim()
                    : line;

            string[] fields =
                data.Split(
                    ';',
                    StringSplitOptions.TrimEntries);

            if (fields.Length < 3)
                continue;

            string type = fields[1];

            string sequence =
                CodePointsToString(fields[0]);

            if (sequence.Length == 0)
                continue;

            string name =
                fields[2].Trim();

            if (name.Length == 0)
                name = ExtractCommentName(comment);

            AddOrReplace(
                entries,
                new SourceRecord(
                    sequence,
                    name,
                    "RGI",
                    type,
                    version));
        }
    }

    private static void ParseZwjFile(
        string contents,
        string version,
        Dictionary<string, EmojiEntry> entries)
    {
        foreach (string raw in contents.Split('\n'))
        {
            string line = raw.Trim();

            if (line.Length == 0 ||
                line.StartsWith('#'))
            {
                continue;
            }

            int hash = line.IndexOf('#');

            string data =
                hash >= 0
                    ? line[..hash].Trim()
                    : line;

            string[] fields =
                data.Split(
                    ';',
                    StringSplitOptions.TrimEntries);

            if (fields.Length < 3)
                continue;

            string sequence =
                CodePointsToString(fields[0]);

            if (sequence.Length == 0)
                continue;

            string type = fields[1].Trim();
            string name = fields[2].Trim();

            AddOrReplace(
                entries,
                new SourceRecord(
                    sequence,
                    name,
                    "RGI",
                    type,
                    version));
        }
    }

    private static void AddOrReplace(
        Dictionary<string, EmojiEntry> entries,
        SourceRecord record)
    {
        var candidate =
            new EmojiEntry(
                record.Sequence,
                record.Name,
                record.Status,
                record.Type,
                record.EmojiVersion);

        if (!entries.TryGetValue(
                record.Sequence,
                out EmojiEntry? existing))
        {
            entries[record.Sequence] = candidate;
            return;
        }

        if (Priority(record) > Priority(existing))
            entries[record.Sequence] = candidate;
    }

    private static int Priority(
        SourceRecord record)
    {
        if (record.Type.StartsWith(
                "RGI_Emoji_ZWJ_Sequence",
                StringComparison.Ordinal))
        {
            return 50;
        }

        if (record.Status == "RGI")
            return 40;

        if (record.Status == "fully-qualified")
            return 30;

        if (record.Status == "minimally-qualified")
            return 20;

        if (record.Status == "unqualified")
            return 10;

        return 5;
    }

    private static int Priority(
        EmojiEntry entry)
    {
        if (entry.Type.StartsWith(
                "RGI_Emoji_ZWJ_Sequence",
                StringComparison.Ordinal))
        {
            return 50;
        }

        if (entry.Status == "RGI")
            return 40;

        if (entry.Status == "fully-qualified")
            return 30;

        if (entry.Status == "minimally-qualified")
            return 20;

        if (entry.Status == "unqualified")
            return 10;

        return 5;
    }

    private static string DetermineEmojiTestType(
        string sequence,
        string status)
    {
        if (IsRegionalIndicatorFlag(sequence))
            return "FLAG";

        if (sequence.Contains('\u200D'))
            return "ZWJ";

        if (IsKeycap(sequence))
            return "KEYCAP";

        if (IsTagSequence(sequence))
            return "TAG";

        if (IsModifierSequence(sequence))
            return "MODIFIER";

        if (sequence.Contains('\uFE0F'))
            return "VARIATION_SEQUENCE";

        if (status == "component")
            return "COMPONENT";

        return "EMOJI";
    }

    private static bool IsRegionalIndicatorFlag(
        string sequence)
    {
        var runes =
            sequence.EnumerateRunes().ToArray();

        return runes.Length == 2 &&
               IsRegionalIndicator(runes[0]) &&
               IsRegionalIndicator(runes[1]);
    }

    private static bool IsRegionalIndicator(
        System.Text.Rune rune)
    {
        return rune.Value >= 0x1F1E6 &&
               rune.Value <= 0x1F1FF;
    }

    private static bool IsKeycap(
        string sequence)
    {
        return sequence.Contains('\u20E3');
    }

    private static bool IsTagSequence(
        string sequence)
    {
        return sequence.EnumerateRunes()
            .Any(r =>
                r.Value >= 0xE0020 &&
                r.Value <= 0xE007F);
    }

    private static bool IsModifierSequence(
        string sequence)
    {
        return sequence.EnumerateRunes()
            .Any(r =>
                r.Value >= 0x1F3FB &&
                r.Value <= 0x1F3FF);
    }

    private static string ExtractEmojiTestName(
        string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return "";

        string value = comment.Trim();

        /*
         * emoji-test.txt comments normally contain the rendered
         * emoji followed by the Emoji version and CLDR name.
         *
         * Example:
         *
         *   😀 E1.0 grinning face
         *   😂 E0.6 face with tears of joy
         *
         * Locate the E<version> marker so the rendered emoji
         * itself is never included in the catalog name.
         */
        Match versionMatch =
            Regex.Match(
                value,
                @"\bE[\d.]+\b");

        if (!versionMatch.Success)
            return ExtractCommentName(value);

        string name =
            value[
                (versionMatch.Index + versionMatch.Length)..]
            .Trim();

        /*
         * Remove optional source-data sequence metadata such as:
         *
         *   E13.1 [1] name
         */
        name =
            Regex.Replace(
                name,
                @"^\[\d+\]\s*",
                "");

        /*
         * Remove parenthetical source annotations.
         */
        int paren = name.IndexOf('(');

        if (paren >= 0)
            name = name[..paren].Trim();

        return name.Trim();
    }

    private static string ExtractCommentName(
        string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return "";

        Match match =
            Regex.Match(
                comment,
                @"#?\s*(?:E[\d.]+\s+)?(?:\[\d+\]\s+)?(.+)$");

        if (!match.Success)
            return comment.Trim();

        string value = match.Groups[1].Value.Trim();

        int paren = value.IndexOf('(');

        if (paren >= 0)
            value = value[..paren].Trim();

        return value;
    }

    private static string ExtractVersion(
        string contents)
    {
        Match match =
            Regex.Match(
                contents,
                @"(?m)^#\s*Version:\s*([0-9.]+)\s*$");

        if (!match.Success)
            throw new InvalidOperationException(
                "Unable to determine Unicode Emoji data version.");

        return match.Groups[1].Value;
    }

    private static string CodePointsToString(
        string codeField)
    {
        var builder = new StringBuilder();

        foreach (string token in codeField.Split(
                     ' ',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Contains(".."))
            {
                string[] range =
                    token.Split(
                        "..",
                        StringSplitOptions.None);

                int start =
                    Convert.ToInt32(
                        range[0],
                        16);

                int end =
                    Convert.ToInt32(
                        range[1],
                        16);

                for (int value = start;
                     value <= end;
                     value++)
                {
                    builder.Append(
                        char.ConvertFromUtf32(value));
                }
            }
            else
            {
                int value =
                    Convert.ToInt32(
                        token,
                        16);

                builder.Append(
                    char.ConvertFromUtf32(value));
            }
        }

        return builder.ToString();
    }
}



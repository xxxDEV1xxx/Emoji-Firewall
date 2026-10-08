using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class EmojiCatalog
{
    public string UnicodeVersion { get; }
    public IReadOnlyDictionary<string, EmojiEntry> Entries => _entries;

    private readonly Dictionary<string, EmojiEntry> _entries;

    private EmojiCatalog(
        string unicodeVersion,
        Dictionary<string, EmojiEntry> entries)
    {
        UnicodeVersion = unicodeVersion;
        _entries = entries;
    }

    public static EmojiCatalog Load(string path)
    {
        using FileStream stream = File.OpenRead(path);

        CatalogFile? catalog =
            JsonSerializer.Deserialize<CatalogFile>(stream);

        if (catalog is null)
            throw new InvalidOperationException(
                "Emoji catalog is empty."
            );

        var entries =
            new Dictionary<string, EmojiEntry>(
                StringComparer.Ordinal
            );

        foreach (EmojiEntry entry in catalog.Entries)
        {
            entries[entry.Sequence] = entry;
        }

        return new EmojiCatalog(
            catalog.UnicodeVersion,
            entries
        );
    }

    public bool TryGet(
        string sequence,
        out EmojiEntry entry)
    {
        return _entries.TryGetValue(
            sequence,
            out entry!
        );
    }

    private sealed class CatalogFile
    {
        [JsonPropertyName("unicodeVersion")]
        public string UnicodeVersion { get; set; } = "";

        [JsonPropertyName("entries")]
        public List<EmojiEntry> Entries { get; set; } = [];
    }
}

public sealed class EmojiEntry
{
    [JsonPropertyName("sequence")]
    public string Sequence { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}
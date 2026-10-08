using System.Text;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        string catalogPath = Path.Combine(
            AppContext.BaseDirectory,
            "Catalog",
            "emoji-catalog.json"
        );

        if (!File.Exists(catalogPath))
        {
            Console.WriteLine(
                "ERROR: emoji-catalog.json does not exist."
            );

            Environment.Exit(1);
        }

        EmojiCatalog catalog =
            EmojiCatalog.Load(catalogPath);

        EmojiFirewallEngine firewall =
            new EmojiFirewallEngine(catalog);

        Console.WriteLine(
            "EMOJI FIREWALL"
        );

        Console.WriteLine(
            "=============="
        );

        Console.WriteLine(
            $"Unicode version : {catalog.UnicodeVersion}"
        );

        Console.WriteLine(
            $"Catalog entries : {catalog.Entries.Count:N0}"
        );

        Console.WriteLine();

        string input =
            "Hello 😀 😂 ❤️ ❤️‍🔥 👍 👍🏽 👨‍💻 👨‍👩‍👧‍👦 🇺🇸 🇬🇧 1️⃣ ☕";

        Console.WriteLine(
            $"INPUT : {input}"
        );

        string output =
            firewall.Transform(input);

        Console.WriteLine(
            $"OUTPUT: {output}"
        );

        Console.WriteLine();
        Console.WriteLine("FIREWALL TRANSFORM: PASS");

        Console.WriteLine();
        Console.WriteLine("=== EXHAUSTIVE CATALOG TEST ===");

        int result =
            EmojiFirewallCatalogTest.Run(
                catalog,
                firewall
            );

        Environment.ExitCode = result;
    }
}


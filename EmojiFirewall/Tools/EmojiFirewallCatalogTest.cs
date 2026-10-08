using System.Text;

public static class EmojiFirewallCatalogTest
{
    public static int Run(
        EmojiCatalog catalog,
        EmojiFirewallEngine firewall)
    {
        int passed = 0;
        int failed = 0;

        foreach (EmojiEntry entry in catalog.Entries.Values)
        {
            string input =
                "LEFT " + entry.Sequence + " RIGHT";

            string output =
                firewall.Transform(input);

            if (output.Contains(entry.Sequence, StringComparison.Ordinal))
            {
                Console.WriteLine(
                    $"FAIL ORIGINAL SURVIVED: {entry.Name}"
                );

                failed++;
                continue;
            }

            string expected =
                "[" + entry.Name + "]";

            if (!output.Contains(
                    expected,
                    StringComparison.Ordinal))
            {
                Console.WriteLine(
                    $"FAIL NAME MISSING: {entry.Name}"
                );

                failed++;
                continue;
            }

            passed++;
        }

        Console.WriteLine();
        Console.WriteLine(
            $"CATALOG TEST PASSED : {passed:N0}"
        );

        Console.WriteLine(
            $"CATALOG TEST FAILED  : {failed:N0}"
        );

        if (failed != 0)
            return 1;

        Console.WriteLine();
        Console.WriteLine(
            "ALL CATALOG ENTRIES PASSED."
        );

        return 0;
    }
}

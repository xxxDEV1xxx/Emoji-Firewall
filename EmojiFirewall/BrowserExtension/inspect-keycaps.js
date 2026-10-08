const fs = require("fs");

const catalog = JSON.parse(
    fs.readFileSync("catalog.json", "utf8")
);

console.log("=== ALL KEYCAP ENTRIES ===");

for (const e of catalog.entries) {
    if (
        e.type === "KEYCAP" ||
        e.type === "Emoji_Keycap_Sequence" ||
        e.name.startsWith("keycap:")
    ) {
        console.log(
            JSON.stringify(e.sequence),
            "length=" + e.sequence.length,
            "codepoints=" +
                [...e.sequence]
                    .map(c => "U+" + c.codePointAt(0).toString(16).toUpperCase())
                    .join(" "),
            "type=" + e.type,
            "status=" + e.status,
            "name=" + e.name
        );
    }
}

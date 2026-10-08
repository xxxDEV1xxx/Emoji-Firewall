const fs = require("fs");

const catalog = JSON.parse(
    fs.readFileSync("catalog.json", "utf8")
);

const root = {
    next: new Map(),
    entry: null
};

function add(sequence, entry) {
    let node = root;

    for (const unit of sequence) {
        const c = unit.charCodeAt(0);

        if (!node.next.has(c)) {
            node.next.set(c, {
                next: new Map(),
                entry: null
            });
        }

        node = node.next.get(c);
    }

    node.entry = entry;
}

function matchAt(text, start) {
    let node = root;
    let position = start;
    let best = null;

    while (position < text.length) {
        const c = text.charCodeAt(position);
        const next = node.next.get(c);

        if (!next)
            break;

        node = next;
        position++;

        if (node.entry) {
            best = {
                entry: node.entry,
                end: position
            };
        }
    }

    return best;
}

for (const entry of catalog.entries)
    add(entry.sequence, entry);

const keycaps = catalog.entries.filter(
    e =>
        e.type === "KEYCAP" ||
        e.type === "Emoji_Keycap_Sequence" ||
        e.name.startsWith("keycap:")
);

console.log("=== KEYCAP TRIE TEST ===");

for (const entry of keycaps) {
    const hit = matchAt(entry.sequence, 0);

    console.log(
        JSON.stringify(entry.sequence),
        "=>",
        hit
            ? `[${hit.entry.name}]`
            : "NO MATCH",
        "length=" + entry.sequence.length
    );
}

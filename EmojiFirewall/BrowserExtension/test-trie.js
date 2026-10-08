const fs = require("fs");

const catalog = JSON.parse(
    fs.readFileSync("catalog.json", "utf8")
);

if (catalog.entries.length !== 4887)
    throw new Error("Expected 4887 catalog entries");

function priority(entry) {
    if (entry.type === "RGI_Emoji_ZWJ_Sequence")
        return 50;

    if (
        entry.type === "Emoji_Keycap_Sequence" ||
        entry.type === "RGI_Emoji_Flag_Sequence" ||
        entry.type === "RGI_Emoji_Tag_Sequence" ||
        entry.type === "RGI_Emoji_Modifier_Sequence" ||
        entry.type === "Basic_Emoji"
    )
        return 40;

    if (entry.status === "fully-qualified")
        return 30;

    if (entry.status === "minimally-qualified")
        return 20;

    if (entry.status === "unqualified")
        return 10;

    if (entry.status === "component")
        return 5;

    return 0;
}

const root = {
    next: new Map(),
    entry: null
};

function add(sequence, entry) {
    let node = root;

    for (let i = 0; i < sequence.length; i++) {
        const unit = sequence.charCodeAt(i);

        let next = node.next.get(unit);

        if (!next) {
            next = {
                next: new Map(),
                entry: null
            };

            node.next.set(unit, next);
        }

        node = next;
    }

    if (
        node.entry === null ||
        priority(entry) > priority(node.entry)
    ) {
        node.entry = entry;
    }
}

for (const entry of catalog.entries)
    add(entry.sequence, entry);

function matchAt(text, start) {
    let node = root;
    let position = start;
    let best = null;

    while (position < text.length) {
        const unit = text.charCodeAt(position);
        const next = node.next.get(unit);

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

function transform(text) {
    let output = [];
    let last = 0;
    let position = 0;

    while (position < text.length) {
        const hit = matchAt(text, position);

        if (hit) {
            output.push(
                text.slice(last, position)
            );

            output.push(
                `[${hit.entry.name}]`
            );

            position = hit.end;
            last = position;
            continue;
        }

        const cp = text.codePointAt(position);

        position +=
            cp > 0xFFFF ? 2 : 1;
    }

    output.push(
        text.slice(last)
    );

    return output.join("");
}

/*
 * Explicit semantic regression cases.
 */
const required = [
    ["😀", "grinning face"],
    ["😂", "face with tears of joy"],
    ["❤️", "red heart"],
    ["❤️‍🔥", "heart on fire"],
    ["👍", "thumbs up"],
    ["👍🏽", "thumbs up: medium skin tone"],
    ["👨‍💻", "man technologist"],
    ["👨‍👩‍👧‍👦", "family: man, woman, girl, boy"],
    ["🇺🇸", "flag: United States"],
    ["🇬🇧", "flag: United Kingdom"],
    ["1️⃣", "keycap: 1"],
    ["#⃣", "keycap: #"],
    ["#️⃣", "keycap: \\x{23}"],
    ["*⃣", "keycap: *"],
    ["*️⃣", "keycap: *"],
    ["🔟", "keycap: 10"],
    ["☕", "hot beverage"]
];

console.log("=== REQUIRED SEQUENCES ===");

for (const [sequence, expectedName] of required) {
    const hit = matchAt(sequence, 0);

    if (!hit)
        throw new Error(
            `NO MATCH: ${JSON.stringify(sequence)}`
        );

    if (hit.end !== sequence.length)
        throw new Error(
            `PARTIAL MATCH: ${JSON.stringify(sequence)}`
        );

    if (hit.entry.name !== expectedName) {
        throw new Error(
            `WRONG ENTRY: ${JSON.stringify(sequence)} -> ` +
            `${hit.entry.name} expected ${expectedName}`
        );
    }

    console.log(
        `PASS: ${JSON.stringify(sequence)} -> [${hit.entry.name}]`
    );
}

/*
 * Full catalog integrity test.
 *
 * A sequence is tested against the semantic entry
 * selected by the same priority algorithm used by
 * the browser trie.
 */
const canonical = new Map();

for (const entry of catalog.entries) {
    const existing = canonical.get(entry.sequence);

    if (
        !existing ||
        priority(entry) > priority(existing)
    ) {
        canonical.set(entry.sequence, entry);
    }
}

let failures = 0;

for (const [sequence, expected] of canonical) {
    const hit = matchAt(sequence, 0);

    if (
        !hit ||
        hit.end !== sequence.length ||
        hit.entry.name !== expected.name
    ) {
        console.error(
            "FAIL:",
            JSON.stringify(sequence),
            "->",
            hit ? hit.entry.name : "NO MATCH",
            "expected",
            expected.name
        );

        failures++;
    }
}

console.log("");
console.log("CATALOG ENTRIES :", catalog.entries.length);
console.log("CANONICAL SEQ   :", canonical.size);
console.log("FAILURES        :", failures);

if (failures !== 0)
    process.exit(1);

const demo =
    "Hello 😀 😂 ❤️ ❤️‍🔥 👍 👍🏽 👨‍💻 👨‍👩‍👧‍👦 🇺🇸 🇬🇧 1️⃣ ☕ #️⃣ 🔟";

console.log("");
console.log("=== DEMO ===");
console.log(transform(demo));

console.log("");
console.log("TRIE REGRESSION: PASS");

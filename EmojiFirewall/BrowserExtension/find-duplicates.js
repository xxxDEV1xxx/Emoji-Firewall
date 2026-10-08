const fs = require("fs");

const catalog =
    JSON.parse(
        fs.readFileSync("catalog.json", "utf8")
    );

const groups = new Map();

for (const entry of catalog.entries) {
    if (!groups.has(entry.sequence))
        groups.set(entry.sequence, []);

    groups.get(entry.sequence).push(entry);
}

let duplicateCount = 0;

for (const [sequence, entries] of groups) {
    if (entries.length > 1) {
        duplicateCount++;

        console.log(
            JSON.stringify(sequence),
            "=>",
            entries.map(e =>
                `${e.type}/${e.status}/${e.name}`
            ).join(" | ")
        );
    }
}

console.log("");
console.log("UNIQUE SEQUENCES:", groups.size);
console.log("DUPLICATE SEQUENCES:", duplicateCount);

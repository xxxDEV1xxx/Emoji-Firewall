(() => {
    "use strict";

    function diagnostic(message) {
        try {
            document.documentElement.dataset.emojiFirewall =
                message;
        }
        catch {}

        try {
            console.log(
                "[Emoji Firewall]",
                message
            );
        }
        catch {}
    }

    diagnostic("LOADED");

    const runtime =
        globalThis.browser?.runtime ??
        globalThis.chrome?.runtime;

    if (!runtime)
        return;

    /*
     * Emoji Firewall
     *
     * Catalog:
     *   4,887 Unicode 15.0 entries
     *
     * Matching:
     *   UTF-16 trie
     *   longest registered sequence wins
     *
     * DOM:
     *   incremental MutationObserver processing
     */

    const root = {
        next: new Map(),
        entry: null
    };

    let catalogLoaded = false;
    let observerInstalled = false;

    function entryPriority(entry) {
        if (
            entry.type ===
            "RGI_Emoji_ZWJ_Sequence"
        )
            return 50;

        if (
            entry.type ===
            "Emoji_Keycap_Sequence" ||
            entry.type ===
            "RGI_Emoji_Flag_Sequence" ||
            entry.type ===
            "RGI_Emoji_Tag_Sequence" ||
            entry.type ===
            "RGI_Emoji_Modifier_Sequence" ||
            entry.type ===
            "Basic_Emoji"
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

    function addSequence(sequence, entry) {
        let node = root;

        for (let i = 0; i < sequence.length; i++) {
            const unit =
                sequence.charCodeAt(i);

            let next =
                node.next.get(unit);

            if (!next) {
                next = {
                    next: new Map(),
                    entry: null
                };

                node.next.set(unit, next);
            }

            node = next;
        }

        /*
         * Duplicate Unicode sequences can occur in the
         * source catalog with different classifications.
         * Keep the highest-priority semantic entry.
         */
        if (
            !node.entry ||
            entryPriority(entry) > entryPriority(node.entry)
        ) {
            node.entry = entry;
        }
    }

    function matchAt(text, start) {
        let node = root;
        let position = start;
        let best = null;

        while (position < text.length) {
            const unit =
                text.charCodeAt(position);

            const next =
                node.next.get(unit);

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

    function isEmojiLike(codePoint) {
        return (
            (codePoint >= 0x1F000 &&
             codePoint <= 0x1FAFF) ||
            (codePoint >= 0x2600 &&
             codePoint <= 0x27BF) ||
            (codePoint >= 0x2300 &&
             codePoint <= 0x23FF) ||
            (codePoint >= 0x2B00 &&
             codePoint <= 0x2BFF) ||
            (codePoint >= 0x1F1E6 &&
             codePoint <= 0x1F1FF) ||
            codePoint === 0x20E3 ||
            codePoint === 0xFE0F ||
            codePoint === 0x200D
        );
    }

    function transformText(text) {
        if (!text)
            return text;

        let output = null;
        let lastCopy = 0;
        let position = 0;

        while (position < text.length) {
            const match =
                matchAt(text, position);

            if (match) {
                if (output === null)
                    output = [];

                output.push(
                    text.slice(
                        lastCopy,
                        position
                    )
                );

                output.push("[");
                output.push(match.entry.name);
                output.push("]");

                position = match.end;
                lastCopy = position;

                continue;
            }

            const codePoint =
                text.codePointAt(position);

            const width =
                codePoint > 0xFFFF ? 2 : 1;

            if (isEmojiLike(codePoint)) {
                if (output === null)
                    output = [];

                output.push(
                    text.slice(
                        lastCopy,
                        position
                    )
                );

                output.push(
                    "[UNRECOGNIZED EMOJI]"
                );

                position += width;
                lastCopy = position;

                continue;
            }

            position += width;
        }

        if (output === null)
            return text;

        output.push(
            text.slice(lastCopy)
        );

        return output.join("");
    }

    function shouldSkip(node) {
        const parent =
            node.parentElement;

        if (!parent)
            return false;

        return !!parent.closest(
            "script,style,noscript,input,textarea,select,option,[contenteditable='true']"
        );
    }

    function processTextNode(node) {
        if (
            !node ||
            !node.nodeValue ||
            shouldSkip(node)
        )
            return;

        const original =
            node.nodeValue;

        const transformed =
            transformText(original);

        if (transformed === original)
            return;

        node.nodeValue = transformed;
    }

    function scanElement(element) {
        if (!element)
            return;

        if (
            element.nodeType ===
            Node.TEXT_NODE
        ) {
            processTextNode(element);
            return;
        }

        const walker =
            document.createTreeWalker(
                element,
                NodeFilter.SHOW_TEXT
            );

        let node;

        while (
            (node = walker.nextNode())
        ) {
            processTextNode(node);
        }
    }

    function installObserver() {
        if (observerInstalled)
            return;

        observerInstalled = true;

        const observer =
            new MutationObserver(
                mutations => {
                    for (const mutation of mutations) {
                        if (
                            mutation.type ===
                            "childList"
                        ) {
                            for (
                                const node
                                of mutation.addedNodes
                            ) {
                                if (
                                    node.nodeType ===
                                    Node.TEXT_NODE
                                ) {
                                    processTextNode(
                                        node
                                    );
                                }
                                else if (
                                    node.nodeType ===
                                    Node.ELEMENT_NODE
                                ) {
                                    scanElement(node);
                                }
                            }
                        }
                        else if (
                            mutation.type ===
                            "characterData"
                        ) {
                            processTextNode(
                                mutation.target
                            );
                        }
                    }
                }
            );

        observer.observe(
            document.documentElement,
            {
                subtree: true,
                childList: true,
                characterData: true
            }
        );
    }

    async function loadCatalog() {
        diagnostic("LOADING_CATALOG");

        const url =
            runtime.getURL("catalog.json");

        const response =
            await fetch(url);

        if (!response.ok) {
            throw new Error(
                `Emoji catalog HTTP ${response.status}`
            );
        }

        const data =
            await response.json();

        if (!Array.isArray(data.entries)) {
            throw new Error(
                "Emoji catalog entries missing."
            );
        }

        for (const entry of data.entries) {
            addSequence(
                entry.sequence,
                entry
            );
        }

        if (data.entries.length !== 4887) {
            console.warn(
                "[Emoji Firewall] expected 4887 catalog entries; received",
                data.entries.length
            );
        }

        catalogLoaded = true;

        diagnostic("CATALOG_LOADED_" + data.entries.length);

        console.log(
            "[Emoji Firewall] catalog loaded:",
            data.entries.length
        );
    }

    async function start() {
        try {
            /*
             * document_start may execute before <html> or <body>
             * exists. Do not terminate the firewall in that state.
             *
             * Install the earliest available DOM bootstrap first,
             * then load the Unicode catalog.
             */
            const armObserver = () => {
                if (document.documentElement) {
                    installObserver();
                    return true;
                }

                return false;
            };

            if (!armObserver()) {
                const bootstrapObserver =
                    new MutationObserver(() => {
                        if (armObserver()) {
                            bootstrapObserver.disconnect();
                        }
                    });

                bootstrapObserver.observe(
                    document,
                    {
                        childList: true
                    }
                );
            }

            /*
             * Load and build the semantic trie.
             *
             * The observer is already armed, so DOM mutations
             * occurring while the catalog is loading are captured.
             */
            await loadCatalog();

            /*
             * The catalog may have loaded before <body> exists.
             * Wait for the document root rather than returning.
             */
            const scanWhenReady = () => {
                if (!document.body)
                    return false;

                scanElement(
                    document.body
                );

                diagnostic("ACTIVE");

                return true;
            };

            if (!scanWhenReady()) {
                const bodyObserver =
                    new MutationObserver(() => {
                        if (scanWhenReady()) {
                            bodyObserver.disconnect();
                        }
                    });

                bodyObserver.observe(
                    document.documentElement ||
                    document,
                    {
                        childList: true,
                        subtree: true
                    }
                );
            }
        }
        catch (error) {
            console.error(
                "[Emoji Firewall] FAILED:",
                error
            );

            diagnostic("FAILED");
        }
    }

    start();
})();






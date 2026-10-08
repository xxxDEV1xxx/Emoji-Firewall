# Emoji-Firewall
because fuck you, thats why
Copyright (c) 2026 Christopher T. Williams

License

Permission is granted to use, copy, modify, and distribute this software solely for non-commercial research, testing, and security-analysis purposes, provided that the copyright notice and this permission notice are retained.

Commercial use, sale, marketplace distribution, or monetization of this software, in whole or in part, is strictly prohibited without prior written consent from the author.

THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. THE AUTHOR SHALL NOT BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER LIABILITY ARISING FROM THE USE OR OTHER DEALINGS IN THE SOFTWARE.
######## how to run 

Open about:debugging#/runtime/this-firefox

load
J:/EmojiFirewall/BrowserExtension/manifest.json
#############
the  full setup
###########
1. Install prerequisites

You need:

.NET 8 SDK
Node.js
Firefox

Verify:

dotnet --version
node --version
npm --version

Node is only needed for the JavaScript syntax/regression checks; the browser extension itself does not require npm.

2. Extract the project

For example:

Expand-Archive .\EmojiFirewall.zip -DestinationPath J:\

Then:

cd J:\EmojiFirewall

You should have:

J:\EmojiFirewall\
├── EmojiFirewall.csproj
├── Program.cs
├── Tools\
├── EmojiFirewall\
│   └── Catalog\
│       └── emoji-catalog.json
└── BrowserExtension\
    ├── manifest.json
    ├── content.js
    ├── catalog.json
    └── ...
3. Build the .NET project
cd J:\EmojiFirewall
dotnet restore
dotnet build

Then run the native firewall/catalog test:

dotnet run

You should get the catalog validation and transformation results, including:

CATALOG TEST PASSED : 4887
CATALOG TEST FAILED  : 0
ALL CATALOG ENTRIES PASSED.
4. Validate the browser firewall
cd J:\EmojiFirewall\BrowserExtension
node --check .\content.js

That should return with no output.

5. Load the extension into Firefox

Open:

about:debugging#/runtime/this-firefox

Select:

Load Temporary Add-on...

Navigate to:

J:\EmojiFirewall\BrowserExtension\manifest.json

Select manifest.json.

The extension should appear in the temporary extensions list.

6. Test it

Open any site containing SVG imagery/emoji.

The current firewall should intercept SVG <img> resources and inline SVGs and replace them visually with:

.

This is deliberately broad. It doesn't need to know:

emoji name
Unicode code point
vendor
filename
site

An SVG representation gets neutralized.

7. Optional local test server

If you want to run the included test page:

cd J:\EmojiFirewall\BrowserExtension

Start-Process `
    -FilePath "python" `
    -ArgumentList "-m","http.server","8765","--bind","127.0.0.1" `
    -WorkingDirectory (Get-Location) `
    -WindowStyle Hidden

Then:

http://127.0.0.1:8765/test.html
8. One-command startup

Once you're satisfied with the package, I'd make the distributed project have a single launcher:

J:\EmojiFirewall\
└── Start-EmojiFirewall.ps1

Then the user experience becomes:

cd J:\EmojiFirewall
.\Start-EmojiFirewall.ps1

That script can perform:

             Start-EmojiFirewall.ps1
                       |
             +---------+---------+
             |                   |
             v                   v
       Validate .NET        Validate JS
             |                   |
             +---------+---------+
                       |
                       v
              Launch/test firewall

For the actual distributed package, I'd also make the README's primary path not require rebuilding the catalog. The already-generated BrowserExtension\catalog.json and content.js are the runtime payload. The Unicode source files and CatalogBuilder can remain there for reproducibility/development.

So the clean distinction is:

DEVELOPER
    |
    +-- CatalogBuilder
    +-- UnicodeData
    +-- .NET tests
    +-- JS regression tests
    |
    v
RELEASE PACKAGE
    |
    +-- BrowserExtension
    |     ├── manifest.json
    |     ├── content.js
    |     └── catalog.json
    |
    +-- README
    +-- license

And importantly, don't make the end user download Unicode data or rebuild anything just to run the firewall. The package should arrive already operational.

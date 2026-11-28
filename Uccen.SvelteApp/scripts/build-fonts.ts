// Rebuilds the Noto Serif subset used by the Cyrillic and Greek transliterations:
//   public/fonts/NotoSerif-cyrillic-greek.woff2         (upright)
//   public/fonts/NotoSerif-Italic-cyrillic-greek.woff2  (italic)
// and the matching `unicode-range` lines in src/app.css.
//
// Usage: npm run build-fonts
// Run it after adding a Cyrillic or Greek character to a mapping; test/fonts.test.ts fails until then.
//
// Why a custom subset:
//   - Google's standard split (cyrillic, cyrillic-ext, greek, greek-ext, vietnamese…) would put a letter
//     and its combining mark in different files (e.g. д in "cyrillic", the dot below in "vietnamese").
//     A mark is only positioned correctly (GPOS table) when it comes from the same file as its letter.
//   - So we ask Google Fonts for one file containing exactly the characters the mappings produce,
//     letters and marks together (the css2 API's `text=` parameter builds such a subset on demand).
//
// The files are downloaded once, verified, and committed: the app never contacts Google at runtime.

import { readFileSync, writeFileSync } from "node:fs";
import { readScriptConfig } from "./mappingFiles.ts";
import { readWoff2 } from "./woff2.ts";

const SCRIPTS = ["cyrl", "grek"];
/** Characters used in the stories themselves (not produced by a mapping) that should match the font. */
const EXTRA_CHARACTERS = ["Ω"];
/** Same filter as test/fonts.test.ts: Cyrillic, Greek and combining marks; Latin comes from other files. */
const isCyrillicGreekOrMark = (cp: number) => (cp >= 0x0300 && cp <= 0x052f) || (cp >= 0x1f00 && cp <= 0x1fff);

const GOOGLE_FONTS_CSS = "https://fonts.googleapis.com/css2?family=Noto+Serif:ital,wght@0,100..900;1,100..900";
/** Google serves WOFF2 variable fonts only to modern browsers, so we present ourselves as one. */
const BROWSER_USER_AGENT =
  "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36";

const fontsDirectory = new URL("../public/fonts/", import.meta.url);
const appCssUrl = new URL("../src/app.css", import.meta.url);
const OUTPUT_FILES = { normal: "NotoSerif-cyrillic-greek.woff2", italic: "NotoSerif-Italic-cyrillic-greek.woff2" };

const codePoints = collectCodePoints();
const fonts = await downloadSubset(codePoints);
for (const [style, font] of Object.entries(fonts)) verify(style, font, codePoints);

for (const [style, font] of Object.entries(fonts)) {
  writeFileSync(new URL(OUTPUT_FILES[style as keyof typeof OUTPUT_FILES], fontsDirectory), font);
  console.log(`${OUTPUT_FILES[style as keyof typeof OUTPUT_FILES]}: ${font.length} bytes`);
}
const previousRange = updateUnicodeRanges(toUnicodeRange(codePoints));
reportChanges(previousRange, codePoints);

// ── 1. Which characters ───────────────────────────────────────────────────────────────────────

/** Every Cyrillic/Greek character and combining mark the mappings can produce (capitals included). */
function collectCodePoints(): number[] {
  const set = new Set<number>();
  for (const scriptName of SCRIPTS) {
    // readScriptConfig adds the capitals exactly as the app does (JavaScript's toUpperCase).
    for (const value of Object.values(readScriptConfig(scriptName).mapping)) {
      for (const character of value) {
        const codePoint = character.codePointAt(0)!;
        if (isCyrillicGreekOrMark(codePoint)) set.add(codePoint);
      }
    }
  }
  for (const character of EXTRA_CHARACTERS) set.add(character.codePointAt(0)!);
  return [...set].sort((a, b) => a - b);
}

// ── 2. Download ───────────────────────────────────────────────────────────────────────────────

/** Asks Google Fonts for a subset holding exactly these characters; returns one WOFF2 per style. */
async function downloadSubset(characters: number[]): Promise<Record<"normal" | "italic", Buffer>> {
  const text = encodeURIComponent(String.fromCodePoint(...characters));
  const css = await fetchText(`${GOOGLE_FONTS_CSS}&text=${text}`);

  const faces = [...css.matchAll(/font-style:\s*(normal|italic);[\s\S]*?src:\s*url\(([^)]+)\)\s*format\('woff2'\)/g)];
  const urls = Object.fromEntries(faces.map((face) => [face[1], face[2]]));
  if (!urls.normal || !urls.italic) throw new Error(`Unexpected Google Fonts response:\n${css}`);

  return { normal: await fetchBuffer(urls.normal), italic: await fetchBuffer(urls.italic) };
}

async function fetchText(url: string): Promise<string> {
  const response = await fetch(url, { headers: { "User-Agent": BROWSER_USER_AGENT } });
  if (!response.ok) throw new Error(`${response.status} ${response.statusText}: ${url}`);
  return response.text();
}

async function fetchBuffer(url: string): Promise<Buffer> {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`${response.status} ${response.statusText}: ${url}`);
  return Buffer.from(await response.arrayBuffer());
}

// ── 3. Verify the downloaded files ────────────────────────────────────────────────────────────

/** Fails (writing nothing) unless the font maps every character and can position combining marks. */
function verify(style: string, font: Buffer, characters: number[]): void {
  const { tables, characters: mapped } = readWoff2(font);
  const missing = characters.filter((cp) => !mapped.has(cp)).map(formatCodePoint);
  if (missing.length > 0) {
    throw new Error(`${style}: Noto Serif has no glyph for ${missing.join(" ")}; choose other characters in the mapping.`);
  }
  if (!tables.includes("GPOS")) throw new Error(`${style}: no GPOS table, combining marks would be misplaced.`);
  console.log(`${style}: ${characters.length} characters verified, tables ${tables.join(" ")}`);
}

// ── 4. Update app.css ─────────────────────────────────────────────────────────────────────────

/** Compact CSS unicode-range: consecutive code points become ranges (U+0391-039D). */
function toUnicodeRange(characters: number[]): string {
  const parts: string[] = [];
  for (let i = 0; i < characters.length; i++) {
    let j = i;
    while (j + 1 < characters.length && characters[j + 1] === characters[j] + 1) j++;
    parts.push(j > i ? `${formatCodePoint(characters[i])}-${hex(characters[j])}` : formatCodePoint(characters[i]));
    i = j;
  }
  return parts.join(", ");
}

/** Rewrites the unicode-range of both cyrillic-greek @font-face rules; returns the previous range. */
function updateUnicodeRanges(range: string): string {
  const css = readFileSync(appCssUrl, "utf8");
  const rule = /(@font-face\s*\{[^}]*?cyrillic-greek\.woff2[^}]*?unicode-range:\s*)([^;]+)(;)/g;
  const matches = [...css.matchAll(rule)];
  if (matches.length !== 2) throw new Error(`Expected 2 cyrillic-greek @font-face rules in app.css, found ${matches.length}`);

  writeFileSync(appCssUrl, css.replace(rule, (_match, before, _old, after) => before + range + after), "utf8");
  return matches[0][2];
}

function reportChanges(previousRange: string, characters: number[]): void {
  const previous = new Set(
    previousRange.split(",").flatMap((part) => {
      const [first, last = first] = part.trim().replace(/^U\+/i, "").split("-").map((h) => parseInt(h, 16));
      return Array.from({ length: last - first + 1 }, (_, i) => first + i);
    })
  );
  const added = characters.filter((cp) => !previous.has(cp)).map(formatCodePoint);
  const removed = [...previous].filter((cp) => !characters.includes(cp)).map(formatCodePoint);
  console.log(`app.css: ${characters.length} characters; added: ${added.join(" ") || "none"}; removed: ${removed.join(" ") || "none"}`);
}

function hex(codePoint: number): string {
  return codePoint.toString(16).toUpperCase().padStart(4, "0");
}

function formatCodePoint(codePoint: number): string {
  return `U+${hex(codePoint)}`;
}

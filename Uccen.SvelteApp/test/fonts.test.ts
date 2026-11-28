import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { readScriptConfig } from "../scripts/mappingFiles.ts";
import { readWoff2 } from "../scripts/woff2.ts";

const appCss = readFileSync(new URL("../src/app.css", import.meta.url), "utf8");

test("each Noto Serif Cyrillic/Greek font file contains every character its unicode-range declares", () => {
  const rules = (appCss.match(/@font-face\s*\{[^}]*\}/g) || []).filter((rule) => rule.includes("cyrillic-greek.woff2"));
  assert.equal(rules.length, 2, "Expected an upright and an italic cyrillic-greek @font-face rule");

  for (const rule of rules) {
    const file = rule.match(/url\("\/fonts\/([^"]+)"\)/)![1];
    const { tables, characters } = readWoff2(readFileSync(new URL(`../public/fonts/${file}`, import.meta.url)));
    const declared = [...parseUnicodeRange(rule.match(/unicode-range:\s*([^;]+);/)![1])];
    const missing = declared.filter((codePoint) => !characters.has(codePoint)).map((cp) => `U+${cp.toString(16).toUpperCase()}`);

    assert.deepEqual(missing, [], `${file} lacks characters its unicode-range declares: run "npm run build-fonts"`);
    assert.ok(tables.includes("GPOS"), `${file} has no GPOS table: combining marks would be misplaced`);
  }
});

function parseUnicodeRange(unicodeRange: string): Set<number> {
  const codePoints = new Set<number>();
  for (const token of unicodeRange.split(",")) {
    const [start, end = start] = token.trim().replace(/^U\+/i, "").split("-");
    for (let codePoint = parseInt(start, 16); codePoint <= parseInt(end, 16); codePoint++) codePoints.add(codePoint);
  }
  return codePoints;
}

/** Code points declared by every "Noto Serif" @font-face rule whose file name contains `fileFragment`. */
function declaredCodePoints(fileFragment: string): Set<number> {
  const covered = new Set<number>();
  const rules = appCss.match(/@font-face\s*\{[^}]*\}/g) || [];

  for (const rule of rules.filter((rule) => rule.includes("Noto Serif") && rule.includes(fileFragment))) {
    for (const codePoint of parseUnicodeRange(rule.match(/unicode-range:\s*([^;]+);/)?.[1] || "")) {
      covered.add(codePoint);
    }
  }

  return covered;
}

test("the Noto Serif Cyrillic/Greek subset covers every character the cyrl and grek mappings produce", () => {
  const covered = declaredCodePoints("cyrillic-greek");
  assert.ok(covered.size > 0, "Expected @font-face rules for the cyrillic-greek subset in app.css");

  for (const scriptName of ["cyrl", "grek"]) {
    const { mapping } = readScriptConfig(scriptName);
    const missing = new Set<string>();

    for (const value of Object.values(mapping)) {
      for (const character of value) {
        const codePoint = character.codePointAt(0)!;
        const isCyrillicGreekOrMark =
          (codePoint >= 0x0300 && codePoint <= 0x052f) || (codePoint >= 0x1f00 && codePoint <= 0x1fff);
        if (isCyrillicGreekOrMark && !covered.has(codePoint)) {
          missing.add(`U+${codePoint.toString(16).toUpperCase().padStart(4, "0")} ${character}`);
        }
      }
    }

    assert.deepEqual([...missing], [], `${scriptName}: run "npm run build-fonts" to add these characters to the font`);
  }
});

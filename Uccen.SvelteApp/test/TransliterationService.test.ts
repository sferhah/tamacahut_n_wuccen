import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { listScriptNames, readScriptConfig } from "../scripts/mappingFiles.ts";
import { TransliterationService } from "../src/TransliterationService.ts";

type Book = {
  Tales: { Source: { Title: string; Paragraphs: string[] }; Translation: { Title: string } }[];
};

const arabicConfig = readScriptConfig("arab");

const book = JSON.parse(
  readFileSync(new URL("../public/data/tamacahut_n_wuccen.json", import.meta.url), "utf8")
) as Book;
const rucherStory = book.Tales.find((story) => story.Translation.Title === "Dans le rucher");

test("Arabic transliteration preserves the known-good rucher title and paragraph", () => {
  assert.ok(rucherStory, 'Expected the "Dans le rucher" story to be present');

  assert.equal(
    TransliterationService.transliterate(rucherStory.Source.Title, arabicConfig),
    "دي تد\u064Eوّيرت ن تزيزوا"
  );
  assert.equal(
    TransliterationService.transliterate(rucherStory.Source.Paragraphs[0], arabicConfig),
    "ييوّاسّ، ئروح ئنيسي د ووشّ\u064Eن أد د\u00A0ـاكر\u064Eن تام\u064Eنت سي تد\u064Eوّيرت ن تزيزوا. ئنيسي ي\u064Eبنا أد د\u00A0ـي\u064Eرّ س\u064Eڨ ووشّ\u064Eن تّار ن تق\u064Eلّالت ن ل\u064Eخليعا."
  );
});

test("Arabic transliteration uses alif wasla for word-initial schwa", () => {
  assert.equal(TransliterationService.transliterate("e", arabicConfig), "ٱ");
  assert.equal(TransliterationService.transliterate("eb be", arabicConfig), "ٱب ب\u064E");
});

test("Arabic transliteration maps medial schwa e to U+064E", () => {
  assert.equal(TransliterationService.transliterate("be", arabicConfig), "ب\u064E");
});

test("Arabic transliteration replaces non-breaking hyphens with kasheeda", () => {
  assert.match(
    TransliterationService.transliterate("s‑yenna", arabicConfig),
    /ـ\u00A0ـ/
  );
});

test("every script mapping parses and transliterates", () => {
  const scriptNames = listScriptNames();
  assert.ok(scriptNames.length > 0, "Expected at least one non-Latin script mapping");

  for (const scriptName of scriptNames) {
    const config = readScriptConfig(scriptName);
    assert.notEqual(TransliterationService.transliterate("Taqbaylit", config), "Taqbaylit", scriptName);
  }
});

test("the transliteration table has an ISO 15924 header and one column per script", () => {
  const scriptNames = listScriptNames();
  const table = TransliterationService.getTransliterations(
    Object.fromEntries(scriptNames.map((scriptName) => [scriptName, readScriptConfig(scriptName)]))
  );

  assert.deepEqual(table[0], ["latn", ...scriptNames]);
  assert.deepEqual(
    table[0],
    ["latn", "tfng", "arab", "cyrl", "grek", "hebr", "syrc", "armi", "nbat", "phnx", "sarb"]
  );
  for (const row of table.slice(1)) {
    assert.equal(row.length, scriptNames.length + 1);
  }
});

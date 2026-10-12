import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { TransliterationService } from "../src/TransliterationService.js";

const arabicData = JSON.parse(
  readFileSync(new URL("../src/transliterationMappings/Arabic.json", import.meta.url), "utf8")
);
const mapping = {};

for (const [key, rawValue] of Object.entries(arabicData.mapping)) {
  const value = rawValue.replace(/[\u200E\u200F]/g, "");
  mapping[key] = value;
  if (key.toUpperCase() !== key) {
    mapping[key.toUpperCase()] = value.toUpperCase();
  }
}

const arabicConfig = {
  name: "Arabic",
  direction: arabicData.direction || "ltr",
  geminizer: String.fromCodePoint(Number.parseInt(arabicData.geminizer, 16)),
  nullVowel: String.fromCodePoint(Number.parseInt(arabicData.nullVowel, 16)),
  scriptType: arabicData.scriptType || "Alphabet",
  initialSchwa: arabicData.initialSchwa || undefined,
  mapping
};

const stories = JSON.parse(
  readFileSync(new URL("../public/data/tamacahut_n_wuccen.json", import.meta.url), "utf8")
);
const rucherStory = stories.find((story) => story.French.Title === "Dans le rucher");

test("Arabic transliteration preserves the known-good rucher title and paragraph", () => {
  assert.ok(rucherStory, 'Expected the "Dans le rucher" story to be present');

  assert.equal(
    TransliterationService.transliterate(rucherStory.Kabyle.Title, arabicConfig),
    "دي تد\u064Eوّيرت ن تزيزوا"
  );
  assert.equal(
    TransliterationService.transliterate(rucherStory.Kabyle.Paragraphs[0], arabicConfig),
    "ييوّاسّ، ئروح ئنيسي د ووشّ\u064Eن أد د\u00A0ـاكر\u064Eن تام\u064Eنت سي تد\u064Eوّيرت ن تيزيزوا. ئنيسي ي\u064Eبنا أد د\u00A0ـي\u064Eرّ س\u064Eڨ ووشّ\u064Eن تّار ن تق\u064Eلّالت ن ل\u064Eخليعا."
  );
});

test("Arabic transliteration uses alif wasla for word-initial schwa", () => {
  assert.equal(TransliterationService.transliterate("e", arabicConfig), "ٱ");
  assert.equal(TransliterationService.transliterate("eb be", arabicConfig), "ٱب ب\u064E");
});

test("Arabic transliteration maps medial schwa e to U+064E", () => {
  assert.equal(TransliterationService.transliterate("be", arabicConfig), "ب\u064E");
});

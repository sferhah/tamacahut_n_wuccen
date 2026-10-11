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
    "دي تدࣤوّيرت ن تزيزوا"
  );
  assert.equal(
    TransliterationService.transliterate(rucherStory.Kabyle.Paragraphs[0], arabicConfig),
    "ييوّاسّ، ئروه ئنيسي د ووشّࣤن أد د\u00A0ـاكرࣤن تامࣤنت سي تدࣤوّيرت ن تيزيزوا. ئنيسي يࣤبنا أد د\u00A0ـيࣤرّ سࣤڨ ووشّࣤن تّار ن تقࣤلّالت ن لࣤخليعا."
  );
});

test("Arabic transliteration maps schwa e to U+08E4", () => {
  assert.equal(TransliterationService.transliterate("e", arabicConfig), "\u08E4");
  assert.equal(TransliterationService.transliterate("be", arabicConfig), "ب\u08E4");
});

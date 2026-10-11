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
    "دي تدوّيڕت ن تزيزوا"
  );
  assert.equal(
    TransliterationService.transliterate(rucherStory.Kabyle.Paragraphs[0], arabicConfig),
    "ييوّاسّ، ئڕوه ئنيسي د ووشّن أد د ـاكرن تامنت سي تدوّيڕت ن تيزيزوا. ئنيسي يبنا أد د ـيرّ سڨ ووشّن تّاڕ ن تقلّالت ن لخليعا."
  );
});

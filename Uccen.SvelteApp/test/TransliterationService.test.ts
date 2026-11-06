import assert from "node:assert/strict";
import { readdirSync, readFileSync, writeFileSync } from "node:fs";
import test from "node:test";
import type { ScriptConfig } from "../src/ConfigDeserializer.ts";
import { TransliterationService } from "../src/TransliterationService.ts";

type MappingData = {
  direction?: "ltr" | "rtl";
  geminizer?: string;
  nullVowel?: string;
  initialSchwa?: string;
  scriptType?: string;
  mapping: Record<string, string>;
};

type Story = {
  Source: { Title: string; Paragraphs: string[] };
  Translation: { Title: string; Paragraphs: string[] };
};

function parseConfig(scriptName: string): ScriptConfig {
  const data = JSON.parse(
    readFileSync(
      new URL(`../src/transliterationMappings/${scriptName}.json`, import.meta.url),
      "utf8"
    )
  ) as MappingData;
  const mapping: Record<string, string> = {};

  for (const [key, rawValue] of Object.entries(data.mapping)) {
    const value = rawValue.replace(/[\u200E\u200F]/g, "");
    mapping[key] = value;

    if (key.toUpperCase() !== key) {
      mapping[key.toUpperCase()] = value.toUpperCase();
    }
  }

  return {
    name: scriptName,
    direction: data.direction || "ltr",
    geminizer: String.fromCharCode(parseInt(data.geminizer || "0x00B2", 16)),
    nullVowel: String.fromCharCode(parseInt(data.nullVowel || "0x00B0", 16)),
    scriptType: data.scriptType || "Alphabet",
    initialSchwa: data.initialSchwa || undefined,
    mapping
  };
}

const arabicConfig = parseConfig("arab");

const stories = JSON.parse(
  readFileSync(new URL("../public/data/tamacahut_n_wuccen.json", import.meta.url), "utf8")
) as Story[];
const rucherStory = stories.find((story) => story.Translation.Title === "Dans le rucher");

test("Arabic transliteration preserves the known-good rucher title and paragraph", () => {
  assert.ok(rucherStory, 'Expected the "Dans le rucher" story to be present');

  assert.equal(
    TransliterationService.transliterate(rucherStory.Source.Title, arabicConfig),
    "دي تد\u064Eوّيرت ن تزيزوا"
  );
  assert.equal(
    TransliterationService.transliterate(rucherStory.Source.Paragraphs[0], arabicConfig),
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

test("Arabic transliteration replaces non-breaking hyphens with kasheeda", () => {
  assert.match(
    TransliterationService.transliterate("s‑yenna", arabicConfig),
    /ـ\u00A0ـ/
  );
});

test("generates transliterated story files for every script mapping", () => {
  const sourceDataDirectory = new URL("../public/data/", import.meta.url);
  const outputDirectory = new URL("../../Uccen.ConsoleApp/Resources/json/", import.meta.url);
  const mappingDirectory = new URL("../src/transliterationMappings/", import.meta.url);
  const inputFiles = readdirSync(sourceDataDirectory)
    .filter((fileName) => /^tamacahut_n_wuccen(?:\.[a-z]{3})?\.json$/.test(fileName))
    .sort();
  const scriptNames = readdirSync(mappingDirectory)
    .filter((fileName) => fileName.endsWith(".json") && fileName !== "latn.json")
    .map((fileName) => fileName.slice(0, -".json".length))
    .sort();

  assert.ok(inputFiles.length > 0, "Expected at least one source data file");
  assert.ok(scriptNames.length > 0, "Expected at least one non-Latin script mapping");

  let generatedFileCount = 0;
  for (const inputFile of inputFiles) {
    const stories = JSON.parse(
      readFileSync(new URL(inputFile, sourceDataDirectory), "utf8")
    ) as Story[];

    for (const scriptName of scriptNames) {
      const config = parseConfig(scriptName);
      const transliteratedStories = stories.map((story) => ({
        ...story,
        Source: {
          ...story.Source,
          Title: TransliterationService.transliterate(story.Source.Title, config),
          Paragraphs: story.Source.Paragraphs.map((paragraph) =>
            TransliterationService.transliterate(paragraph, config)
          )
        }
      }));
      const localeSuffix = inputFile.match(/^tamacahut_n_wuccen(\.[a-z]{3})?\.json$/)?.[1] || "";
      const outputFile = `tamacahut_n_wuccen_${scriptName}${localeSuffix}.json`;
      const outputUrl = new URL(outputFile, outputDirectory);

      writeFileSync(outputUrl, `${JSON.stringify(transliteratedStories, null, 2)}\n`, "utf8");
      assert.deepEqual(JSON.parse(readFileSync(outputUrl, "utf8")), transliteratedStories);
      generatedFileCount++;
    }
  }

  assert.equal(generatedFileCount, inputFiles.length * scriptNames.length);
});

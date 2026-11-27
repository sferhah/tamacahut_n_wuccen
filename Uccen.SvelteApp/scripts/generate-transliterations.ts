// Generates the files the console app turns into the README table and the PDFs:
//   - tamacahut_n_wuccen_<script>[.<translation>].json: each book with its Kabyle text transliterated
//   - transliteration_table.json: the letter-by-letter mapping table for every script
// Output: Uccen.ConsoleApp/Resources/json/
//
// Usage: npm run generate

import { readdirSync, readFileSync, writeFileSync } from "node:fs";
import { TransliterationService } from "../src/TransliterationService.ts";
import { listScriptNames, readScriptConfig } from "./mappingFiles.ts";

type Tale = {
  Source: { Title: string; Paragraphs: string[] };
  Translation: { Title: string; Paragraphs: string[] };
};

type Book = {
  Tales: Tale[];
};

const BOOK_FILE_PATTERN = /^tamacahut_n_wuccen(\.[a-z]{3})?\.json$/;
const sourceDataDirectory = new URL("../public/data/", import.meta.url);
const outputDirectory = new URL("../../Uccen.ConsoleApp/Resources/json/", import.meta.url);

const bookFiles = readdirSync(sourceDataDirectory).filter((fileName) => BOOK_FILE_PATTERN.test(fileName)).sort();
const scriptConfigs = listScriptNames().map(readScriptConfig);

if (bookFiles.length === 0) throw new Error(`No book file found in ${sourceDataDirectory.pathname}`);
if (scriptConfigs.length === 0) throw new Error("No non-Latin script mapping found");

for (const bookFile of bookFiles) {
  const book = JSON.parse(readFileSync(new URL(bookFile, sourceDataDirectory), "utf8")) as Book;
  const translationSuffix = bookFile.match(BOOK_FILE_PATTERN)?.[1] || "";

  for (const config of scriptConfigs) {
    const transliterate = (text: string) => TransliterationService.transliterate(text, config);
    const transliteratedBook: Book = {
      ...book,
      Tales: book.Tales.map((tale) => ({
        ...tale,
        Source: {
          ...tale.Source,
          Title: transliterate(tale.Source.Title),
          Paragraphs: tale.Source.Paragraphs.map(transliterate)
        }
      }))
    };

    writeOutput(`tamacahut_n_wuccen_${config.name}${translationSuffix}.json`, `${JSON.stringify(transliteratedBook, null, 2)}\n`);
  }
}

// One row per line so the file reads like a table.
const table = TransliterationService.getTransliterations(
  Object.fromEntries(scriptConfigs.map((config) => [config.name, config]))
);
writeOutput("transliteration_table.json", `[\n${table.map((row) => `  ${JSON.stringify(row)}`).join(",\n")}\n]\n`);

console.log(`Generated ${bookFiles.length * scriptConfigs.length + 1} files in Uccen.ConsoleApp/Resources/json/`);

function writeOutput(fileName: string, content: string): void {
  writeFileSync(new URL(fileName, outputDirectory), content, "utf8");
}

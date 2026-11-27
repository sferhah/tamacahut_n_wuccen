// Reads the transliteration mapping files from disk, for Node (tests and generators).
// The app itself bundles them through ConfigDeserializer instead.

import { readdirSync, readFileSync } from "node:fs";
import { parseScriptConfig, type ScriptConfig, type ScriptConfigData } from "../src/ScriptConfig.ts";

const mappingDirectory = new URL("../src/transliterationMappings/", import.meta.url);

/**
 * Column order of the transliteration table (after "latn"): Tifinagh, then the scripts that have
 * PDF editions, then the others. Scripts missing from this list are appended alphabetically.
 */
const SCRIPT_ORDER = ["tfng", "arab", "cyrl", "grek", "hebr", "syrc", "armi", "nbat", "phnx", "sarb"];

/** ISO 15924 codes of every script that has a mapping file, except Latin (the source script), in table order. */
export function listScriptNames(): string[] {
  const rank = (scriptName: string) => {
    const index = SCRIPT_ORDER.indexOf(scriptName);
    return index === -1 ? SCRIPT_ORDER.length : index;
  };

  return readdirSync(mappingDirectory)
    .filter((fileName) => fileName.endsWith(".json") && fileName !== "latn.json")
    .map((fileName) => fileName.slice(0, -".json".length))
    .sort((a, b) => rank(a) - rank(b) || a.localeCompare(b));
}

export function readScriptConfig(scriptName: string): ScriptConfig {
  const data = JSON.parse(
    readFileSync(new URL(`${scriptName}.json`, mappingDirectory), "utf8")
  ) as ScriptConfigData;
  return parseScriptConfig(scriptName, data);
}

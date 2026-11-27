// Reads the transliteration mapping files from disk, for Node (tests and generators).
// The app itself bundles them through ConfigDeserializer instead.

import { readdirSync, readFileSync } from "node:fs";
import { parseScriptConfig, type ScriptConfig, type ScriptConfigData } from "../src/ScriptConfig.ts";

const mappingDirectory = new URL("../src/transliterationMappings/", import.meta.url);

/** ISO 15924 codes of every script that has a mapping file, except Latin (the source script). */
export function listScriptNames(): string[] {
  return readdirSync(mappingDirectory)
    .filter((fileName) => fileName.endsWith(".json") && fileName !== "latn.json")
    .map((fileName) => fileName.slice(0, -".json".length))
    .sort();
}

export function readScriptConfig(scriptName: string): ScriptConfig {
  const data = JSON.parse(
    readFileSync(new URL(`${scriptName}.json`, mappingDirectory), "utf8")
  ) as ScriptConfigData;
  return parseScriptConfig(scriptName, data);
}

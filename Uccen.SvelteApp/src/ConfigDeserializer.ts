import { parseScriptConfig, type ScriptConfig, type ScriptConfigData } from "./ScriptConfig";

export type { ScriptConfig } from "./ScriptConfig";

// Bundled by Vite; use parseScriptConfig directly outside Vite (e.g. in Node tests).
const configData = import.meta.glob<ScriptConfigData>("./transliterationMappings/*.json", {
  eager: true,
  import: "default"
});

export class ConfigDeserializer {
  static parse(scriptName: string): ScriptConfig {
    const data = configData[`./transliterationMappings/${scriptName}.json`];
    if (!data) {
      throw new Error(`Unable to load configuration for ${scriptName}`);
    }

    return parseScriptConfig(scriptName, data);
  }
}

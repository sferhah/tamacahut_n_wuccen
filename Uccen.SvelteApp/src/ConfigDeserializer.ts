export interface ScriptConfig {
  name: string;
  direction: "ltr" | "rtl";
  geminizer: string;
  nullVowel: string;
  scriptType: string;
  initialSchwa?: string;
  mapping: Record<string, string>;
}

interface ScriptConfigData {
  direction?: "ltr" | "rtl";
  geminizer?: string;
  nullVowel?: string;
  initialSchwa?: string;
  scriptType?: string;
  mapping?: Record<string, string>;
}

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

    const mapping: Record<string, string> = {};
    for (const [key, rawValue] of Object.entries(data.mapping || {})) {
      const value = rawValue.replace(/[\u200E\u200F]/g, "");
      mapping[key] = value;

      if (key.toUpperCase() !== key) {
        mapping[key.toUpperCase()] = value.toUpperCase();
      }
    }

    const config = {
      name: scriptName,
      direction: data.direction || "ltr",
      geminizer: String.fromCharCode(parseInt(data.geminizer || "0x00B2", 16)),
      nullVowel: String.fromCharCode(parseInt(data.nullVowel || "0x00B0", 16)),
      scriptType: data.scriptType || "Alphabet",
      initialSchwa: data.initialSchwa || undefined,
      mapping
    };

    return config;
  }
}

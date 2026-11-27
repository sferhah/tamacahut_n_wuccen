// Pure parsing of a transliteration mapping file (src/transliterationMappings/<script>.json).
// Kept free of Vite-specific APIs so it can run in Node too (tests, generators).

export interface ScriptConfig {
  name: string;
  direction: "ltr" | "rtl";
  geminizer: string;
  nullVowel: string;
  scriptType: string;
  initialSchwa?: string;
  mapping: Record<string, string>;
}

/** Raw content of a mapping file. */
export interface ScriptConfigData {
  direction?: "ltr" | "rtl";
  geminizer?: string;
  nullVowel?: string;
  initialSchwa?: string;
  scriptType?: string;
  mapping?: Record<string, string>;
}

export function parseScriptConfig(scriptName: string, data: ScriptConfigData): ScriptConfig {
  const mapping: Record<string, string> = {};
  for (const [key, rawValue] of Object.entries(data.mapping || {})) {
    const value = rawValue.replace(/[‎‏]/g, "");
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

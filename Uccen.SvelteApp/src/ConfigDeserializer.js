/** @typedef {{ direction?: string, geminizer?: string, nullVowel?: string, scriptType?: string, mapping?: Record<string, string> }} ScriptConfigData */

/** @type {Record<string, ScriptConfigData>} */
const configData = import.meta.glob("./transliterationMappings/*.json", {
  eager: true,
  import: "default"
});

export class ConfigDeserializer {
  /** @param {string} scriptName */
  static parse(scriptName) {
    const data = configData[`./transliterationMappings/${scriptName}.json`];
    if (!data) {
      throw new Error(`Unable to load configuration for ${scriptName}`);
    }

    /** @type {Record<string, string>} */
    const mapping = {};
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
      mapping
    };

    return config;
  }
}

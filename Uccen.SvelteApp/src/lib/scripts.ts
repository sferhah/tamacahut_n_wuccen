// Turning a script choice into a transliteration function.

import { ConfigDeserializer, type ScriptConfig } from "../ConfigDeserializer";
import { TransliterationService } from "../TransliterationService";
import { DEFAULT_SCRIPT, type ScriptOption } from "./options";

export type Transliterate = (text: string) => string;

/** Mapping config for a script, or null for Latin (the source script) or when the config is invalid. */
export function loadScriptConfig(scriptName: string): ScriptConfig | null {
  if (scriptName === DEFAULT_SCRIPT) return null;

  try {
    return ConfigDeserializer.parse(scriptName);
  } catch (error) {
    console.error(`Unable to load configuration for ${scriptName}`, error);
    return null;
  }
}

export function createTransliterator(config: ScriptConfig | null): Transliterate {
  return config ? (text) => TransliterationService.transliterate(text, config) : (text) => text;
}

/** Shows each script's menu entry as "Taqbaylit" written in that script. */
export function withNativeDisplayNames(scripts: ScriptOption[]): ScriptOption[] {
  return scripts.map((script) => {
    if (script.name === DEFAULT_SCRIPT) return script;

    try {
      const config = ConfigDeserializer.parse(script.name);
      return { ...script, displayName: TransliterationService.transliterate("Taqbaylit", config) };
    } catch (error) {
      console.error(`Unable to transliterate the menu for ${script.name}`, error);
      return script;
    }
  });
}

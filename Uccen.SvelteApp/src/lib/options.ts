// Catalog of the scripts the Kabyle text can be shown in, and of the available translations.

export const DEFAULT_SCRIPT = "latn";
export const DEFAULT_TRANSLATION = "und";
/**
 * Bundled in public/fonts and assembled in app.css: Noto Serif for Latin, Cyrillic and Greek,
 * Noto Naskh Arabic for Arabic script, all under this one family name.
 */
export const DEFAULT_FONT_FACE = "'Noto Serif', serif";

export type ScriptOption = {
  /** ISO 15924 code, also the name of the mapping file in transliterationMappings/ */
  name: string;
  displayName: string;
  isRtl: boolean;
  fontFace: string;
  textSizeIncrement: number;
};

export type TranslationOption = {
  key: string;
  pdfCode: string;
  displayName: string;
  isRtl: boolean;
  fontFace: string;
  textSizeIncrement: number;
};

export const SCRIPTS: ScriptOption[] = [
  { name: "latn", displayName: "Taqbaylit", isRtl: false, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 0 },
  { name: "tfng", displayName: "Tifinagh", isRtl: false, fontFace: "Calibri", textSizeIncrement: 0 },
  { name: "arab", displayName: "Arabic", isRtl: true, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 3 },
  { name: "hebr", displayName: "Hebrew", isRtl: true, fontFace: "'Noto Sans Hebrew', sans-serif", textSizeIncrement: 3 },
  { name: "syrc", displayName: "Syriac", isRtl: true, fontFace: "Calibri", textSizeIncrement: 0 },
  { name: "armi", displayName: "Aramaic", isRtl: true, fontFace: "Calibri", textSizeIncrement: 0 },
  { name: "phnx", displayName: "Phoenician", isRtl: true, fontFace: "Calibri", textSizeIncrement: 0 },
  { name: "nbat", displayName: "Nabataean", isRtl: true, fontFace: "'Noto Sans Nabataean', sans-serif", textSizeIncrement: 0 },
  { name: "sarb", displayName: "Musnad", isRtl: true, fontFace: "Calibri", textSizeIncrement: 0 },
  { name: "cyrl", displayName: "Cyrillic", isRtl: false, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 0 },
  { name: "grek", displayName: "Greek", isRtl: false, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 0 }
];

export const TRANSLATIONS: TranslationOption[] = [
  { key: "und", pdfCode: "fra", displayName: "Français", isRtl: false, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 0 },
  { key: "eng", pdfCode: "eng", displayName: "English", isRtl: false, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 0 },
  { key: "arb", pdfCode: "arb", displayName: "عربية فصحى", isRtl: true, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 3 },
  { key: "arq", pdfCode: "arq", displayName: "دارجة دزيرية", isRtl: true, fontFace: DEFAULT_FONT_FACE, textSizeIncrement: 3 }
];

export function isKnownScript(name: string | null): name is string {
  return SCRIPTS.some((script) => script.name === name);
}

export function isKnownTranslation(key: string | null): key is string {
  return TRANSLATIONS.some((translation) => translation.key === key);
}

export function findTranslation(key: string): TranslationOption {
  return TRANSLATIONS.find((translation) => translation.key === key) || TRANSLATIONS[0];
}

export function isArabicTranslation(key: string): boolean {
  return key === "arb" || key === "arq";
}

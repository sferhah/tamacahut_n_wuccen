import locales from "../locales.json";
import { isArabicTranslation } from "./options";

export type UiLocale = (typeof locales)["eng"];
export type TextDirection = "ltr" | "rtl";

/** The interface language follows the selected translation (French by default). */
export function getUiLocale(translationKey: string): UiLocale {
  if (translationKey === "eng") return locales.eng;
  if (isArabicTranslation(translationKey)) return locales.arb;
  return locales.fra;
}

export function getDirection(locale: UiLocale): TextDirection {
  return locale.direction === "rtl" ? "rtl" : "ltr";
}

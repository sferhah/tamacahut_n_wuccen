// Reader preferences: restored from the URL and localStorage, saved back on every change.

import {
  DEFAULT_SCRIPT,
  DEFAULT_TRANSLATION,
  isKnownScript,
  isKnownTranslation
} from "./options";

export const MIN_TEXT_SIZE_INCREMENT = -2;
export const MAX_TEXT_SIZE_INCREMENT = 6;
const MOBILE_VIEWPORT_MAX_WIDTH = 600;

const STORAGE_KEYS = {
  script: "SelectedScript",
  page: "SelectedIndex",
  translation: "SelectedTranslation",
  sourceTextSize: "KabyleTextSizeIncrement",
  translationTextSize: "TranslationTextSizeIncrement",
  initialSizeEvaluated: "InitialTextSizeEvaluated"
};

export type TextSizeTarget = "source" | "translation";

export type Preferences = {
  script: string;
  translation: string;
  page: number;
  sourceTextSize: number;
  translationTextSize: number;
};

/** URL parameters win over saved values, which win over defaults. */
export function restorePreferences(): Preferences {
  shrinkTextOnFirstMobileVisit();

  const urlParams = new URLSearchParams(window.location.search);
  const urlScript = urlParams.get("script");
  const urlTranslation = urlParams.get("trans");
  const urlPage = urlParams.get("page");
  const savedScript = localStorage.getItem(STORAGE_KEYS.script);

  return {
    script: isKnownScript(urlScript) ? urlScript : isKnownScript(savedScript) ? savedScript : DEFAULT_SCRIPT,
    translation: isKnownTranslation(urlTranslation) ? urlTranslation : restoreSavedTranslation(),
    page: urlPage !== null && /^[1-9]\d*$/.test(urlPage)
      ? Number(urlPage) - 1
      : readStoredInteger(STORAGE_KEYS.page, 0, 0, Number.MAX_SAFE_INTEGER),
    sourceTextSize: readTextSize(STORAGE_KEYS.sourceTextSize),
    translationTextSize: readTextSize(STORAGE_KEYS.translationTextSize)
  };
}

export function saveScript(script: string): void {
  localStorage.setItem(STORAGE_KEYS.script, script);
}

export function saveTranslation(translation: string): void {
  localStorage.setItem(STORAGE_KEYS.translation, translation);
}

export function savePage(page: number): void {
  localStorage.setItem(STORAGE_KEYS.page, page.toString());
}

export function saveTextSize(target: TextSizeTarget, size: number): void {
  const key = target === "source" ? STORAGE_KEYS.sourceTextSize : STORAGE_KEYS.translationTextSize;
  localStorage.setItem(key, size.toString());
}

export function clampTextSize(size: number): number {
  return Math.max(MIN_TEXT_SIZE_INCREMENT, Math.min(MAX_TEXT_SIZE_INCREMENT, size));
}

/** Mirrors the current reading position in the address bar so it can be shared. */
export function updateUrl(state: { script: string; translation: string; page: number }): void {
  const url = new URL(window.location.href);
  url.searchParams.set("trans", state.translation);
  url.searchParams.set("script", state.script);
  url.searchParams.set("page", (state.page + 1).toString());
  window.history.replaceState(window.history.state, "", `${url.pathname}${url.search}${url.hash}`);
}

/** On the very first visit from a small screen, start with the smallest text size. */
function shrinkTextOnFirstMobileVisit(): void {
  if (localStorage.getItem(STORAGE_KEYS.initialSizeEvaluated) !== null) return;

  if (
    window.innerWidth <= MOBILE_VIEWPORT_MAX_WIDTH &&
    localStorage.getItem(STORAGE_KEYS.sourceTextSize) === null &&
    localStorage.getItem(STORAGE_KEYS.translationTextSize) === null
  ) {
    saveTextSize("source", MIN_TEXT_SIZE_INCREMENT);
    saveTextSize("translation", MIN_TEXT_SIZE_INCREMENT);
  }
  localStorage.setItem(STORAGE_KEYS.initialSizeEvaluated, "true");
}

function restoreSavedTranslation(): string {
  const saved = localStorage.getItem(STORAGE_KEYS.translation);
  if (saved === null) return getBrowserTranslation();
  if (saved === "fra") return "und"; // legacy key for the French translation
  return isKnownTranslation(saved) ? saved : DEFAULT_TRANSLATION;
}

function getBrowserTranslation(): string {
  const translationByLanguage: Record<string, string> = { en: "eng", fr: "und", ar: "arb" };
  const browserLanguages = [...(navigator.languages || []), navigator.language];

  for (const browserLanguage of browserLanguages) {
    const language = browserLanguage?.trim().toLowerCase().split(/[-_]/)[0] || "";
    const translation = translationByLanguage[language];
    if (isKnownTranslation(translation)) return translation;
  }

  return DEFAULT_TRANSLATION;
}

function readTextSize(key: string): number {
  return readStoredInteger(key, 0, MIN_TEXT_SIZE_INCREMENT, MAX_TEXT_SIZE_INCREMENT);
}

function readStoredInteger(key: string, fallback: number, min: number, max: number): number {
  const storedValue = localStorage.getItem(key);
  if (storedValue === null) return fallback;

  const value = Number(storedValue);
  return Number.isInteger(value) ? Math.max(min, Math.min(max, value)) : fallback;
}

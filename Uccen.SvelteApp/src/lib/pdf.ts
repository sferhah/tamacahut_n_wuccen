import { DEFAULT_SCRIPT, type TranslationOption } from "./options";

/** Scripts the console app builds PDFs for (Uccen.ConsoleApp/Program.cs). */
const PDF_SCRIPTS = new Set(["latn", "arab", "tfng", "cyrl", "grek", "hebr"]);
const PDF_BASE_URL =
  "https://raw.githubusercontent.com/sferhah/tamacahut_n_wuccen/master/Uccen.ConsoleApp/Resources/pdf";

/** URL of the PDF for this script and translation, or null when no PDF exists for the script. */
export function getPdfUrl(scriptName: string, translation: TranslationOption): string | null {
  if (!PDF_SCRIPTS.has(scriptName)) return null;

  const scriptSuffix = scriptName === DEFAULT_SCRIPT ? "" : `_${scriptName}`;
  const translationSuffix =
    translation.pdfCode === "fra" || translation.pdfCode === "und" ? "" : `.${translation.pdfCode}`;
  return `${PDF_BASE_URL}/tamacahut_n_wuccen${scriptSuffix}${translationSuffix}.pdf`;
}

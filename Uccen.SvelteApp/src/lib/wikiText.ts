export type WikiTextSegment = { text: string; formatting: "bold" | "italic" | null };

/** Splits a line on wiki markup: '''bold''' and ''italic''. */
export function splitWikiText(line: string): WikiTextSegment[] {
  const segments: WikiTextSegment[] = [];
  const wikiTextPattern = /'''([\s\S]*?)'''|''([\s\S]*?)''/g;
  let previousIndex = 0;
  let match;

  while ((match = wikiTextPattern.exec(line)) !== null) {
    if (match.index > previousIndex) {
      segments.push({ text: line.slice(previousIndex, match.index), formatting: null });
    }

    segments.push({ text: match[1] ?? match[2], formatting: match[1] !== undefined ? "bold" : "italic" });
    previousIndex = wikiTextPattern.lastIndex;
  }

  if (previousIndex < line.length || segments.length === 0) {
    segments.push({ text: line.slice(previousIndex), formatting: null });
  }

  return segments;
}

export function containsArabicScript(text: string): boolean {
  return /\p{Script=Arabic}/u.test(text);
}

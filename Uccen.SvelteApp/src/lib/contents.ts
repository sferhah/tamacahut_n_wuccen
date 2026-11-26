// Entries of the table of contents.

import type { Story, StoryText } from "../StoryData";
import type { Transliterate } from "./scripts";

export type ContentsItem = {
  index: number;
  sourceTitle: string;
  translationTitle: string;
};

export function buildContentsItems(stories: Story[], transliterate: Transliterate): ContentsItem[] {
  return stories.map((story, index) => ({
    index,
    sourceTitle: getHeading(story.Source, transliterate),
    translationTitle: getHeading(story.Translation, (text) => text)
  }));
}

export function filterContentsItems(items: ContentsItem[], query: string): ContentsItem[] {
  const normalizedQuery = query.trim().toLocaleLowerCase();
  return items.filter((item) =>
    `${item.sourceTitle} ${item.translationTitle}`.toLocaleLowerCase().includes(normalizedQuery)
  );
}

/** The title, or the first paragraph when the title is not readable (e.g. "#12" for an illustration). */
function getHeading(text: StoryText | undefined, transliterate: Transliterate): string {
  const heading = transliterate(text?.Title || "").trim();
  if (isReadableHeading(heading)) return heading;

  return transliterate(text?.Paragraphs?.[0] || "").trim().replace(/\s+/g, " ") || heading;
}

function isReadableHeading(value: string): boolean {
  return /[\p{L}]/u.test(value) && !/^#\d+$/u.test(value.trim());
}

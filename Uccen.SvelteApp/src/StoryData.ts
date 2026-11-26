export type StoryText = { Title: string; Paragraphs: string[] };

export type Story = {
  Source: StoryText;
  Translation: StoryText;
};

type Book = {
  Tales: Story[];
};

export type StoryParagraphs = {
  Source: { Paragraphs: string[] };
};

export function discardEmptyTales<T extends StoryParagraphs>(tales: T[]): T[] {
  return tales.filter((tale) =>
    tale.Source.Paragraphs.some((paragraph) => paragraph !== "")
  );
}

/** Loads the book for a translation ("und" is the default French edition). */
export async function fetchTales(translationKey: string): Promise<Story[]> {
  const fileName = translationKey === "und"
    ? "tamacahut_n_wuccen.json"
    : `tamacahut_n_wuccen.${translationKey}.json`;
  const response = await fetch(`data/${fileName}`);
  if (!response.ok) {
    throw new Error(`Story request failed with status ${response.status}`);
  }

  const responseData: unknown = await response.json();
  if (!isBook(responseData)) {
    throw new Error("Story data must be a book containing valid tales");
  }

  return discardEmptyTales(responseData.Tales);
}

/** Tales whose title is "#<n>" are illustrated by /images/<n>.svg. */
export function getIllustrationId(story: Story | undefined): string | null {
  return story?.Source.Title.match(/^#(\d+)/)?.[1] ?? null;
}

export function transliterateStory(story: Story, transliterate: (text: string) => string): Story {
  return {
    Source: {
      Title: transliterate(story.Source.Title),
      Paragraphs: story.Source.Paragraphs.map(transliterate)
    },
    Translation: story.Translation
  };
}

function isBook(value: unknown): value is Book {
  if (typeof value !== "object" || value === null) return false;
  const book = value as Partial<Book>;
  return Array.isArray(book.Tales) && book.Tales.every(isStory);
}

function isStory(value: unknown): value is Story {
  if (typeof value !== "object" || value === null) return false;
  const story = value as Partial<Story>;
  return isStoryText(story.Source) && isStoryText(story.Translation);
}

function isStoryText(value: Partial<StoryText> | undefined): value is StoryText {
  return typeof value?.Title === "string" &&
    Array.isArray(value.Paragraphs) &&
    value.Paragraphs.every((paragraph) => typeof paragraph === "string");
}

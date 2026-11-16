export type StoryParagraphs = {
  Source: { Paragraphs: string[] };
};

export function discardEmptyTales<T extends StoryParagraphs>(tales: T[]): T[] {
  return tales.filter((tale) =>
    tale.Source.Paragraphs.some((paragraph) => paragraph !== "")
  );
}

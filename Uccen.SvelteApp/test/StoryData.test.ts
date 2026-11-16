import assert from "node:assert/strict";
import test from "node:test";
import { discardEmptyTales } from "../src/StoryData.ts";

test("discards tales with only empty source paragraphs", () => {
  const emptyTale = {
    Source: { Paragraphs: ["", ""] },
    Translation: { Paragraphs: [""] }
  };
  const sourceOnlyTale = {
    Source: { Paragraphs: ["A source paragraph"] },
    Translation: { Paragraphs: [""] }
  };
  const translationOnlyTale = {
    Source: { Paragraphs: [""] },
    Translation: { Paragraphs: ["A translated paragraph"] }
  };

  assert.deepEqual(
    discardEmptyTales([emptyTale, sourceOnlyTale, translationOnlyTale]),
    [sourceOnlyTale]
  );
});

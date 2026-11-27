<!--
  The two-column reading table: Kabyle source on the left, translation on the right.
  The column controls (pickers, text size) are passed in as the `sourceControls`
  and `translationControls` snippets.
-->
<script module lang="ts">
  export type ColumnStyle = {
    fontFace: string;
    sizeIncrement: number;
    dir: "ltr" | "rtl";
    /** Text is written in Arabic script (italics are shown upright). */
    arabicScript: boolean;
  };
</script>

<script lang="ts">
  import type { Snippet } from "svelte";
  import { cubicOut } from "svelte/easing";
  import { fly } from "svelte/transition";
  import type { Story } from "../StoryData";
  import RichText from "./RichText.svelte";

  type Props = {
    story: Story;
    source: ColumnStyle;
    translation: ColumnStyle;
    illustrationId?: string | null;
    sourceControls: Snippet;
    translationControls: Snippet;
    /** Changing it plays the page-turn animation (the page number). */
    pageKey: number;
    /** Horizontal offset, in px, the new page slides in from: > 0 from the right, < 0 from the left. */
    slideFrom?: number;
  };

  let {
    story,
    source,
    translation,
    illustrationId = null,
    sourceControls,
    translationControls,
    pageKey,
    slideFrom = 0
  }: Props = $props();

  const reduceMotion =
    typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  const paragraphsAligned = $derived(
    story.Source.Paragraphs.length === story.Translation.Paragraphs.length
  );

  function titleStyle(column: ColumnStyle): string {
    return `font-family: ${column.fontFace}; font-size: ${0.9 + column.sizeIncrement * 0.15}rem;`;
  }

  function textStyle(column: ColumnStyle): string {
    return `font-family: ${column.fontFace}; font-size: ${1 + column.sizeIncrement * 0.15}rem;`;
  }
</script>

<table class="table">
  <thead>
    <tr>
      <td width="50%">{@render sourceControls()}</td>
      <td width="50%">{@render translationControls()}</td>
    </tr>
    <tr>
      <th style="width: 50%">
        <h2 style={titleStyle(source)} dir={source.dir}>{story.Source.Title}</h2>
      </th>
      <th style="width: 50%">
        <h2 style={titleStyle(translation)} dir={translation.dir}>{story.Translation.Title}</h2>
      </th>
    </tr>
  </thead>

  {#key pageKey}
  <!-- Page turn: the new page slides in (just fades in when the reader prefers reduced motion). -->
  <tbody
    class:centered={story.Source.Title.startsWith("#")}
    in:fly={{ x: reduceMotion ? 0 : slideFrom, duration: 250, easing: cubicOut }}
  >
    {#if paragraphsAligned}
      <!-- One row per paragraph, so each paragraph sits next to its translation. -->
      {#each story.Source.Paragraphs as sourceParagraph, i}
        <tr>
          {#if sourceParagraph !== ""}
            <td>
              <div class="text-content" style={textStyle(source)} dir={source.dir}>
                <RichText text={sourceParagraph} arabicScript={source.arabicScript} />
              </div>
            </td>
          {/if}
          <td colspan={sourceParagraph === "" ? 2 : 1}>
            <div class="text-content" style={textStyle(translation)} dir={translation.dir}>
              <RichText text={story.Translation.Paragraphs[i]} arabicScript={translation.arabicScript} />
            </div>
          </td>
        </tr>
      {/each}
    {:else}
      <!-- Paragraph counts differ: show each side as one block. -->
      <tr>
        <td>
          <div class="text-content" style={textStyle(source)} dir={source.dir}>
            {#each story.Source.Paragraphs as paragraph}
              <p><RichText text={paragraph} arabicScript={source.arabicScript} /></p>
            {/each}
          </div>
        </td>
        <td>
          <div class="text-content" style={textStyle(translation)} dir={translation.dir}>
            {#each story.Translation.Paragraphs as paragraph}
              <p><RichText text={paragraph} arabicScript={translation.arabicScript} /></p>
            {/each}
          </div>
        </td>
      </tr>
    {/if}
    {#if illustrationId !== null}
      <tr>
        <td colspan="2" class="illustration-cell">
          <img src={`/images/${illustrationId}.svg`} alt={`Illustration for tale ${illustrationId}`} />
        </td>
      </tr>
    {/if}
  </tbody>
  {/key}
</table>

<style>
  .table {
    width: 100%;
    border-collapse: collapse;
  }
  .table thead {
    position: sticky;
    top: 0;
    z-index: 2;
    background: white;
  }
  .table thead tr:first-child > td {
    padding-top: 6px;
    padding-bottom: 6px;
  }
  .table thead th {
    padding-top: 4px;
    padding-bottom: 4px;
  }
  .table thead h2 {
    margin: 0;
  }
  td, th {
    padding: 12px;
    vertical-align: top;
    border-bottom: 1px solid #ddd;
  }
  .table tr > :nth-child(2) {
    border-left: 2px dashed #bbb;
  }
  .table tbody tr:last-child > td {
    border-bottom: none;
  }
  .text-content {
    line-height: 1.6;
  }
  .centered .text-content {
    text-align: center;
  }
  .illustration-cell {
    text-align: center;
  }
  .illustration-cell img {
    display: block;
    width: min(100%, 600px);
    max-height: 32rem;
    margin: 1.5rem auto;
    object-fit: contain;
  }
  .table td, .table th {
    padding: 12px 16px;
  }

  @media (max-width: 600px) {
    .table td, .table th {
      padding: 8px;
    }
  }
</style>

<!-- One paragraph: line breaks become <br>, '''bold''' and ''italic'' wiki markup is rendered. -->
<script lang="ts">
  import { containsArabicScript, splitWikiText } from "../lib/wikiText";

  type Props = {
    text: string;
    /** The surrounding text is in Arabic script, where italics are shown upright. */
    arabicScript?: boolean;
  };

  let { text, arabicScript = false }: Props = $props();
</script>

{#each text.split('\n') as line}
  {#each splitWikiText(line) as segment}
    {#if segment.formatting === "italic"}<em class:arabic-italic={containsArabicScript(segment.text)} class:arabic-script={arabicScript}>{segment.text}</em>{:else if segment.formatting === "bold"}<strong>{segment.text}</strong>{:else}{segment.text}{/if}
  {/each}<br />
{/each}

<style>
  .arabic-italic {
    background-color: #f5f5f5;
  }

  .arabic-script {
    font-style: normal;
  }
</style>

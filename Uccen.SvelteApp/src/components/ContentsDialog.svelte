<!-- Table of contents modal with a search box. -->
<script lang="ts">
  import { filterContentsItems, type ContentsItem } from "../lib/contents";
  import { isBackdropClick } from "../lib/browser";
  import { getDirection, type UiLocale } from "../lib/locale";

  export let items: ContentsItem[];
  export let page: number;
  export let locale: UiLocale;
  export let onselect: (page: number) => void;

  let dialog: HTMLDialogElement;
  let search = "";

  $: filteredItems = filterContentsItems(items, search);

  export function open(): void {
    search = "";
    dialog.showModal();
    dialog.querySelector(`[data-contents-page="${page}"]`)?.scrollIntoView({ block: "nearest" });
  }

  function close(): void {
    dialog.close();
  }

  function select(index: number): void {
    close();
    onselect(index);
  }
</script>

<dialog
  bind:this={dialog}
  class="modal contents-modal"
  aria-labelledby="contents-title"
  aria-describedby="contents-summary"
  lang={locale.lang}
  dir={getDirection(locale)}
  tabindex="-1"
  on:click={(event) => isBackdropClick(dialog, event) && close()}
>
  <div class="contents-header">
    <div>
      <p class="contents-eyebrow">{locale.storyIndex}</p>
      <h3 id="contents-title">{locale.contents}</h3>
    </div>
    <button on:click={close} class="close-btn" aria-label={locale.closeContents}>&times;</button>
  </div>
  <div class="contents-body">
    <div class="contents-summary-row">
      <p id="contents-summary">{locale.contentsSummary}</p>
      <span class="contents-count">{locale.entries.replace("{count}", String(items.length))}</span>
    </div>
    <label class="contents-search">
      <svg aria-hidden="true" viewBox="0 0 24 24">
        <circle cx="10.8" cy="10.8" r="6.3" />
        <path d="m15.5 15.5 4.2 4.2" />
      </svg>
      <input bind:value={search} type="search" placeholder={locale.findStory} aria-label={locale.findStory} />
    </label>
    {#if filteredItems.length > 0}
      <ol class="contents-list">
        {#each filteredItems as item}
          <li>
            <button
              type="button"
              class:current={item.index === page}
              aria-current={item.index === page ? "page" : undefined}
              data-contents-page={item.index}
              on:click={() => select(item.index)}
            >
              <span class="contents-index">{String(item.index + 1).padStart(2, "0")}</span>
              <span class="contents-entry">
                <span class="contents-source">{item.sourceTitle}</span>
                {#if item.translationTitle && item.translationTitle !== item.sourceTitle}
                  <span class="contents-translation">{item.translationTitle}</span>
                {/if}
              </span>
              {#if item.index === page}<span class="now-reading">{locale.nowReading}</span>{/if}
            </button>
          </li>
        {/each}
      </ol>
    {:else}
      <p class="contents-empty" role="status">{locale.noSearchResults.replace("{query}", search)}</p>
    {/if}
  </div>
</dialog>

<style>
  .contents-modal {
    width: min(560px, calc(100vw - 2rem));
    max-width: 560px;
    max-height: min(82vh, 760px);
    overflow: hidden;
    border: 1px solid #e8e8e8;
    border-radius: 12px;
    color: #24343c;
  }

  .contents-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 1.1rem 1.35rem;
    border-bottom: 1px solid #e8e8e8;
    background: #fff;
  }

  .contents-header h3 {
    margin: 0;
    font-size: 1.35rem;
    letter-spacing: -0.02em;
  }

  .contents-eyebrow {
    margin: 0 0 0.2rem;
    color: #718087;
    font-size: 0.67rem;
    font-weight: 700;
    letter-spacing: 0.12em;
  }

  .contents-header .close-btn {
    width: 36px;
    height: 36px;
    border-radius: 6px;
    color: #5b686e;
  }

  .contents-body {
    padding: 1rem 1.35rem 1.25rem;
  }

  .contents-summary-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    margin-bottom: 0.9rem;
  }

  .contents-summary-row p {
    margin: 0;
    color: #637178;
    font-size: 0.88rem;
  }

  .contents-count {
    flex: 0 0 auto;
    color: #718087;
    font-size: 0.75rem;
    font-variant-numeric: tabular-nums;
  }

  .contents-search {
    display: flex;
    align-items: center;
    gap: 0.6rem;
    height: 42px;
    padding: 0 0.75rem;
    border: 1px solid #dfe4e6;
    border-radius: 7px;
    background: #fafbfb;
  }

  .contents-search:focus-within {
    border-color: #aab8bf;
    outline: 2px solid #e7edef;
  }

  .contents-search svg {
    width: 18px;
    height: 18px;
    flex: 0 0 auto;
    fill: none;
    stroke: #718087;
    stroke-linecap: round;
    stroke-width: 1.7;
  }

  .contents-search input {
    width: 100%;
    border: 0;
    outline: 0;
    background: transparent;
    color: inherit;
    font: inherit;
    font-size: 0.9rem;
  }

  .contents-list {
    max-height: min(52vh, 470px);
    overflow-y: auto;
    margin: 0.85rem -0.45rem 0;
    padding: 0 0.45rem;
    list-style: none;
    overscroll-behavior: contain;
  }

  .contents-list li + li {
    margin-top: 0.2rem;
  }

  .contents-list button {
    display: flex;
    align-items: center;
    gap: 0.85rem;
    width: 100%;
    min-height: 58px;
    padding: 0.65rem 0.7rem;
    border-color: transparent;
    border-radius: 7px;
    text-align: start;
  }

  .contents-list button:hover {
    background: #f5f7f7;
  }

  .contents-list button.current {
    border-color: #cbded9;
    background: #f3f8f6;
  }

  .contents-index {
    width: 2rem;
    flex: 0 0 auto;
    color: #859197;
    font-size: 0.74rem;
    font-variant-numeric: tabular-nums;
    text-align: center;
  }

  .contents-entry {
    display: flex;
    min-width: 0;
    flex: 1;
    flex-direction: column;
    gap: 0.15rem;
  }

  .contents-source,
  .contents-translation {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .contents-source {
    color: #29383f;
    font-size: 0.92rem;
    font-weight: 600;
  }

  .contents-translation {
    color: #778389;
    font-size: 0.78rem;
  }

  .current .contents-index,
  .current .contents-source {
    color: #3F716B;
  }

  .now-reading {
    flex: 0 0 auto;
    color: #3F716B;
    font-size: 0.61rem;
    font-weight: 700;
    letter-spacing: 0.06em;
  }

  .contents-empty {
    margin: 1.5rem 0;
    color: #637178;
    text-align: center;
  }

  @media (max-width: 600px) {
    .contents-modal {
      max-height: 88vh;
    }

    .contents-header {
      padding: 0.9rem 1rem;
    }

    .contents-body {
      padding: 0.9rem 1rem 1rem;
    }

    .contents-list {
      max-height: 57vh;
    }
  }
</style>

<!-- Fixed bottom bar: reading progress, contents button, page arrows, about button. -->
<script lang="ts">
  import { getDirection, type UiLocale } from "../lib/locale";

  type Props = {
    page: number;
    pageCount: number;
    locale: UiLocale;
    onpagechange: (page: number) => void;
    oncontents: () => void;
    onabout: () => void;
  };

  let { page, pageCount, locale, onpagechange, oncontents, onabout }: Props = $props();

  const pageLabel = $derived(
    locale.pageOf.replace("{current}", String(page + 1)).replace("{total}", String(pageCount))
  );
</script>

<nav class="pagination" aria-label={locale.storyNavigation} dir={getDirection(locale)}>
  <div
    class="reading-progress"
    role="progressbar"
    aria-label={locale.readingProgress}
    aria-valuemin="0"
    aria-valuemax={pageCount}
    aria-valuenow={page + 1}
  >
    <span class="reading-progress-fill" style={`width: ${((page + 1) / pageCount) * 100}%`}></span>
  </div>
  <div class="pagination-tools">
    <button onclick={oncontents} class="contents-btn" aria-label={locale.tableOfContents} title={locale.tableOfContents}>
      <svg aria-hidden="true" viewBox="0 0 24 24" class="contents-icon">
        <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v16H6.5A2.5 2.5 0 0 0 4 21.5v-16ZM4 18.5A2.5 2.5 0 0 1 6.5 16H20M8 7h8M8 10h8" />
      </svg>
      <span class="contents-label">{locale.contents}</span>
    </button>
  </div>
  <div class="pagination-controls">
    <button onclick={() => onpagechange(page - 1)} disabled={page <= 0} aria-label={locale.previousPage}>
      <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-caret">
        <path d="M14 7l-5 5 5 5V7z" />
      </svg>
    </button>
    <span class="page-number" aria-label={pageLabel}>
      <span>{String(page + 1).padStart(2, "0")}</span>
      <span class="page-separator" aria-hidden="true">/</span>
      <span>{pageCount}</span>
    </span>
    <button onclick={() => onpagechange(page + 1)} disabled={page >= pageCount - 1} aria-label={locale.nextPage}>
      <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-caret">
        <path d="M10 17l5-5-5-5v10z" />
      </svg>
    </button>
  </div>
  <button onclick={onabout} class="help-btn pagination-help" aria-label={locale.about} title={locale.about}>
    <span aria-hidden="true">i</span>
  </button>
</nav>

<style>
  .pagination {
    position: fixed;
    right: 0;
    bottom: 0;
    left: 0;
    z-index: 3;
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
    align-items: center;
    padding: 0.75rem 1rem;
    background: white;
    border-top: 1px solid #ddd;
  }
  .reading-progress {
    position: absolute;
    top: -1px;
    right: 0;
    left: 0;
    height: 3px;
    overflow: hidden;
    background: #eee;
  }
  .reading-progress-fill {
    display: block;
    height: 100%;
    background: #3F716B;
    transition: width 240ms ease;
  }
  .pagination-tools,
  .pagination-controls {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .pagination-tools {
    grid-column: 1;
    justify-self: start;
  }
  .pagination-controls {
    grid-column: 2;
  }
  .pagination-help,
  .contents-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.4rem;
    min-height: 40px;
    padding: 0 0.75rem;
    border-color: #e2e2e2;
    border-radius: 6px;
    color: #36434a;
  }
  .pagination-help {
    grid-column: 3;
    justify-self: end;
    width: 40px;
    padding: 0;
    margin: 0;
  }
  .contents-btn {
    font-size: 0.875rem;
    font-weight: 600;
  }
  .contents-icon {
    width: 18px;
    height: 18px;
    fill: none;
    stroke: currentColor;
    stroke-linecap: round;
    stroke-linejoin: round;
    stroke-width: 1.6;
  }
  .pagination-tools button:hover,
  .pagination-controls button:hover:not(:disabled) {
    border-color: #b6c0c5;
    background: #f7f8f8;
  }
  .page-number {
    display: inline-flex;
    align-items: baseline;
    justify-content: center;
    gap: 0.35rem;
    min-width: 5.2rem;
    color: #24343c;
    font-size: 1rem;
    font-variant-numeric: tabular-nums;
    font-weight: 700;
    user-select: none;
  }
  .page-separator {
    color: #9ba4a8;
    font-size: 0.8rem;
    font-weight: 400;
  }
  /* Keep SVG arrows consistent */
  .icon-caret {
    width: 24px;
    height: 24px;
    fill: currentColor; /* Match the button text color, including its disabled state */
    vertical-align: middle;
  }
  .pagination[dir="rtl"] .icon-caret {
    transform: scaleX(-1);
  }
  .pagination button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 6px 12px;
    width: 80px;
  }
  .pagination-tools button {
    width: auto;
  }

  @media (max-width: 600px) {
    .pagination {
      padding: 0.65rem 0.5rem;
    }

    .pagination-tools {
      gap: 0.35rem;
    }

    .pagination-help,
    .contents-btn {
      width: 38px;
      height: 38px;
      min-height: 38px;
      padding: 0;
    }

    .contents-label {
      display: none;
    }

    .pagination-controls {
      gap: 0.25rem;
    }

    .pagination-controls button {
      width: 48px;
      padding: 6px;
    }

    .page-number {
      min-width: 4.5rem;
      font-size: 0.92rem;
    }
  }

  .help-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 6px;
    margin-left: 6px;
    vertical-align: middle;
    border: 1px solid #ccc;
    border-radius: 4px;
    background: white;
    cursor: pointer;
    box-sizing: border-box;
    height: 32px; /* Match the plus and minus buttons */
    width: 32px;
  }

  .help-btn:hover {
    border-color: #ff3e00;
    background-color: #fffbf9;
  }

  .help-btn span {
    color: #444;
    font-size: 20px;
    font-weight: 700;
    line-height: 1;
  }

  .help-btn:hover span {
    color: #ff3e00;
  }
</style>

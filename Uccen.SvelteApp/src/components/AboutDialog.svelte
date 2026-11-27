<!-- "About" modal: short description, PDF download for the current script/translation, source code link. -->
<script lang="ts">
  import { isBackdropClick } from "../lib/browser";
  import { getDirection, type UiLocale } from "../lib/locale";

  type Props = {
    locale: UiLocale;
    /** null when no PDF exists for the current script */
    pdfUrl: string | null;
    scriptName: string;
    translationName: string;
  };

  let { locale, pdfUrl, scriptName, translationName }: Props = $props();

  let dialog: HTMLDialogElement;

  export function open(): void {
    dialog.showModal();
  }

  function close(): void {
    dialog.close();
  }
</script>

<dialog
  bind:this={dialog}
  class="modal about-modal"
  aria-labelledby="about-title"
  lang={locale.lang}
  dir={getDirection(locale)}
  onclick={(event) => isBackdropClick(dialog, event) && close()}
>
  <div class="modal-header">
    <h3 id="about-title">{locale.about}</h3>
    <button onclick={close} class="close-btn" aria-label={locale.close}>&times;</button>
  </div>

  <div class="modal-body">
    <p class="about-description">{locale.aboutDescription}</p>
    <ul class="about-links">
      <li>
        {#if pdfUrl}
          <a class="about-link" href={pdfUrl}>
            <img src="/images/pdf.svg" alt="" />
            <span class="about-link-text">
              <span class="about-link-label">{locale.pdfVersion}</span>
              <span class="about-link-detail"><bdi>{scriptName}</bdi> · <bdi>{translationName}</bdi></span>
            </span>
          </a>
        {:else}
          <div class="about-link about-link-disabled">
            <img src="/images/pdf.svg" alt="" />
            <span class="about-link-text">
              <span class="about-link-label">{locale.pdfVersion}</span>
              <span class="about-link-detail">{locale.pdfUnavailable}</span>
            </span>
          </div>
        {/if}
      </li>
      <li>
        <a class="about-link" href="https://github.com/sferhah/tamacahut_n_wuccen" target="_blank" rel="noopener noreferrer">
          <svg aria-hidden="true" viewBox="0 0 24 24" class="about-icon">
            <path d="m8 17-5-5 5-5M16 7l5 5-5 5M14 4l-4 16" />
          </svg>
          <span class="about-link-text">
            <span class="about-link-label">{locale.sourceCode} <span aria-hidden="true">↗</span></span>
          </span>
        </a>
      </li>
    </ul>
  </div>
</dialog>

<style>
  .about-modal {
    width: min(440px, calc(100vw - 2rem));
    color: #24343c;
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    background: #f4f4f4;
    border-bottom: 1px solid #ddd;
  }

  .modal-header h3 {
    margin: 0;
  }

  .modal-body {
    min-height: 0;
    flex: 1 1 auto;
    padding: 1rem;
    padding-bottom: calc(1rem + env(safe-area-inset-bottom, 0px));
    overflow: auto;
  }

  .about-description {
    margin: 0 0 1rem;
    line-height: 1.5;
    color: #36434a;
  }

  .about-links {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    margin: 0;
    padding: 0;
    list-style: none;
  }

  .about-link {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.65rem 0.75rem;
    border: 1px solid #e2e2e2;
    border-radius: 8px;
    color: #24343c;
    text-decoration: none;
  }

  a.about-link:hover,
  a.about-link:focus-visible {
    border-color: #b6c0c5;
    background: #f7f8f8;
  }

  .about-link img,
  .about-icon {
    flex: 0 0 auto;
    width: 24px;
    height: 24px;
  }

  .about-icon {
    fill: none;
    stroke: #3F716B;
    stroke-width: 2;
    stroke-linecap: round;
    stroke-linejoin: round;
  }

  .about-link-text {
    display: flex;
    flex-direction: column;
    gap: 0.15rem;
    min-width: 0;
  }

  .about-link-label {
    font-weight: 600;
    font-size: 0.95rem;
  }

  .about-link-detail {
    font-size: 0.8rem;
    color: #637178;
  }

  .about-link-disabled {
    opacity: 0.6;
  }
</style>

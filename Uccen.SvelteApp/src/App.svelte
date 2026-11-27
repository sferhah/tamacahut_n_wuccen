<script lang="ts">
  import { onMount, tick } from "svelte";
  import type { ScriptConfig } from "./ConfigDeserializer";
  import { fetchTales, getIllustrationId, transliterateStory, type Story } from "./StoryData";
  import AboutDialog from "./components/AboutDialog.svelte";
  import ContentsDialog from "./components/ContentsDialog.svelte";
  import PaginationBar from "./components/PaginationBar.svelte";
  import Picker from "./components/Picker.svelte";
  import StoryView, { type ColumnStyle } from "./components/StoryView.svelte";
  import TextSizeControls from "./components/TextSizeControls.svelte";
  import { isAndroidDevice, isIOSDevice } from "./lib/browser";
  import { buildContentsItems } from "./lib/contents";
  import { getDirection, getUiLocale } from "./lib/locale";
  import {
    DEFAULT_FONT_FACE,
    DEFAULT_SCRIPT,
    DEFAULT_TRANSLATION,
    SCRIPTS,
    TRANSLATIONS,
    findTranslation,
    isArabicTranslation
  } from "./lib/options";
  import { getPdfUrl } from "./lib/pdf";
  import * as preferences from "./lib/preferences";
  import { createTransliterator, loadScriptConfig, withNativeDisplayNames } from "./lib/scripts";
  import { swipe, type SwipeDirection } from "./lib/swipe";

  // ── State ────────────────────────────────────────────────────────────────
  // $state.raw: these arrays are only ever replaced, never mutated, so deep tracking is unnecessary.
  let tales: Story[] = $state.raw([]);
  let scripts = $state.raw(SCRIPTS);
  let page = $state(0);
  let selectedScript = $state(DEFAULT_SCRIPT);
  let selectedTranslation = $state(DEFAULT_TRANSLATION);
  /** null when the Kabyle text is shown as-is (Latin) */
  let scriptConfig = $state.raw<ScriptConfig | null>(null);
  let sourceTextSize = $state(0);
  let translationTextSize = $state(0);

  let loading = $state(true);
  let loadingError = $state("");
  let translationError = $state("");
  let useCustomPickers = $state(false);
  let isIOS = false;

  let aboutDialog = $state<ReturnType<typeof AboutDialog>>();
  let contentsDialog = $state<ReturnType<typeof ContentsDialog>>();

  // ── Derived ──────────────────────────────────────────────────────────────
  const uiLocale = $derived(getUiLocale(selectedTranslation));
  $effect(() => {
    document.documentElement.lang = uiLocale.lang;
  });

  const currentScript = $derived(scripts.find((script) => script.name === selectedScript) || scripts[0]);
  const currentTranslation = $derived(findTranslation(selectedTranslation));
  const transliterate = $derived(createTransliterator(scriptConfig));

  const story = $derived(tales[page] ? transliterateStory(tales[page], transliterate) : null);
  const contentsItems = $derived(buildContentsItems(tales, transliterate));
  const pdfUrl = $derived(getPdfUrl(selectedScript, currentTranslation));

  const scriptOptions = $derived(
    scripts.map((script) => ({ value: script.name, label: script.displayName }))
  );
  const translationOptions = TRANSLATIONS.map((translation) => ({
    value: translation.key,
    label: translation.displayName
  }));

  const sourceStyle: ColumnStyle = $derived({
    fontFace: scriptConfig ? currentScript.fontFace : DEFAULT_FONT_FACE,
    sizeIncrement: sourceTextSize + currentScript.textSizeIncrement,
    dir: scriptConfig?.direction === "rtl" ? "rtl" : "ltr",
    arabicScript: selectedScript === "arab"
  });

  const translationStyle: ColumnStyle = $derived({
    fontFace: currentTranslation.fontFace,
    sizeIncrement: translationTextSize + currentTranslation.textSizeIncrement,
    dir: currentTranslation.isRtl ? "rtl" : "ltr",
    arabicScript: isArabicTranslation(selectedTranslation)
  });

  // ── Startup ──────────────────────────────────────────────────────────────
  onMount(() => {
    useCustomPickers = isAndroidDevice();
    isIOS = isIOSDevice();
    initialize();
  });

  async function initialize(): Promise<void> {
    try {
      const saved = preferences.restorePreferences();
      selectedTranslation = saved.translation;
      sourceTextSize = saved.sourceTextSize;
      translationTextSize = saved.translationTextSize;

      const loadedTales = await loadTales(saved.translation);
      if (!loadedTales) {
        throw new Error("Initial story request was superseded");
      }
      tales = loadedTales;
      page = clampPage(saved.page);
      preferences.savePage(page);
      changeScript(saved.script);
      scripts = withNativeDisplayNames(SCRIPTS);
    } catch (error) {
      loadingError = uiLocale.storyLoadError;
      console.error("Application initialization error", error);
    } finally {
      loading = false;
    }
  }

  // ── Actions ──────────────────────────────────────────────────────────────
  let storyRequestId = 0;

  /** Resolves to null when a newer request was started in the meantime. */
  async function loadTales(translationKey: string): Promise<Story[] | null> {
    const requestId = ++storyRequestId;
    const loadedTales = await fetchTales(translationKey);
    return requestId === storyRequestId ? loadedTales : null;
  }

  function changeScript(scriptName: string): void {
    selectedScript = scriptName;
    scriptConfig = loadScriptConfig(scriptName);
    preferences.saveScript(scriptName);
    updateUrl();
  }

  async function changeTranslation(translationKey: string): Promise<void> {
    try {
      const loadedTales = await loadTales(translationKey);
      if (!loadedTales) return;

      tales = loadedTales;
      selectedTranslation = translationKey;
      preferences.saveTranslation(translationKey);
      page = clampPage(page);
      preferences.savePage(page);
      translationError = "";
      updateUrl();
    } catch (error) {
      translationError = uiLocale.translationLoadError;
      console.error(`Unable to load the ${translationKey} translation`, error);
    }
  }

  async function goToPage(nextPage: number): Promise<void> {
    if (nextPage < 0 || nextPage >= tales.length) return;

    page = nextPage;
    preferences.savePage(page);
    updateUrl();
    await tick();
    if (isIOS) {
      window.scrollTo(0, 0);
    } else {
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  }

  /** Swiping left turns to the next page, like the bottom bar's arrows (mirrored for right-to-left). */
  function handleSwipe(direction: SwipeDirection): void {
    const towardsNext = getDirection(uiLocale) === "rtl" ? "right" : "left";
    goToPage(direction === towardsNext ? page + 1 : page - 1);
  }

  function changeTextSize(target: preferences.TextSizeTarget, size: number): void {
    const nextSize = preferences.clampTextSize(size);
    if (target === "source") {
      sourceTextSize = nextSize;
    } else {
      translationTextSize = nextSize;
    }
    preferences.saveTextSize(target, nextSize);
  }

  function clampPage(value: number): number {
    return Math.min(value, Math.max(0, tales.length - 1));
  }

  function updateUrl(): void {
    preferences.updateUrl({ script: selectedScript, translation: selectedTranslation, page });
  }
</script>

{#if loading}
  <div class="loader" lang={uiLocale.lang} dir={getDirection(uiLocale)}><p><em>{uiLocale.loading}</em></p></div>
{:else if loadingError}
  <div class="loader" role="alert" lang={uiLocale.lang} dir={getDirection(uiLocale)}><p>{loadingError}</p></div>
{:else if !story}
  <div class="loader" lang={uiLocale.lang} dir={getDirection(uiLocale)}><p>{uiLocale.noStories}</p></div>
{:else}
  <main lang={uiLocale.lang} dir="ltr" use:swipe={{ onswipe: handleSwipe }}>
    <StoryView
      {story}
      source={sourceStyle}
      translation={translationStyle}
      illustrationId={getIllustrationId(tales[page])}
    >
      {#snippet sourceControls()}
        <Picker
          options={scriptOptions}
          value={selectedScript}
          label={uiLocale.transliteration}
          custom={useCustomPickers}
          onchange={changeScript}
        />
        <TextSizeControls
          size={sourceTextSize}
          decreaseLabel={uiLocale.decreaseSize}
          increaseLabel={uiLocale.increaseSize}
          onchange={(size) => changeTextSize("source", size)}
        />
      {/snippet}

      {#snippet translationControls()}
        <Picker
          options={translationOptions}
          value={selectedTranslation}
          label={uiLocale.translation}
          custom={useCustomPickers}
          onchange={changeTranslation}
        />
        {#if translationError}
          <span role="alert">{translationError}</span>
        {/if}
        <TextSizeControls
          size={translationTextSize}
          decreaseLabel={uiLocale.decreaseSize}
          increaseLabel={uiLocale.increaseSize}
          onchange={(size) => changeTextSize("translation", size)}
        />
      {/snippet}
    </StoryView>

    <PaginationBar
      {page}
      pageCount={tales.length}
      locale={uiLocale}
      onpagechange={goToPage}
      oncontents={() => contentsDialog?.open()}
      onabout={() => aboutDialog?.open()}
    />
  </main>

  <ContentsDialog bind:this={contentsDialog} items={contentsItems} {page} locale={uiLocale} onselect={goToPage} />

  <AboutDialog
    bind:this={aboutDialog}
    locale={uiLocale}
    {pdfUrl}
    scriptName={currentScript.displayName}
    translationName={currentTranslation.displayName}
  />
{/if}

<style>
  main {
    max-width: 1200px;
    min-height: 100vh;
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    margin: 0 auto;
    padding: 1rem 1rem 6rem;
    font-family: system-ui, sans-serif;
  }

  .loader {
    text-align: center;
    padding: 3rem;
    font-family: sans-serif;
  }

  @media (max-width: 600px) {
    main {
      padding: 0.75rem 0.5rem 6rem;
    }
  }
</style>

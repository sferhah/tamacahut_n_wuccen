<script>
  import { onMount } from 'svelte';
  import { ConfigDeserializer } from './ConfigDeserializer.js';
  import { TransliterationService } from './TransliterationService.js';


  
  // Main state
  let tales = [];
  let tale = null;
  let loading = true;
  
  // User state (saved in localStorage)
  let page = 0;
  let selectedScript = "Kabyle";
  let selectedTranslation = "Français";
  let kabyleTextSizeIncrement = 0;
  let translationTextSizeIncrement = 0;

  // Configuration
  const translations = ["Français", "English"];
  const faceDefault = "Consolas, Monaco, Menlo, 'Droid Sans Mono', 'Inconsolata', 'Courier New'";
  
let scripts = [
  { name: "Kabyle", displayName: "Taqbaylit", isRtl: false, fontFace: faceDefault },
  { name: "Tifinagh", displayName: "Tifinagh", isRtl: false, fontFace: "Calibri" },
  { name: "Arabic", displayName: "Arabic", isRtl: true, fontFace: "Calibri" },
  { name: "Arabic2", displayName: "Arabic2", isRtl: true, fontFace: "Calibri" },
  { name: "Hebrew", displayName: "Hebrew", isRtl: true, fontFace: "Calibri" },
  { name: "Syriac", displayName: "Syriac", isRtl: true, fontFace: "Calibri" },
  { name: "Aramaic", displayName: "Aramaic", isRtl: true, fontFace: "Calibri" },
  { name: "Phoenician", displayName: "Phoenician", isRtl: true, fontFace: "Calibri" },
  { name: "Musnad", displayName: "Musnad", isRtl: true, fontFace: "Calibri" },
  { name: "Cyrillic", displayName: "Cyrillic", isRtl: false, fontFace: faceDefault },
  { name: "Greek", displayName: "Greek", isRtl: false, fontFace: faceDefault }
];
  // Derived reactive variables (recomputed on each change)
  $: currentScriptObj = scripts.find(s => s.name === selectedScript) || scripts[0];
  $: pageCount = tales.length;
  
  let currentConfig = null;
  let rtl = false;
  let face = faceDefault;

  let dialogElement; // Controls the <dialog> element
  let transliterationsData = []; // Holds the generated table
  let allConfigs = {}; // Stores all loaded configurations

onMount(async () => {
  // 1. Restore user preferences from localStorage
  selectedScript = localStorage.getItem("SelectedScript") || "Kabyle";
  

  page = parseInt(localStorage.getItem("SelectedIndex")) || 0;
  kabyleTextSizeIncrement = parseInt(localStorage.getItem("KabyleTextSizeIncrement")) || 0;
  translationTextSizeIncrement = parseInt(localStorage.getItem("TranslationTextSizeIncrement")) || 0;

  try {
    // 2. Fetch the stories JSON
    const response = await fetch(`data/tamacahut_n_wuccen.json?${Date.now()}`);
    tales = await response.json();

    // Load the restored script configuration before displaying the story
    changeScript(selectedScript);
    
    // 3. Translate the script names in the dropdown
    scripts = scripts.map((s) => {
        if (s.name === "Kabyle") return s;
        try {
          const config = ConfigDeserializer.parse(s.name);
          const translatedName = (s.name === "Arabic2")? TransliterationService.transliterate("Taqbaylit2", config) : TransliterationService.transliterate("Taqbaylit", config);
          return { ...s, displayName: translatedName };
        } catch (e) {
          console.error(`Unable to transliterate the menu for ${s.name}`, e);
          return s;
        }
      });

  } catch (error) {
    console.error("Application initialization error", error);
  } finally {
    loading = false;
  }

      // Load all configurations in the background for the reference table
    try {
      for (const script of scripts.filter(s => s.name !== "Kabyle")) {
        allConfigs[script.name] = ConfigDeserializer.parse(script.name);
      }

      // Generate the table from all loaded configurations
      transliterationsData = TransliterationService.getTransliterations(allConfigs);
    } catch (e) {
      console.error("Error generating help table", e);
    }  


});

 // Functions to open and close the modal
  function openHelp() { dialogElement.showModal(); }
  function closeHelp() { dialogElement.close(); }


function handleScriptChange(e, resetPage = true) {
  changeScript(e.target.value, resetPage)
}

  function changeScript(_selectedScript, resetPage = true) {
    selectedScript = _selectedScript;
    localStorage.setItem("SelectedScript", selectedScript);
    if (resetPage) {
      localStorage.setItem("SelectedIndex", page.toString());
    }

    if (selectedScript === "Kabyle") {
      currentConfig = null;
      rtl = false;
      face = faceDefault;
    } else {
      try {
        currentConfig = ConfigDeserializer.parse(selectedScript);
        rtl = currentConfig.direction === "rtl";
        face = currentScriptObj.fontFace;
      } catch (err) {
        console.error(`Unable to load configuration for ${selectedScript}`, err);
        currentConfig = null;
        rtl = false;
        face = faceDefault;
      }
    }

    updateTale();
  }


  function updateTale() {
    if (tales.length === 0 || !tales[page]) return;
    
    const rawTale = tales[page];

    // Transliterate using the loaded configuration
    let processedKabyleTitle = rawTale.Kabyle.Title;
    let processedKabyleParagraphs = [...rawTale.Kabyle.Paragraphs];

    if (selectedScript !== "Kabyle" && currentConfig) {
      processedKabyleTitle = TransliterationService.transliterate(rawTale.Kabyle.Title, currentConfig);
      processedKabyleParagraphs = rawTale.Kabyle.Paragraphs.map(p => 
        TransliterationService.transliterate(p, currentConfig)
      );
    }

    tale = {
      Kabyle: {
        Title: processedKabyleTitle,
        Paragraphs: processedKabyleParagraphs
      },
      French: { ...rawTale.French }
    };
  }

  function previous() {
    if (page > 0) {
      page--;
      localStorage.setItem("SelectedIndex", page.toString());
      updateTale();
    }
  }

  function next() {
    if (page < pageCount - 1) {
      page++;
      localStorage.setItem("SelectedIndex", page.toString());
      updateTale();
    }
  }

  function changeSize(target, direction) {
    if (target === 'kabyle') {
      kabyleTextSizeIncrement = Math.max(0, Math.min(4, kabyleTextSizeIncrement + direction));
      localStorage.setItem("KabyleTextSizeIncrement", kabyleTextSizeIncrement.toString());
    } else {
      translationTextSizeIncrement = Math.max(0, Math.min(4, translationTextSizeIncrement + direction));
      localStorage.setItem("TranslationTextSizeIncrement", translationTextSizeIncrement.toString());
    }
  }
</script>

{#if loading || !tale}
  <div class="loader"><p><em>Loading...</em></p></div>
{:else}
  <main>
    <div class="pagination-header">
      <!-- Previous button (left arrow) -->
      <button on:click={previous} disabled={page <= 0} aria-label="Previous">
        <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-caret">
          <path d="M14 7l-5 5 5 5V7z" />
        </svg>
      </button>

      <span class="page-number">{page}</span>

      <!-- Next button (right arrow) -->
      <button on:click={next} disabled={page >= pageCount - 1} aria-label="Next">
        <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-caret">
          <path d="M10 17l5-5-5-5v10z" />
        </svg>
      </button>
    </div>


    <table class="table">
      <thead>
        <tr>
          <!-- Kabyle column: controls and text size -->
          <td width="50%">
            <select value={selectedScript} on:change={handleScriptChange}>
              {#each scripts as script}
                <option value={script.name}>{script.displayName}</option>
              {/each}
            </select>

            <!-- Vector help button -->
            <button on:click={openHelp} class="help-btn" aria-label="Transliteration help">
              <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn-help">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 16h-2v-2h2v2zm1.07-7.75l-.9.92C12.45 11.9 12 12.5 12 14h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H7c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.04-.42 1.99-1.07 2.75z"/>
              </svg>
            </button>

            <span class="size-controls">
              <button on:click={() => changeSize('kabyle', -1)} disabled={kabyleTextSizeIncrement <= 0} aria-label="Decrease size">
                <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn">
                  <path d="M19 13H5v-2h14v2z" />
                </svg>
              </button>
              <button on:click={() => changeSize('kabyle', 1)} disabled={kabyleTextSizeIncrement >= 4} aria-label="Increase size">
                <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn">
                  <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" />
                </svg>
              </button>
            </span>
          </td>

          <!-- Translation column: controls and text size -->
          <td width="50%">
            <select bind:value={selectedTranslation}>
              {#each translations as translation}
                <option value={translation}>{translation}</option>
              {/each}
            </select>
            <span class="size-controls">
              <button on:click={() => changeSize('translation', -1)} disabled={translationTextSizeIncrement <= 0} aria-label="Decrease size">
                <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn">
                  <path d="M19 13H5v-2h14v2z" />
                </svg>
              </button>
              <button on:click={() => changeSize('translation', 1)} disabled={translationTextSizeIncrement >= 4} aria-label="Increase size">
                <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn">
                  <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" />
                </svg>
              </button>
            </span>
          </td>
        </tr>


        <tr>
          <th width="50%">
            <h2 style="font-family: {face}; font-size: {1.2 + kabyleTextSizeIncrement * 0.2}rem;" dir={rtl ? "rtl" : "ltr"}>
              {tale.Kabyle.Title}
            </h2>
          </th>
          <th width="50%">
            <h2 style="font-size: {1.2 + translationTextSizeIncrement * 0.2}rem;">
              {tale.French.Title}
            </h2>
          </th>
        </tr>
      </thead>

      <tbody>
        {#if tale.Kabyle.Paragraphs.length === tale.French.Paragraphs.length}
          {#each tale.Kabyle.Paragraphs as kabylePara, i}
            <tr>
              <td>
                <div class="text-content" style="font-family: {face}; font-size: {1 + kabyleTextSizeIncrement * 0.15}rem;" dir={rtl ? "rtl" : "ltr"}>
                  {#each kabylePara.split('\n') as line}
                    {line}<br />
                  {/each}
                </div>
              </td>
              <td>
                <div class="text-content" style="font-size: {1 + translationTextSizeIncrement * 0.15}rem;">
                  {#each tale.French.Paragraphs[i].split('\n') as line}
                    {line}<br />
                  {/each}
                </div>
              </td>
            </tr>
          {/each}
        {:else}
          <tr>
            <td>
              <div class="text-content" style="font-family: {face}; font-size: {1 + kabyleTextSizeIncrement * 0.15}rem;" dir={rtl ? "rtl" : "ltr"}>
                {#each tale.Kabyle.Paragraphs as paragraph}
                  <p>
                    {#each paragraph.split('\n') as line}
                      {line}<br />
                    {/each}
                  </p>
                {/each}
              </div>
            </td>
            <td>
              <div class="text-content" style="font-size: {1 + translationTextSizeIncrement * 0.15}rem;">
                {#each tale.French.Paragraphs as paragraph}
                  <p>
                    {#each paragraph.split('\n') as line}
                      {line}<br />
                    {/each}
                  </p>
                {/each}
              </div>
            </td>
          </tr>
        {/if}
      </tbody>
    </table>
  </main>

    <!-- Help modal (native dialog) -->
  <dialog bind:this={dialogElement} class="modal">
    <div class="modal-header">
      <h3>Mapping</h3>
      <button on:click={closeHelp} class="close-btn">&times;</button>
    </div>
    
    <div class="modal-body">
      {#if transliterationsData.length > 0}
        <table class="help-table">
          <thead>
            <tr>              
              {#each transliterationsData[0] as column}
                <th><div class="rotated">{column}</div></th>
              {/each}
            </tr>
          </thead>
          <tbody>            
            {#each transliterationsData.slice(1) as row}
              <tr>
                {#each row as cell}
                  <td>{cell}</td>
                {/each}
              </tr>
            {/each}
          </tbody>
        </table>
      {:else}
        <p>Loading the help table...</p>
      {/if}
    </div>
  </dialog>
{/if}

<style>
  main {
    max-width: 1200px;
    margin: 0 auto;
    padding: 1rem;
    font-family: system-ui, sans-serif;
  }
  .loader {
    text-align: center;
    padding: 3rem;
    font-family: sans-serif;
  }
  .pagination-header {
    text-align: center;
    margin-bottom: 1.5rem;
  }
  .page-number {
    font-size: 1.5rem;
    font-weight: bold;
    margin: 0 1.5rem;
    user-select: none;
  }
  .table {
    width: 100%;
    border-collapse: collapse;
  }
  td, th {
    padding: 12px;
    vertical-align: top;
    border-bottom: 1px solid #ddd;
  }
  .size-controls {
    float: right;
  }
  .text-content {
    line-height: 1.6;
  }
  select, button {
    padding: 6px 12px;
    border-radius: 4px;
    border: 1px solid #ccc;
    background: white;
    cursor: pointer;
  }
  button:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }
    /* Keep SVG arrows consistent */
  .icon-caret {
    width: 24px;
    height: 24px;
    fill: currentColor; /* Match the button text color, including its disabled state */
    vertical-align: middle;
  }

  .pagination-header button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 6px; /* Set spacing around the arrow */
  }

  /* Plus and minus buttons */
  .icon-btn {
    width: 16px; /* Slightly smaller than the arrows for visual balance */
    height: 16px;
    fill: currentColor;
    vertical-align: middle;
  }

  .size-controls button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 6px;
    margin-left: 2px;
  }
  
  .table td, .table th {
    padding: 12px 24px; /* Add horizontal spacing */
  }



    /* Help button */
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
    border-color: #ff3e00; /* Optional Svelte-orange hover effect */
    background-color: #fffbf9;
  }

  /* Keep the help icon stroke subtle */
  .icon-btn-help {
    width: 18px;
    height: 18px;
    fill: #666; /* Use softened gray instead of black */
  }
  
  .help-btn:hover .icon-btn-help {
    fill: #ff3e00;
  }


  select {
    height: 32px;
    border: 1px solid #ccc;
    border-radius: 4px;
    padding: 0 8px;
    background: white;
  }

  /* Style de la Pop-up Modale */
  .modal {
    border: none;
    border-radius: 8px;
    padding: 0;
    max-width: 90%;
    max-height: 85vh;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
  }
  
  /* Effet d'arrière-plan semi-transparent quand la pop-up est ouverte */
  .modal::backdrop {
    background: rgba(0, 0, 0, 0.5);
    backdrop-filter: blur(2px);
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

  .close-btn {
    background: none;
    border: none;
    font-size: 1.5rem;
    font-weight: bold;
    cursor: pointer;
    padding: 0 5px;
  }

  .modal-body {
    padding: 1rem;
    overflow-auto: auto;
    max-height: 70vh;
  }

  /* Internal mapping table */
  .help-table {
    width: 100%;
    border-collapse: collapse;
    text-align: center;
    margin-top: 25px; /* Slightly reduced to match the slimmer title size */
  }


  .help-table th, .help-table td {
    border: 1px solid #ddd;
    padding: 8px;
    font-size: 1.1rem;
  }

  .help-table th {
    /* This is the trick: force a transparent background so text is never clipped */
    background-color: transparent !important; 
    height: 40px; 
    position: relative;
    vertical-align: bottom;
    padding: 0;
    overflow: visible;
  }

  /* Rotation adjustment without the bread-slice effect */
  .rotated {
    transform: rotate(-45deg);
    transform-origin: bottom left;
    position: absolute;
    bottom: 5px;
    left: 40%;
    width: 150px;
    text-align: left;
    font-weight: bold;
    font-size: 0.85rem; /* Changed from 1rem to 0.85rem to reduce the size */
    white-space: nowrap;
    overflow: visible;
    pointer-events: none;
    z-index: 999;
  }


</style>

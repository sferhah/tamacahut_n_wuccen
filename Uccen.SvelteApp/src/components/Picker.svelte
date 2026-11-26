<!--
  Dropdown for choosing a script or a translation.
  Uses a native <select>, or a custom menu (`custom`) where the native one renders poorly (Android).
-->
<script lang="ts">
  export let options: { value: string; label: string }[];
  export let value: string;
  export let label: string;
  export let custom = false;
  export let onchange: (value: string) => void;

  let open = false;
  let root: HTMLElement;
  let trigger: HTMLButtonElement;

  $: selectedLabel = options.find((option) => option.value === value)?.label ?? "";

  function select(nextValue: string): void {
    open = false;
    onchange(nextValue);
  }

  function handleWindowClick(event: MouseEvent): void {
    if (open && event.target instanceof Node && !root.contains(event.target)) {
      open = false;
    }
  }

  function handleWindowKeydown(event: KeyboardEvent): void {
    if (open && event.key === "Escape") {
      open = false;
      trigger.focus();
    }
  }
</script>

<svelte:window on:click={handleWindowClick} on:keydown={handleWindowKeydown} />

{#if custom}
  <div class="custom-picker" bind:this={root}>
    <button
      type="button"
      class="picker-trigger"
      aria-haspopup="listbox"
      aria-expanded={open}
      bind:this={trigger}
      on:click={() => (open = !open)}
    >
      {selectedLabel}
      <span class="picker-chevron" aria-hidden="true"></span>
    </button>
    {#if open}
      <div class="picker-menu" role="listbox" aria-label={label}>
        {#each options as option}
          <button
            type="button"
            class="picker-option"
            role="option"
            aria-selected={option.value === value}
            on:click={() => select(option.value)}
          >
            {option.label}
          </button>
        {/each}
      </div>
    {/if}
  </div>
{:else}
  <select {value} aria-label={label} on:change={(event) => onchange(event.currentTarget.value)}>
    {#each options as option}
      <option value={option.value}>{option.label}</option>
    {/each}
  </select>
{/if}

<style>
  .custom-picker {
    position: relative;
    display: inline-block;
    max-width: 100%;
    vertical-align: top;
  }

  .picker-trigger {
    display: inline-flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    max-width: 100%;
    min-height: 32px;
    padding: 5px 8px;
    font-size: 14px;
    line-height: 1.2;
    white-space: nowrap;
  }

  .picker-chevron {
    width: 7px;
    height: 7px;
    flex: 0 0 auto;
    border-right: 1.5px solid currentColor;
    border-bottom: 1.5px solid currentColor;
    transform: translateY(-2px) rotate(45deg);
  }

  .picker-menu {
    position: absolute;
    top: calc(100% + 4px);
    left: 0;
    z-index: 5;
    width: max-content;
    min-width: 100%;
    max-width: min(80vw, 280px);
    max-height: min(40vh, 320px);
    overflow-y: auto;
    padding: 4px;
    border: 1px solid #ccc;
    border-radius: 6px;
    background: white;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.18);
  }

  .picker-option {
    display: block;
    width: 100%;
    min-height: 34px;
    padding: 6px 8px;
    border: 0;
    border-radius: 3px;
    font-size: 14px;
    line-height: 1.2;
    text-align: left;
    white-space: normal;
  }

  .picker-option[aria-selected="true"] {
    background: #f0f0f0;
    font-weight: 600;
  }

  .picker-option:hover,
  .picker-option:focus-visible {
    background: #f4f4f4;
    outline: none;
  }
</style>

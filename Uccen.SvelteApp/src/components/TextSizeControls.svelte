<!-- The − / + buttons next to each column's picker. -->
<script lang="ts">
  import { MAX_TEXT_SIZE_INCREMENT, MIN_TEXT_SIZE_INCREMENT } from "../lib/preferences";

  type Props = {
    size: number;
    decreaseLabel: string;
    increaseLabel: string;
    onchange: (size: number) => void;
  };

  let { size, decreaseLabel, increaseLabel, onchange }: Props = $props();
</script>

<span class="size-controls">
  <button onclick={() => onchange(size - 1)} disabled={size <= MIN_TEXT_SIZE_INCREMENT} aria-label={decreaseLabel}>
    <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn">
      <path d="M19 13H5v-2h14v2z" />
    </svg>
  </button>
  <button onclick={() => onchange(size + 1)} disabled={size >= MAX_TEXT_SIZE_INCREMENT} aria-label={increaseLabel}>
    <svg xmlns="http://w3.org" viewBox="0 0 24 24" class="icon-btn">
      <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" />
    </svg>
  </button>
</span>

<style>
  .size-controls {
    float: right;
  }

  .size-controls button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 6px;
    margin-left: 2px;
  }

  .icon-btn {
    width: 16px; /* Slightly smaller than the arrows for visual balance */
    height: 16px;
    fill: currentColor;
    vertical-align: middle;
  }
</style>

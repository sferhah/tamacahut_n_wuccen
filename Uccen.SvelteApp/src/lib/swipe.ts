// Svelte action: calls `onswipe` when the user swipes horizontally on a touch screen.
// Usage: <main use:swipe={{ onswipe: (direction) => ... }}>

import type { Action } from "svelte/action";

export type SwipeDirection = "left" | "right";

type SwipeOptions = {
  onswipe: (direction: SwipeDirection) => void;
};

/** Minimum horizontal travel, in CSS pixels. */
const MIN_DISTANCE = 60;
/** The gesture must be clearly horizontal: |dx| at least this many times |dy|. */
const MIN_HORIZONTAL_RATIO = 1.5;
/** Slower drags are treated as reading/scrolling, not as a swipe. */
const MAX_DURATION_MS = 800;
/** Touches starting this close to the screen edge belong to the browser's own back/forward gesture. */
const EDGE_MARGIN = 24;
/** Swipes starting on these elements are left to them. */
const IGNORED_TARGETS = "input, select, textarea, .custom-picker";

export const swipe: Action<HTMLElement, SwipeOptions> = (node, initialOptions) => {
  let options = initialOptions;
  let start: { x: number; y: number; time: number } | null = null;

  function handleTouchStart(event: TouchEvent): void {
    const touch = event.touches[0];
    const isZoomedIn = (window.visualViewport?.scale ?? 1) > 1.01;
    const startsOnIgnoredTarget = event.target instanceof Element && event.target.closest(IGNORED_TARGETS);
    const startsAtScreenEdge = touch.clientX < EDGE_MARGIN || touch.clientX > window.innerWidth - EDGE_MARGIN;

    start = event.touches.length === 1 && !isZoomedIn && !startsOnIgnoredTarget && !startsAtScreenEdge
      ? { x: touch.clientX, y: touch.clientY, time: event.timeStamp }
      : null;
  }

  function handleTouchMove(event: TouchEvent): void {
    if (event.touches.length > 1) start = null; // pinch-zoom
  }

  function handleTouchEnd(event: TouchEvent): void {
    if (!start) return;

    const touch = event.changedTouches[0];
    const dx = touch.clientX - start.x;
    const dy = touch.clientY - start.y;
    const isSwipe =
      Math.abs(dx) >= MIN_DISTANCE &&
      Math.abs(dx) >= Math.abs(dy) * MIN_HORIZONTAL_RATIO &&
      event.timeStamp - start.time <= MAX_DURATION_MS &&
      !window.getSelection()?.toString(); // the user was selecting text

    start = null;
    if (isSwipe) options.onswipe(dx < 0 ? "left" : "right");
  }

  function handleTouchCancel(): void {
    start = null;
  }

  node.addEventListener("touchstart", handleTouchStart, { passive: true });
  node.addEventListener("touchmove", handleTouchMove, { passive: true });
  node.addEventListener("touchend", handleTouchEnd, { passive: true });
  node.addEventListener("touchcancel", handleTouchCancel, { passive: true });

  return {
    update(newOptions) {
      options = newOptions;
    },
    destroy() {
      node.removeEventListener("touchstart", handleTouchStart);
      node.removeEventListener("touchmove", handleTouchMove);
      node.removeEventListener("touchend", handleTouchEnd);
      node.removeEventListener("touchcancel", handleTouchCancel);
    }
  };
};

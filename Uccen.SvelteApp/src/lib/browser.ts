export function isAndroidDevice(): boolean {
  return /Android/i.test(navigator.userAgent);
}

export function isIOSDevice(): boolean {
  return /iPhone|iPad|iPod/i.test(navigator.userAgent) ||
    (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
}

/** True when a click on a modal <dialog> landed on its backdrop rather than its content. */
export function isBackdropClick(dialog: HTMLDialogElement, event: MouseEvent): boolean {
  const bounds = dialog.getBoundingClientRect();
  return event.clientX < bounds.left ||
    event.clientX > bounds.right ||
    event.clientY < bounds.top ||
    event.clientY > bounds.bottom;
}

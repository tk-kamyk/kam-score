import { computed, ref } from 'vue'

const DISMISSED_KEY = 'pwa-install-hint-dismissed'

const dismissed = ref(localStorage.getItem(DISMISSED_KEY) === '1')

const ua = typeof navigator !== 'undefined' ? navigator.userAgent : ''

// Treat the device as "mobile" when both signals agree: it has a coarse pointer
// AND the UA looks like a phone or tablet. The pointer query rules out desktop
// browsers with spoofed UAs; the UA check rules out touch laptops.
const isMobile =
  typeof window !== 'undefined' &&
  window.matchMedia('(pointer: coarse)').matches &&
  /Mobi|Android|iPhone|iPad|iPod/i.test(ua)

// iPadOS Safari masquerades as Mac unless "Request Mobile Website" is set, so
// we additionally accept Macintosh UAs with multi-touch as iPad.
const isIos =
  /iPad|iPhone|iPod/.test(ua) ||
  (/Macintosh/.test(ua) && typeof navigator !== 'undefined' && navigator.maxTouchPoints > 1)

// Safari, not an in-app browser like Chrome iOS, Firefox iOS, Edge iOS, Opera iOS.
const isSafari = !/CriOS|FxiOS|EdgiOS|OPiOS/i.test(ua)

const isStandalone =
  (typeof navigator !== 'undefined' &&
    (navigator as Navigator & { standalone?: boolean }).standalone === true) ||
  (typeof window !== 'undefined' && window.matchMedia('(display-mode: standalone)').matches)

export function usePwaInstall() {
  const showIosHint = computed(
    () => isMobile && isIos && isSafari && !isStandalone && !dismissed.value,
  )

  function dismiss() {
    localStorage.setItem(DISMISSED_KEY, '1')
    dismissed.value = true
  }

  return { showIosHint, dismiss }
}

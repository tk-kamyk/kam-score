<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'

const props = defineProps<{
  loading: boolean
  color?: string
  hintDelayMs?: number
  ariaLabel?: string
}>()

const showHint = ref(false)
let timer: ReturnType<typeof setTimeout> | null = null

function clearTimer() {
  if (timer) {
    clearTimeout(timer)
    timer = null
  }
}

watch(
  () => props.loading,
  (active) => {
    clearTimer()
    showHint.value = false
    if (active) {
      timer = setTimeout(() => {
        showHint.value = true
      }, props.hintDelayMs ?? 10000)
    }
  },
  { immediate: true },
)

onUnmounted(clearTimer)
</script>

<template>
  <div v-if="loading">
    <v-progress-linear indeterminate :color="color ?? 'primary'" :aria-label="ariaLabel" />
    <div
      v-if="showHint"
      class="text-caption text-medium-emphasis mt-1 text-center"
      role="status"
      aria-live="polite"
    >
      This may take a moment. Please wait.
    </div>
  </div>
</template>

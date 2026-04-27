<script setup lang="ts">
import { computed } from 'vue'
import { usePwaInstall } from '@/composables/usePwaInstall'

const { showIosHint, dismiss } = usePwaInstall()

const visible = computed({
  get: () => showIosHint.value,
  set: (value) => {
    if (!value) dismiss()
  },
})
</script>

<template>
  <v-snackbar
    v-model="visible"
    :timeout="-1"
    location="bottom"
    color="surface-light"
    multi-line
    role="status"
    aria-live="polite"
    class="ks-pwa-hint"
  >
    <div class="d-flex align-center ga-3">
      <v-icon icon="mdi-export-variant" size="28" color="primary" />
      <div class="text-body-2">
        Install <strong>Kam² Score</strong> on your home screen — tap the
        <strong>Share</strong> button, then <strong>Add to Home Screen</strong>.
      </div>
    </div>

    <template #actions>
      <v-btn icon="mdi-close" variant="text" size="small" aria-label="Dismiss" @click="dismiss" />
    </template>
  </v-snackbar>
</template>

<style scoped>
.ks-pwa-hint :deep(.v-snackbar__wrapper) {
  border: 1px solid var(--ks-border-strong);
}
</style>

<script setup lang="ts">
import { useSlots } from 'vue'
import { formatPhaseFormat } from '@/structure/types'
import type { PhaseDto } from '@/structure/types'

defineProps<{
  phase: PhaseDto
  expanded: boolean
}>()

const emit = defineEmits<{
  toggle: []
}>()

const slots = useSlots()
</script>

<template>
  <v-card class="phase-card">
    <v-card-title class="d-flex align-center justify-space-between phase-header" @click="emit('toggle')">
      <div class="d-flex align-center flex-wrap ga-1">
        <v-icon
          :icon="expanded ? 'mdi-chevron-down' : 'mdi-chevron-right'"
          size="small"
          class="mr-1"
        />
        <span class="text-title-medium text-sm-headline-small">{{ phase.name }}</span>
        <v-chip size="small" color="primary" variant="tonal" class="ml-4" prepend-icon="mdi-sitemap">
          {{ formatPhaseFormat(phase.format) }}
        </v-chip>
        <slot name="chips" />
      </div>
      <slot name="header-actions" />
    </v-card-title>

    <template v-if="expanded">
      <slot />
      <v-card-actions v-if="slots.actions" class="justify-end pa-4">
        <slot name="actions" />
      </v-card-actions>
    </template>
  </v-card>
</template>

<style scoped>
.phase-card {
  border: 1px solid var(--ks-border);
}

.phase-header {
  cursor: pointer;
}
</style>

<script setup lang="ts">
import { computed, useSlots } from 'vue'
import { formatPhaseFormat } from '@/structure/types'
import type { PhaseDto } from '@/structure/types'

const props = defineProps<{
  phase: PhaseDto
  expanded: boolean
}>()

const emit = defineEmits<{
  toggle: []
}>()

const slots = useSlots()

const statusChip = computed(() => {
  if (props.phase.status === 'Scheduled') return { color: 'info', icon: 'mdi-calendar-clock', label: 'Scheduled' }
  if (props.phase.status === 'InProgress') return { color: 'warning', icon: 'mdi-play-circle-outline', label: 'In Progress' }
  if (props.phase.status === 'Completed') return { color: 'success', icon: 'mdi-check-circle-outline', label: 'Completed' }
  return null
})
</script>

<template>
  <v-card class="phase-card">
    <v-card-title class="d-flex align-center justify-space-between phase-header" role="button" tabindex="0" :aria-expanded="expanded" @click="emit('toggle')" @keydown.enter.space.prevent="emit('toggle')">
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
        <v-chip v-if="phase.startTime" size="small" color="warning" variant="tonal" prepend-icon="mdi-calendar-clock">
          {{ phase.startTime }}
        </v-chip>
        <v-chip v-if="statusChip" size="small" :color="statusChip.color" variant="tonal" :prepend-icon="statusChip.icon">
          {{ statusChip.label }}
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

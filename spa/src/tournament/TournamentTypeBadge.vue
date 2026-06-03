<script setup lang="ts">
import { computed } from 'vue'
import type { TournamentType } from '@/tournament/types'

const props = defineProps<{ type?: TournamentType }>()

const config = computed(() => {
  switch (props.type) {
    case 'Private':
      return { color: 'warning', icon: 'mdi-lock', label: 'Private' }
    case 'Template':
      return { color: 'info', icon: 'mdi-content-copy', label: 'Template' }
    default:
      return { color: 'success', icon: 'mdi-earth', label: 'Public' }
  }
})
</script>

<template>
  <v-chip size="small" variant="tonal" :color="config.color" :prepend-icon="config.icon">
    {{ config.label }}
  </v-chip>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { stationColor } from '@/volunteer/stations'

// Renders one assigned volunteer as a chip. Availability drives the chip colour/variant and the
// trailing warning icon (unchanged). The station shows as a leading filled colour dot AND a
// visible 1-based number, so it is not conveyed by colour alone (WCAG 1.4.1).
const props = defineProps<{
  name: string
  available: boolean
  station?: number | null
}>()

defineEmits<{ close: [] }>()

const color = computed(() => stationColor(props.station))
const stationLabel = computed(() => (props.station == null ? null : props.station + 1))
</script>

<template>
  <v-chip
    size="small"
    :color="available ? 'primary' : 'warning'"
    :variant="available ? 'tonal' : 'outlined'"
    closable
    :close-label="`Remove ${name}`"
    @click:close="$emit('close')"
  >
    <span v-if="stationLabel" class="sr-only">Station {{ stationLabel }}: </span>
    <v-icon v-if="color" start icon="mdi-circle" :color="color" size="small" aria-hidden="true" />
    {{ name }}
    <v-icon v-if="!available" end icon="mdi-alert" size="x-small" aria-hidden="true" />
  </v-chip>
</template>

<style scoped>
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>

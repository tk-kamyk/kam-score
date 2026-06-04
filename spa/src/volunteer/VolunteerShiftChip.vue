<script setup lang="ts">
// Renders one assigned volunteer as a chip. Availability drives the chip colour/variant and the
// trailing warning icon; the station is conveyed by the surrounding group header.
defineProps<{
  name: string
  available: boolean
}>()

defineEmits<{ close: [] }>()
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
    {{ name }}
    <v-icon v-if="!available" end icon="mdi-alert" size="x-small" aria-hidden="true" />
  </v-chip>
</template>

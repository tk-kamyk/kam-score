<script setup lang="ts">
const enabled = defineModel<boolean>('enabled', { required: true })
const bestOfSets = defineModel<number | undefined>('bestOfSets')
const pointsPerSetText = defineModel<string>('pointsPerSetText', { required: true })
</script>

<template>
  <v-switch
    v-model="enabled"
    label="Custom game conditions"
    color="primary"
    density="comfortable"
    hide-details
    class="mb-4"
  />
  <template v-if="enabled">
    <v-select
      v-model="bestOfSets"
      :items="[1, 3, 5]"
      label="Best of Sets"
    />
    <v-text-field
      v-model="pointsPerSetText"
      label="Points per Set (comma-separated)"
      placeholder="25, 25, 15"
    />
  </template>
</template>

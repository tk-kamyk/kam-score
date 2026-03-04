<script setup lang="ts">
defineProps<{
  title: string
}>()
</script>

<template>
  <div class="d-flex justify-space-between align-center mb-4">
    <h3 class="section-title text-title-small text-md-title-medium">{{ title }}</h3>
    <slot />
  </div>
</template>

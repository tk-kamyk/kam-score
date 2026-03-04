<script setup lang="ts">
import TournamentOverview from '@/tournament/TournamentOverview.vue'

const props = defineProps<{ id: string }>()
</script>

<template>
  <TournamentOverview :id="props.id" />
</template>

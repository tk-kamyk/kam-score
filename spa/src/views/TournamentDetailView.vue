<script setup lang="ts">
import TournamentDetail from '@/tournament/TournamentDetail.vue'

const props = defineProps<{ id: string }>()
</script>

<template>
  <TournamentDetail :id="props.id" />
</template>

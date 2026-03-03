<script setup lang="ts">
import type { GameDto } from '@/game/types'

defineProps<{
  game: GameDto
}>()

defineEmits<{
  'enter-result': []
}>()

function formatSets(game: GameDto): string {
  return game.sets?.map(s => `${s.homePoints}–${s.awayPoints}`).join(' / ') || ''
}
</script>

<template>
  <template v-if="game.status === 'Completed' && game.homeScore != null">
    <v-chip size="small" color="success" variant="tonal">
      <template v-if="!game.sets?.length || (game.sets?.length ?? 0) > 1">
        {{ game.homeScore }}–{{ game.awayScore }}
      </template>
      <template v-else>
        {{ game.sets?.[0]?.homePoints }}–{{ game.sets?.[0]?.awayPoints }}
      </template>
    </v-chip>
    <div v-if="(game.sets?.length ?? 0) > 1" class="text-body-small text-medium-emphasis mt-1">
      {{ formatSets(game) }}
    </div>
  </template>
  <v-btn v-else size="small" variant="tonal" color="primary" @click="$emit('enter-result')" prepend-icon="mdi-scoreboard">Enter result</v-btn>
</template>

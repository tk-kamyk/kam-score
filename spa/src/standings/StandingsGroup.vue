<script setup lang="ts">
import type { StandingDto } from '@/standings/types'

defineProps<{
  standings: StandingDto[]
  phaseFormat: string
}>()

const isRoundRobin = (format: string) => format === 'RoundRobin'
</script>

<template>
  <v-card class="data-table-card">
    <v-table density="compact" class="styled-table">
      <thead>
        <tr>
          <th class="text-center">#</th>
          <th>Team</th>
          <th v-if="isRoundRobin(phaseFormat)" class="text-center" >Points</th>
          <th class="text-center">Games</th>
          <th class="text-center">Wins</th>
          <th v-if="isRoundRobin(phaseFormat)" class="text-center">Draws</th>
          <th class="text-center">Losses</th>
          <template v-if="isRoundRobin(phaseFormat)">
            <th class="text-center">Set Diff</th>
            <th class="text-center">Point Diff</th>
          </template>
        </tr>
      </thead>
      <tbody>
        <tr v-for="standing in standings" :key="standing.teamId">
          <td class="text-center">{{ standing.position }}</td>
          <td>{{ standing.teamName ?? standing.teamId }}</td>
          <td v-if="isRoundRobin(phaseFormat)"class="text-center">{{ standing.points }}</td>
          <td class="text-center">{{ standing.gamesPlayed }}</td>
          <td class="text-center">{{ standing.wins }}</td>
          <td v-if="isRoundRobin(phaseFormat)" class="text-center">{{ standing.draws }}</td>
          <td class="text-center">{{ standing.losses }}</td>
          <template v-if="isRoundRobin(phaseFormat)">
            <td class="text-center">{{ standing.setDifference != null && standing.setDifference > 0 ? '+' : '' }}{{ standing.setDifference }}</td>
            <td class="text-center">{{ standing.pointDifference != null && standing.pointDifference > 0 ? '+' : '' }}{{ standing.pointDifference }}</td>
          </template>
        </tr>
      </tbody>
    </v-table>
  </v-card>
</template>

<style scoped>
.data-table-card {
  border: 1px solid var(--ks-border);
}

.styled-table thead tr {
  background-color: rgb(var(--v-theme-surface-bright));
}

.styled-table tbody tr:hover {
  background-color: var(--ks-border-subtle);
}
</style>

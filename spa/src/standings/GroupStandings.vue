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
          <th class="text-center">GP</th>
          <th class="text-center">W</th>
          <template v-if="isRoundRobin(phaseFormat)">
            <th class="text-center">D</th>
          </template>
          <th class="text-center">L</th>
          <template v-if="isRoundRobin(phaseFormat)">
            <th class="text-center">Pts</th>
            <th class="text-center">S+</th>
            <th class="text-center">S-</th>
            <th class="text-center">S±</th>
          </template>
        </tr>
      </thead>
      <tbody>
        <tr v-for="standing in standings" :key="standing.teamId">
          <td class="text-center">{{ standing.position }}</td>
          <td>{{ standing.teamName ?? standing.teamId }}</td>
          <td class="text-center">{{ standing.gamesPlayed }}</td>
          <td class="text-center">{{ standing.wins }}</td>
          <template v-if="isRoundRobin(phaseFormat)">
            <td class="text-center">{{ standing.draws }}</td>
          </template>
          <td class="text-center">{{ standing.losses }}</td>
          <template v-if="isRoundRobin(phaseFormat)">
            <td class="text-center font-weight-bold">{{ standing.points }}</td>
            <td class="text-center">{{ standing.setsWon }}</td>
            <td class="text-center">{{ standing.setsLost }}</td>
            <td class="text-center">{{ standing.setDifference != null && standing.setDifference > 0 ? '+' : '' }}{{ standing.setDifference }}</td>
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

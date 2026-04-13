<script setup lang="ts">
import { computed } from 'vue'
import type { StandingDto } from '@/standings/types'

const props = defineProps<{
  standings: StandingDto[]
  phaseFormat: string
  groupWinners?: number
  totalTeamsProceeding?: number
  numberOfGroups: number
}>()

const isRoundRobin = (format: string) => format === 'RoundRobin'

const progressionThresholds = computed(() => {
  const gw = props.groupWinners
  const total = props.totalTeamsProceeding
  const groups = props.numberOfGroups

  if ((!gw && !total) || groups <= 0) return { directMax: 0, candidateMax: 0 }

  const directMax = gw ?? 0
  const directCount = directMax * groups
  const wildcardSlots = total ? total - directCount : 0

  if (wildcardSlots <= 0) return { directMax, candidateMax: directMax }

  const candidateDepth = Math.ceil(wildcardSlots / groups)
  return { directMax, candidateMax: directMax + candidateDepth }
})

function getRowClass(index: number): string {
  const row = index + 1
  const { directMax, candidateMax } = progressionThresholds.value
  if (directMax > 0 && row <= directMax) return 'row-direct-qualifier'
  if (candidateMax > 0 && row <= candidateMax) return 'row-candidate'
  return ''
}
</script>

<template>
  <v-card class="data-table-card">
    <v-table density="compact" class="styled-table">
      <thead>
        <tr>
          <th scope="col" class="text-center">#</th>
          <th scope="col">Team</th>
          <th v-if="isRoundRobin(phaseFormat)" scope="col" class="text-center">Points</th>
          <th scope="col" class="text-center">Games</th>
          <th scope="col" class="text-center">Wins</th>
          <th v-if="isRoundRobin(phaseFormat)" scope="col" class="text-center">Draws</th>
          <th scope="col" class="text-center">Losses</th>
          <template v-if="isRoundRobin(phaseFormat)">
            <th scope="col" class="text-center">Set Diff</th>
            <th scope="col" class="text-center">Point Diff</th>
          </template>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="(standing, index) in standings"
          :key="standing.teamId"
          :class="getRowClass(index)"
        >
          <td class="text-center">{{ standing.position }}</td>
          <td>{{ standing.teamName ?? standing.teamId }}</td>
          <td v-if="isRoundRobin(phaseFormat)" class="text-center">{{ standing.points }}</td>
          <td class="text-center">{{ standing.gamesPlayed }}</td>
          <td class="text-center">{{ standing.wins }}</td>
          <td v-if="isRoundRobin(phaseFormat)" class="text-center">{{ standing.draws }}</td>
          <td class="text-center">{{ standing.losses }}</td>
          <template v-if="isRoundRobin(phaseFormat)">
            <td class="text-center">
              {{ standing.setDifference != null && standing.setDifference > 0 ? '+' : ''
              }}{{ standing.setDifference }}
            </td>
            <td class="text-center">
              {{ standing.pointDifference != null && standing.pointDifference > 0 ? '+' : ''
              }}{{ standing.pointDifference }}
            </td>
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

.styled-table tbody tr.row-direct-qualifier {
  background-color: rgba(var(--v-theme-success), 0.12);
}

.styled-table tbody tr.row-candidate {
  background-color: rgba(var(--v-theme-warning), 0.12);
}

.styled-table tbody tr.row-direct-qualifier:hover {
  background-color: rgba(var(--v-theme-success), 0.2);
}

.styled-table tbody tr.row-candidate:hover {
  background-color: rgba(var(--v-theme-warning), 0.2);
}
</style>

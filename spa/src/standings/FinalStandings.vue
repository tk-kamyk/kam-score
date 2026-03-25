<script setup lang="ts">
import { computed } from 'vue'
import type { FinalStandingDto } from '@/standings/types'
import SectionHeader from '@/components/SectionHeader.vue'

const props = defineProps<{
  data: FinalStandingDto[]
  loading: boolean
}>()

const levels = computed(() => {
  if (!props.data.length) return []

  const levelNames = [...new Set(props.data.map(s => s.levelName).filter(Boolean))]

  if (levelNames.length === 0) {
    return [{ name: null, standings: props.data }]
  }

  return levelNames.map(name => ({
    name,
    standings: props.data.filter(s => s.levelName === name),
  }))
})

const hasData = computed(() => props.data.length > 0)
</script>

<template>
  <div v-if="hasData || loading" class="mt-8">
    <SectionHeader title="Final Standings" />

    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4" />

    <div v-if="hasData" class="d-flex flex-column flex-md-row flex-wrap ga-8">
      <div v-for="level in levels" :key="level.name ?? 'flat'" class="level-col">
        <h4 v-if="level.name" class="text-title-small mb-2 mt-0">{{ level.name }}</h4>

        <v-card class="data-table-card">
          <v-table density="compact" class="styled-table">
            <thead>
              <tr>
                <th scope="col" class="text-center position-col">#</th>
                <th scope="col">Team</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="standing in level.standings" :key="standing.teamId">
                <td class="text-center position-col">{{ standing.position }}</td>
                <td>{{ standing.teamName }}</td>
              </tr>
            </tbody>
          </v-table>
        </v-card>
      </div>
    </div>
  </div>
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

.level-col {
  flex: 1 1 0;
  min-width: 40%;
}

.position-col {
  width: 48px;
}
</style>

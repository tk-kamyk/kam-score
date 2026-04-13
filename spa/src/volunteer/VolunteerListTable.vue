<script setup lang="ts">
import type { VolunteerDto } from '@/volunteer/types'
import { useTeamStore } from '@/team/store'

defineProps<{
  volunteers: VolunteerDto[]
  loading: boolean
}>()

const emit = defineEmits<{
  edit: [volunteer: VolunteerDto]
  delete: [volunteer: VolunteerDto]
}>()

const teamStore = useTeamStore()

function teamName(teamId?: string | null): string {
  if (!teamId) return ''
  return teamStore.teams.find((t) => t.id === teamId)?.name ?? ''
}
</script>

<template>
  <div>
    <v-progress-linear v-if="loading" indeterminate color="primary" class="mb-4" />

    <v-card v-if="volunteers.length > 0" class="data-table-card">
      <v-table density="comfortable" class="styled-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Contact</th>
            <th>Team</th>
            <th class="text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="volunteer in volunteers" :key="volunteer.id">
            <td>{{ volunteer.name }}</td>
            <td class="text-medium-emphasis">{{ volunteer.contact || '—' }}</td>
            <td class="text-medium-emphasis">{{ teamName(volunteer.teamId) }}</td>
            <td class="text-right">
              <v-btn
                icon="mdi-pencil"
                variant="text"
                size="small"
                :aria-label="'Edit volunteer ' + volunteer.name"
                @click="emit('edit', volunteer)"
              />
              <v-btn
                icon="mdi-delete"
                variant="text"
                size="small"
                color="error"
                :aria-label="'Delete volunteer ' + volunteer.name"
                @click="emit('delete', volunteer)"
              />
            </td>
          </tr>
        </tbody>
      </v-table>
    </v-card>

    <v-alert v-else-if="!loading" type="info" variant="tonal" class="mt-4 mb-4">
      No volunteers yet.
    </v-alert>
  </div>
</template>

<style scoped>
.data-table-card {
  border: 1px solid var(--ks-border);
}

.styled-table thead tr {
  background-color: rgb(var(--v-theme-surface-bright));
}
</style>

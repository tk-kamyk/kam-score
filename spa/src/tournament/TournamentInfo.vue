<script setup lang="ts">
import { ref, watch } from 'vue'
import type { TournamentDto } from '@/tournament/types'

const props = defineProps<{
  tournament: TournamentDto
  isOwner: boolean
}>()

const emit = defineEmits<{
  updated: [dto: TournamentDto]
  deleted: []
}>()

const showEditDialog = ref(false)
const showDeleteDialog = ref(false)
const editForm = ref<TournamentDto>({ name: '', discipline: 'Volleyball' })
const editCustomConditions = ref(false)
const editPointsPerSetText = ref('')

const disciplines = ['Volleyball', 'BeachVolleyball']

watch(editCustomConditions, (on) => {
  if (on && !editForm.value.gameConditions) {
    editForm.value.gameConditions = { bestOfSets: undefined, pointsPerSet: undefined }
  }
})

function openEdit() {
  editForm.value = { ...props.tournament }
  editCustomConditions.value = !!props.tournament.gameConditions
  editPointsPerSetText.value = props.tournament.gameConditions?.pointsPerSet?.join(', ') ?? ''
  showEditDialog.value = true
}

function handleUpdate() {
  const dto = { ...editForm.value }
  if (editCustomConditions.value) {
    const points = editPointsPerSetText.value
      .split(',')
      .map(s => parseInt(s.trim()))
      .filter(n => !isNaN(n))
    dto.gameConditions = {
      bestOfSets: dto.gameConditions?.bestOfSets,
      pointsPerSet: points.length > 0 ? points : undefined,
    }
  } else {
    dto.gameConditions = undefined
  }
  emit('updated', dto)
  showEditDialog.value = false
}

function handleDelete() {
  emit('deleted')
  showDeleteDialog.value = false
}
</script>

<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-6">
      <h3 class="section-title text-title-medium text-md-title-large">Details</h3>
      <div v-if="isOwner">
        <v-btn icon="mdi-pencil" variant="text" size="small" @click="openEdit" />
        <v-btn icon="mdi-delete" variant="text" size="small" color="error" @click="showDeleteDialog = true" />
      </div>
    </div>

    <v-card class="data-table-card">
      <v-table density="comfortable" class="styled-table">
        <thead>
          <tr>
            <th>Property</th>
            <th>Value</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td class="font-weight-bold">Discipline</td>
            <td>{{ tournament.discipline }}</td>
          </tr>
          <tr v-if="tournament.startTime">
            <td class="font-weight-bold">Start Time</td>
            <td>{{ new Date(tournament.startTime).toLocaleString() }}</td>
          </tr>
          <tr v-if="tournament.gameLength">
            <td class="font-weight-bold">Game Length</td>
            <td>{{ tournament.gameLength }} minutes</td>
          </tr>
          <tr v-if="tournament.gameConditions?.bestOfSets">
            <td class="font-weight-bold">Best of Sets</td>
            <td>{{ tournament.gameConditions.bestOfSets }}</td>
          </tr>
          <tr v-if="tournament.gameConditions?.pointsPerSet">
            <td class="font-weight-bold">Points per Set</td>
            <td>{{ tournament.gameConditions.pointsPerSet.join(', ') }}</td>
          </tr>
          <tr v-if="tournament.tournamentCode">
            <td class="font-weight-bold">Tournament Code</td>
            <td>
              <v-chip color="secondary" size="small" variant="tonal" class="font-weight-bold code-chip">
                {{ tournament.tournamentCode }}
              </v-chip>
            </td>
          </tr>
        </tbody>
      </v-table>
    </v-card>
  </div>

  <!-- Edit Dialog -->
  <v-dialog v-model="showEditDialog" max-width="500">
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">Edit Tournament</v-card-title>
      <v-card-text>
        <v-text-field
          v-model="editForm.name"
          label="Name"
        />
        <v-select
          v-model="editForm.discipline"
          :items="disciplines"
          label="Discipline"
        />
        <v-text-field
          v-model="editForm.startTime"
          label="Start Time"
          placeholder="10:00"
        />
        <v-text-field
          v-model.number="editForm.gameLength"
          label="Game Length (minutes)"
          type="number"
        />
        <v-switch
          v-model="editCustomConditions"
          label="Custom game conditions"
          color="primary"
          density="comfortable"
          hide-details
          class="mb-4"
        />
        <template v-if="editCustomConditions">
          <v-select
            v-model="editForm.gameConditions!.bestOfSets"
            :items="[1, 3, 5]"
            label="Best of Sets"
          />
          <v-text-field
            v-model="editPointsPerSetText"
            label="Points per Set (comma-separated)"
            placeholder="25, 25, 15"
          />
        </template>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showEditDialog = false">Cancel</v-btn>
        <v-btn color="primary" variant="elevated" :disabled="!editForm.name" @click="handleUpdate">
          Save
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <!-- Delete Confirmation -->
  <v-dialog v-model="showDeleteDialog" max-width="400">
    <v-card class="pa-2">
      <v-card-title class="text-uppercase dialog-title">Delete Tournament</v-card-title>
      <v-card-text>
        Are you sure you want to delete "{{ tournament.name }}"? This action cannot be undone.
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
        <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
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

<script setup lang="ts">
import { ref } from 'vue'
import SectionHeader from '@/components/SectionHeader.vue'
import GameConditionsForm from '@/tournament/GameConditionsForm.vue'
import { buildGameConditions, formatPointsPerSet } from '@/tournament/gameConditionsUtils'
import { formatDate } from '@/tournament/dateUtils'
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
const editBestOfSets = ref<number | undefined>()
const editPointsPerSetText = ref('')

const disciplines = ['Volleyball', 'BeachVolleyball']

function openEdit() {
  editForm.value = {
    ...props.tournament,
    startTime: props.tournament.startTime?.split('T')[0],
  }
  editCustomConditions.value = !!props.tournament.gameConditions
  editBestOfSets.value = props.tournament.gameConditions?.bestOfSets
  editPointsPerSetText.value = formatPointsPerSet(props.tournament.gameConditions?.pointsPerSet)
  showEditDialog.value = true
}

function handleUpdate() {
  const dto: TournamentDto = {
    ...editForm.value,
    gameConditions: buildGameConditions(editCustomConditions.value, editBestOfSets.value, editPointsPerSetText.value),
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
    <SectionHeader title="Details">
      <div v-if="isOwner">
        <v-btn icon="mdi-pencil" variant="text" size="small" aria-label="Edit tournament" @click="openEdit" />
        <v-btn icon="mdi-delete" variant="text" size="small" color="error" aria-label="Delete tournament" @click="showDeleteDialog = true" />
      </div>
    </SectionHeader>

    <v-card class="data-table-card">
      <v-table density="comfortable" class="styled-table">
        <thead>
          <tr>
            <th scope="col">Property</th>
            <th scope="col">Value</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <th scope="row" class="font-weight-bold">Discipline</th>
            <td>{{ tournament.discipline }}</td>
          </tr>
          <tr v-if="tournament.startTime">
            <th scope="row" class="font-weight-bold">Date</th>
            <td>{{ formatDate(new Date(tournament.startTime)) }}</td>
          </tr>
          <tr v-if="tournament.gameLength">
            <th scope="row" class="font-weight-bold">Game Length</th>
            <td>{{ tournament.gameLength }} minutes</td>
          </tr>
          <tr v-if="tournament.gameConditions?.bestOfSets">
            <th scope="row" class="font-weight-bold">Best of Sets</th>
            <td>{{ tournament.gameConditions.bestOfSets }}</td>
          </tr>
          <tr v-if="tournament.gameConditions?.pointsPerSet?.length">
            <th scope="row" class="font-weight-bold">Points per Set</th>
            <td>{{ tournament.gameConditions.pointsPerSet.join(', ') }}</td>
          </tr>
          <tr v-if="tournament.tournamentCode">
            <th scope="row" class="font-weight-bold">Tournament Code</th>
            <td>
              <v-chip color="secondary" size="small" variant="tonal" prepend-icon="mdi-key" class="font-weight-bold code-chip">
                {{ tournament.tournamentCode }}
              </v-chip>
            </td>
          </tr>
        </tbody>
      </v-table>
    </v-card>
  </div>

  <!-- Edit Dialog -->
  <v-dialog v-model="showEditDialog" max-width="500" aria-labelledby="edit-tournament-dialog-title">
    <v-card class="pa-2">
      <v-card-title id="edit-tournament-dialog-title" class="text-uppercase dialog-title">Edit Tournament</v-card-title>
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
          label="Date"
          type="date"
        />
        <v-text-field
          v-model.number="editForm.gameLength"
          label="Game Length (minutes)"
          type="number"
        />
        <GameConditionsForm
          v-model:enabled="editCustomConditions"
          v-model:best-of-sets="editBestOfSets"
          v-model:points-per-set-text="editPointsPerSetText"
        />
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
  <v-dialog v-model="showDeleteDialog" max-width="400" aria-labelledby="delete-tournament-dialog-title">
    <v-card class="pa-2">
      <v-card-title id="delete-tournament-dialog-title" class="text-uppercase dialog-title">Delete Tournament</v-card-title>
      <v-card-text id="delete-warning-text">
        Are you sure you want to delete "{{ tournament.name }}"? This action cannot be undone.
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
        <v-btn color="error" variant="elevated" aria-describedby="delete-warning-text" @click="handleDelete">Delete</v-btn>
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

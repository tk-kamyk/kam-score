<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import type { TournamentDto } from '@/tournament/types'

const props = defineProps<{ id: string }>()

const router = useRouter()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()

const showEditDialog = ref(false)
const showDeleteDialog = ref(false)
const editForm = ref<TournamentDto>({ name: '', discipline: 'Volleyball' })
const editCustomConditions = ref(false)
const editPointsPerSetText = ref('')

const tournament = computed(() => tournamentStore.currentTournament)
const isOwner = computed(() =>
  auth.isAuthenticated && tournament.value?.ownerId === auth.username
)

const disciplines = ['Volleyball', 'BeachVolleyball']

onMounted(() => {
  tournamentStore.fetchTournament(props.id)
})

watch(editCustomConditions, (on) => {
  if (on && !editForm.value.gameConditions) {
    editForm.value.gameConditions = { winningSets: undefined, pointsPerSet: undefined }
  }
})

function openEdit() {
  if (tournament.value) {
    editForm.value = { ...tournament.value }
    editCustomConditions.value = !!tournament.value.gameConditions
    editPointsPerSetText.value = tournament.value.gameConditions?.pointsPerSet?.join(', ') ?? ''
  }
  showEditDialog.value = true
}

async function handleUpdate() {
  try {
    const dto = { ...editForm.value }
    if (editCustomConditions.value) {
      const points = editPointsPerSetText.value
        .split(',')
        .map(s => parseInt(s.trim()))
        .filter(n => !isNaN(n))
      dto.gameConditions = {
        winningSets: dto.gameConditions?.winningSets,
        pointsPerSet: points.length > 0 ? points : undefined,
      }
    } else {
      dto.gameConditions = undefined
    }
    await tournamentStore.updateTournament(props.id, dto)
    showEditDialog.value = false
    showSuccess('Tournament updated')
  } catch {
    showError('Failed to update tournament')
  }
}

async function handleDelete() {
  try {
    await tournamentStore.deleteTournament(props.id)
    showDeleteDialog.value = false
    showSuccess('Tournament deleted')
    router.push({ name: 'home' })
  } catch {
    showError('Failed to delete tournament')
  }
}
</script>

<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-4" @click="router.push({ name: 'home' })">
      Back to Tournaments
    </v-btn>

    <v-progress-linear v-if="tournamentStore.loading" indeterminate color="primary" />

    <v-card v-if="tournament" class="mb-4">
      <v-card-title class="d-flex justify-space-between align-center">
        <span>{{ tournament.name }}</span>
        <div v-if="isOwner">
          <v-btn icon="mdi-pencil" variant="text" size="small" @click="openEdit" />
          <v-btn icon="mdi-delete" variant="text" size="small" color="error" @click="showDeleteDialog = true" />
        </div>
      </v-card-title>
      <v-card-text>
        <v-table density="comfortable">
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
            <tr v-if="tournament.gameConditions?.winningSets">
              <td class="font-weight-bold">Winning Sets</td>
              <td>{{ tournament.gameConditions.winningSets }}</td>
            </tr>
            <tr v-if="tournament.gameConditions?.pointsPerSet">
              <td class="font-weight-bold">Points per Set</td>
              <td>{{ tournament.gameConditions.pointsPerSet.join(', ') }}</td>
            </tr>
            <tr v-if="tournament.tournamentCode">
              <td class="font-weight-bold">Tournament Code</td>
              <td>
                <v-chip color="primary" size="small">{{ tournament.tournamentCode }}</v-chip>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
    </v-card>

    <!-- Edit Dialog -->
    <v-dialog v-model="showEditDialog" max-width="500">
      <v-card title="Edit Tournament">
        <v-card-text>
          <v-text-field
            v-model="editForm.name"
            label="Name"
            variant="outlined"
            density="comfortable"
          />
          <v-select
            v-model="editForm.discipline"
            :items="disciplines"
            label="Discipline"
            variant="outlined"
            density="comfortable"
          />
          <v-text-field
            v-model="editForm.startTime"
            label="Start Time"
            type="datetime-local"
            variant="outlined"
            density="comfortable"
          />
          <v-text-field
            v-model.number="editForm.gameLength"
            label="Game Length (minutes)"
            type="number"
            variant="outlined"
            density="comfortable"
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
            <v-text-field
              v-model.number="editForm.gameConditions!.winningSets"
              label="Winning Sets"
              type="number"
              variant="outlined"
              density="comfortable"
            />
            <v-text-field
              v-model="editPointsPerSetText"
              label="Points per Set (comma-separated)"
              placeholder="25, 25, 15"
              variant="outlined"
              density="comfortable"
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
      <v-card title="Delete Tournament">
        <v-card-text>
          Are you sure you want to delete "{{ tournament?.name }}"? This action cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDeleteDialog = false">Cancel</v-btn>
          <v-btn color="error" variant="elevated" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

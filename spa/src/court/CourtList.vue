<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCourtStore } from '@/court/store'
import { useGameStore } from '@/game/store'
import { useSnackbar } from '@/composables/useSnackbar'
import SectionHeader from '@/components/SectionHeader.vue'
import ConfirmDeleteDialog from '@/components/ConfirmDeleteDialog.vue'
import CourtFormDialog from '@/court/CourtFormDialog.vue'
import GenerateCourtsDialog from '@/court/GenerateCourtsDialog.vue'
import CourtGames from '@/court/CourtGames.vue'
import LoadingBar from '@/components/LoadingBar.vue'
import { getErrorMessage } from '@/api/errors'
import type { CourtDto } from '@/court/types'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
  active: boolean
}>()

const route = useRoute()
const router = useRouter()
const courtStore = useCourtStore()
const gameStore = useGameStore()
const { showSuccess, showError } = useSnackbar()

const expandedCourt = ref<string | null>((route.query.court as string) || null)

watch(expandedCourt, (courtId) => {
  const query = { ...route.query }
  if (courtId) query.court = courtId
  else delete query.court
  router.replace({ query })
})

function toggleExpand(courtId?: string) {
  if (!courtId) return
  expandedCourt.value = expandedCourt.value === courtId ? null : courtId
}

// --- Dialog state ---

const showFormDialog = ref(false)
const showDeleteDialog = ref(false)
const showGenerateDialog = ref(false)
const editingCourt = ref<CourtDto | null>(null)
const deletingCourt = ref<CourtDto | null>(null)
const formDialog = ref<InstanceType<typeof CourtFormDialog> | null>(null)
const deleteDialog = ref<InstanceType<typeof ConfirmDeleteDialog> | null>(null)
const generateDialog = ref<InstanceType<typeof GenerateCourtsDialog> | null>(null)

onMounted(() => {
  courtStore.fetchCourts(props.tournamentId)
  gameStore.fetchGames(props.tournamentId)
})

watch(
  () => props.active,
  (isActive) => {
    if (!isActive) return
    courtStore.fetchCourts(props.tournamentId)
    gameStore.fetchGames(props.tournamentId)
  },
)

function openCreate() {
  editingCourt.value = null
  showFormDialog.value = true
}

function openEdit(court: CourtDto) {
  editingCourt.value = court
  showFormDialog.value = true
}

function openDelete(court: CourtDto) {
  deletingCourt.value = court
  showDeleteDialog.value = true
}

function openGenerate() {
  showGenerateDialog.value = true
}

async function handleSave(court: CourtDto) {
  try {
    if (editingCourt.value?.id) {
      await courtStore.updateCourt(props.tournamentId, editingCourt.value.id, court)
      showSuccess('Court updated')
    } else {
      await courtStore.createCourt(props.tournamentId, court)
      showSuccess('Court created')
    }
    showFormDialog.value = false
  } catch (error) {
    if (!formDialog.value?.handleError(error)) {
      showError(
        getErrorMessage(
          error,
          editingCourt.value ? 'Failed to update court' : 'Failed to create court',
        ),
      )
    }
  }
}

async function handleDelete() {
  if (!deletingCourt.value?.id) return
  try {
    await courtStore.deleteCourt(props.tournamentId, deletingCourt.value.id)
    showDeleteDialog.value = false
    showSuccess('Court deleted')
  } catch (error) {
    if (!deleteDialog.value?.handleError(error)) {
      showError(getErrorMessage(error, 'Failed to delete court'))
    }
  }
}

async function handleGenerate(count: number) {
  try {
    const generated = await courtStore.generateCourts(props.tournamentId, count)
    showGenerateDialog.value = false
    showSuccess(`Generated ${generated.length} courts`)
  } catch (error) {
    if (!generateDialog.value?.handleError(error)) {
      showError(getErrorMessage(error, 'Failed to generate courts'))
    }
  }
}
</script>

<template>
  <div>
    <SectionHeader title="Courts">
      <div>
        <v-btn
          v-if="isOwner"
          variant="outlined"
          color="primary"
          prepend-icon="mdi-apps"
          class="mr-2"
          @click="openGenerate"
        >
          Generate Courts
        </v-btn>
        <v-btn v-if="isOwner" color="primary" prepend-icon="mdi-plus" @click="openCreate">
          Add Court
        </v-btn>
      </div>
    </SectionHeader>

    <LoadingBar :loading="courtStore.loading" class="mb-4" />

    <v-card v-if="courtStore.courts.length > 0" class="data-table-card">
      <v-table density="comfortable" class="styled-table">
        <thead>
          <tr>
            <th>Name</th>
            <th v-if="isOwner" class="text-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="court in courtStore.courts" :key="court.id">
            <tr class="court-row" @click="toggleExpand(court.id)">
              <td>
                <v-icon
                  :icon="expandedCourt === court.id ? 'mdi-chevron-down' : 'mdi-chevron-right'"
                  size="small"
                  class="mr-1"
                />
                {{ court.name }}
              </td>
              <td v-if="isOwner" class="text-right">
                <v-btn
                  icon="mdi-pencil"
                  variant="text"
                  size="small"
                  :aria-label="'Edit court ' + court.name"
                  @click.stop="openEdit(court)"
                />
                <v-btn
                  icon="mdi-delete"
                  variant="text"
                  size="small"
                  color="error"
                  :aria-label="'Delete court ' + court.name"
                  @click.stop="openDelete(court)"
                />
              </td>
            </tr>
            <tr v-if="expandedCourt === court.id">
              <td :colspan="isOwner ? 2 : 1" class="pa-0 court-expanded-cell">
                <CourtGames
                  :tournament-id="tournamentId"
                  :court-id="court.id!"
                  :court-name="court.name"
                  :is-owner="isOwner"
                />
              </td>
            </tr>
          </template>
        </tbody>
      </v-table>
    </v-card>

    <v-alert v-else-if="!courtStore.loading" class="mt-4 mb-4" type="info" variant="tonal">
      No courts yet.
    </v-alert>

    <CourtFormDialog
      ref="formDialog"
      v-model="showFormDialog"
      :editing-court="editingCourt"
      @save="handleSave"
    />
    <ConfirmDeleteDialog
      ref="deleteDialog"
      v-model="showDeleteDialog"
      title="Delete Court"
      :message="`Are you sure you want to delete &quot;${deletingCourt?.name ?? ''}&quot;?`"
      @confirm="handleDelete"
    />
    <GenerateCourtsDialog
      ref="generateDialog"
      v-model="showGenerateDialog"
      :existing-court-count="courtStore.courts.length"
      @generate="handleGenerate"
    />
  </div>
</template>

<style scoped>
.data-table-card {
  border: 1px solid var(--ks-border);
}

.styled-table thead tr {
  background-color: rgb(var(--v-theme-surface-bright));
}

.styled-table tbody tr.court-row {
  cursor: pointer;
}

.styled-table tbody tr.court-row:hover {
  background-color: var(--ks-border-subtle);
}

.court-expanded-cell {
  background-color: rgb(var(--v-theme-surface-bright));
}
</style>

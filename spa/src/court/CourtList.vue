<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useCourtStore } from '@/court/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { useFormErrors } from '@/composables/useFormErrors'
import SectionHeader from '@/components/SectionHeader.vue'
import CourtGames from '@/court/CourtGames.vue'
import type { CourtDto } from '@/court/types'
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  tournamentId: string
  isOwner: boolean
}>()

const route = useRoute()
const router = useRouter()
const courtStore = useCourtStore()
const { showSuccess, showError } = useSnackbar()
const { fieldErrors, handleError, clearErrors, clearFieldError } = useFormErrors()

const showFormDialog = ref(false)
const showDeleteDialog = ref(false)
const editingCourt = ref<CourtDto | null>(null)
const deletingCourt = ref<CourtDto | null>(null)
const form = ref<CourtDto>({ name: '' })
const formRef = ref<InstanceType<typeof VForm> | null>(null)

const expandedCourt = ref<string | null>((route.query.court as string) || null)

watch(expandedCourt, (courtId) => {
  const query = { ...route.query }
  if (courtId) {
    query.court = courtId
  } else {
    delete query.court
  }
  router.replace({ query })
})

function toggleExpand(courtId?: string) {
  if (!courtId) return
  expandedCourt.value = expandedCourt.value === courtId ? null : courtId
}

const nameRules = [
  (v: string) => !!v || 'Court name is required.',
  (v: string) => v.length <= 100 || 'Court name must not exceed 100 characters.',
]

onMounted(() => {
  courtStore.fetchCourts(props.tournamentId)
})

function openCreate() {
  editingCourt.value = null
  form.value = { name: '' }
  clearErrors()
  showFormDialog.value = true
}

function openEdit(court: CourtDto) {
  editingCourt.value = court
  form.value = { ...court }
  clearErrors()
  showFormDialog.value = true
}

function openDelete(court: CourtDto) {
  deletingCourt.value = court
  showDeleteDialog.value = true
}

async function handleSave() {
  const { valid } = await formRef.value!.validate()
  if (!valid) return

  try {
    if (editingCourt.value?.id) {
      await courtStore.updateCourt(props.tournamentId, editingCourt.value.id, form.value)
      showSuccess('Court updated')
    } else {
      await courtStore.createCourt(props.tournamentId, form.value)
      showSuccess('Court created')
    }
    showFormDialog.value = false
  } catch (error) {
    if (!handleError(error)) {
      showError(editingCourt.value ? 'Failed to update court' : 'Failed to create court')
    }
  }
}

async function handleDelete() {
  if (!deletingCourt.value?.id) return
  try {
    await courtStore.deleteCourt(props.tournamentId, deletingCourt.value.id)
    showDeleteDialog.value = false
    showSuccess('Court deleted')
  } catch {
    showError('Failed to delete court')
  }
}
</script>

<template>
  <div>
    <SectionHeader title="Courts">
      <v-btn v-if="isOwner" color="primary" prepend-icon="mdi-plus" @click="openCreate">
        Add Court
      </v-btn>
    </SectionHeader>

    <v-progress-linear v-if="courtStore.loading" indeterminate color="primary" class="mb-4" />

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
                <v-btn icon="mdi-pencil" variant="text" size="small" @click.stop="openEdit(court)" />
                <v-btn icon="mdi-delete" variant="text" size="small" color="error" @click.stop="openDelete(court)" />
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

    <v-alert class="mt-4 mb-4" v-else-if="!courtStore.loading" type="info" variant="tonal">
      No courts yet.
    </v-alert>

    <!-- Create / Edit Dialog -->
    <v-dialog v-model="showFormDialog" max-width="500">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">
          {{ editingCourt ? 'Edit Court' : 'Add Court' }}
        </v-card-title>
        <v-card-text>
          <v-form ref="formRef" @submit.prevent="handleSave">
            <v-text-field
              v-model="form.name"
              label="Name"
              :rules="nameRules"
              :error-messages="fieldErrors('name')"
              @update:model-value="clearFieldError('name')"
            />
          </v-form>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showFormDialog = false">Cancel</v-btn>
          <v-btn color="primary" variant="elevated" @click="handleSave">
            {{ editingCourt ? 'Save' : 'Create' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation -->
    <v-dialog v-model="showDeleteDialog" max-width="400">
      <v-card class="pa-2">
        <v-card-title class="text-uppercase dialog-title">Delete Court</v-card-title>
        <v-card-text>
          Are you sure you want to delete "{{ deletingCourt?.name }}"?
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

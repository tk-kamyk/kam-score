<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface Team {
  id: string
  name: string
}

const props = defineProps<{
  teams: Team[]
  initialOrder: string[]
  editable: boolean
  saving?: boolean
}>()

const emit = defineEmits<{
  save: [orderedTeamIds: string[]]
}>()

const orderedTeams = ref<Team[]>([])
const dirty = ref(false)

const hasSavedOrder = computed(() => props.initialOrder.length === props.teams.length)

function resetFromProps() {
  const byId = new Map(props.teams.map((t) => [t.id, t]))
  if (hasSavedOrder.value) {
    orderedTeams.value = props.initialOrder
      .map((id) => byId.get(id))
      .filter((t): t is Team => t !== undefined)
  } else {
    orderedTeams.value = [...props.teams]
  }
  dirty.value = false
}

watch(
  () => [props.teams, props.initialOrder],
  () => resetFromProps(),
  { immediate: true, deep: true },
)

function moveUp(index: number) {
  if (index <= 0) return
  const copy = [...orderedTeams.value]
  ;[copy[index - 1], copy[index]] = [copy[index], copy[index - 1]]
  orderedTeams.value = copy
  dirty.value = true
}

function moveDown(index: number) {
  if (index >= orderedTeams.value.length - 1) return
  const copy = [...orderedTeams.value]
  ;[copy[index], copy[index + 1]] = [copy[index + 1], copy[index]]
  orderedTeams.value = copy
  dirty.value = true
}

function handleSave() {
  emit(
    'save',
    orderedTeams.value.map((t) => t.id),
  )
  dirty.value = false
}

function handleReset() {
  resetFromProps()
}
</script>

<template>
  <v-card class="data-table-card">
    <v-alert
      v-if="editable && !hasSavedOrder"
      type="info"
      variant="tonal"
      density="compact"
      class="ma-3"
    >
      No standings saved yet for this group. Reorder teams into the final order and click
      <strong>Save standings</strong>.
    </v-alert>

    <v-alert
      v-if="!editable && !hasSavedOrder"
      type="info"
      variant="tonal"
      density="compact"
      class="ma-3"
    >
      Standings have not been entered for this group yet.
    </v-alert>

    <v-table v-if="editable || hasSavedOrder" density="compact" class="styled-table">
      <thead>
        <tr>
          <th scope="col" class="text-center" style="width: 4rem">#</th>
          <th scope="col">Team</th>
          <th v-if="editable" scope="col" class="text-center" style="width: 7rem">Reorder</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(team, index) in orderedTeams" :key="team.id">
          <td class="text-center">{{ index + 1 }}</td>
          <td>{{ team.name }}</td>
          <td v-if="editable" class="text-center">
            <v-btn
              icon="mdi-arrow-up"
              size="x-small"
              variant="text"
              :ripple="false"
              :disabled="index === 0 || saving"
              :aria-label="`Move ${team.name} up`"
              @click="moveUp(index)"
            />
            <v-btn
              icon="mdi-arrow-down"
              size="x-small"
              variant="text"
              :ripple="false"
              :disabled="index === orderedTeams.length - 1 || saving"
              :aria-label="`Move ${team.name} down`"
              @click="moveDown(index)"
            />
          </td>
        </tr>
      </tbody>
    </v-table>

    <v-card-actions v-if="editable" class="px-4 pb-3">
      <v-spacer />
      <v-btn variant="text" :disabled="!dirty || saving" @click="handleReset">Reset</v-btn>
      <v-btn
        color="primary"
        variant="elevated"
        :disabled="!dirty || saving"
        :loading="saving"
        @click="handleSave"
      >
        Save standings
      </v-btn>
    </v-card-actions>
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

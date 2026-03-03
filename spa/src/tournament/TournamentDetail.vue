<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import TournamentInfo from '@/tournament/TournamentInfo.vue'
import TeamList from '@/team/TeamList.vue'
import CourtList from '@/court/CourtList.vue'
import StructureDetail from '@/structure/StructureDetail.vue'
import ScheduleOverview from '@/game/ScheduleOverview.vue'
import type { TournamentDto } from '@/tournament/types'

const props = defineProps<{ id: string }>()

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()
const { smAndDown } = useDisplay()

const validTabs = ['details', 'teams', 'courts', 'structure', 'schedule']
const activeTab = ref(validTabs.includes(route.query.tab as string) ? (route.query.tab as string) : 'details')

watch(activeTab, (tab) => {
  router.replace({ query: { ...route.query, tab } })
})

const tournament = computed(() => tournamentStore.currentTournament)
const isOwner = computed(() =>
  auth.isAuthenticated && tournament.value?.ownerId === auth.username
)

const tabLabels: Record<string, string> = {
  details: 'Details',
  teams: 'Teams',
  courts: 'Courts',
  structure: 'Structure',
  schedule: 'Schedule',
}

const breadcrumbItems = computed(() => [
  { title: 'Tournaments', to: { name: 'home' }, disabled: false },
  { title: tournament.value?.name ?? '...', disabled: false },
  { title: tabLabels[activeTab.value] ?? activeTab.value, disabled: true },
])

function handleBreadcrumbClick() {
  activeTab.value = 'details'
}

onMounted(() => {
  tournamentStore.fetchTournament(props.id)
})

async function handleUpdate(dto: TournamentDto) {
  try {
    await tournamentStore.updateTournament(props.id, dto)
    showSuccess('Tournament updated')
  } catch {
    showError('Failed to update tournament')
  }
}

async function handleDelete() {
  try {
    await tournamentStore.deleteTournament(props.id)
    showSuccess('Tournament deleted')
    router.push({ name: 'home' })
  } catch {
    showError('Failed to delete tournament')
  }
}
</script>

<template>
  <div>
    <v-breadcrumbs :items="breadcrumbItems" class="breadcrumbs px-0 py-2 mb-4 text-body-small text-md-body-medium">
      <template #divider>
        <v-icon icon="mdi-chevron-right" size="small" />
      </template>
      <template #item="{ item }">
        <v-breadcrumbs-item
          v-if="item.to"
          class="breadcrumb-clickable"
          :to="item.to"
          :disabled="item.disabled"
        >
          {{ item.title }}
        </v-breadcrumbs-item>
        <span
          v-else-if="!item.disabled"
          class="breadcrumb-clickable"
          @click="handleBreadcrumbClick()"
        >
          {{ item.title }}
        </span>
        <v-breadcrumbs-item v-else :disabled="item.disabled">
          {{ item.title }}
        </v-breadcrumbs-item>
      </template>
    </v-breadcrumbs>

    <v-progress-linear v-if="tournamentStore.loading" indeterminate color="primary" />

    <template v-if="tournament">
      <h2 class="section-title text-title-large text-md-headline-medium text-lg-headline-large mb-6">{{ tournament.name }}</h2>

      <v-tabs v-model="activeTab" color="primary" class="mb-6" slider-color="primary" :density="smAndDown ? 'compact' : 'comfortable'">
        <v-tab value="details">Details</v-tab>
        <v-tab value="teams">Teams</v-tab>
        <v-tab value="courts">Courts</v-tab>
        <v-tab value="structure">Structure</v-tab>
        <v-tab value="schedule">Schedule</v-tab>
      </v-tabs>

      <v-tabs-window v-model="activeTab">
        <v-tabs-window-item value="details">
          <TournamentInfo
            :tournament="tournament"
            :is-owner="isOwner"
            @updated="handleUpdate"
            @deleted="handleDelete"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="teams">
          <TeamList :tournament-id="id" :is-owner="isOwner" />
        </v-tabs-window-item>

        <v-tabs-window-item value="courts">
          <CourtList :tournament-id="id" :is-owner="isOwner" />
        </v-tabs-window-item>

        <v-tabs-window-item value="structure">
          <StructureDetail :tournament-id="id" :is-owner="isOwner" />
        </v-tabs-window-item>

        <v-tabs-window-item value="schedule">
          <ScheduleOverview :tournament-id="id" :is-owner="isOwner" />
        </v-tabs-window-item>
      </v-tabs-window>
    </template>
  </div>
</template>

<style scoped>
.breadcrumbs :deep(a) {
    color: rgb(var(--v-theme-primary));
    text-decoration: none;
}

.breadcrumbs :deep(.v-breadcrumbs-item--disabled) {
    opacity: 0.5;
}

.breadcrumb-clickable {
    color: rgb(var(--v-theme-primary));
    cursor: pointer;
    max-width: 200px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    display: inline-block;
    vertical-align: bottom;
}

.breadcrumb-clickable:hover {
    text-decoration: underline;
}

</style>

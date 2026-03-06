<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import TournamentBreadcrumb from '@/tournament/TournamentBreadcrumb.vue'
import TournamentInfo from '@/tournament/TournamentInfo.vue'
import TeamList from '@/team/TeamList.vue'
import CourtList from '@/court/CourtList.vue'
import StructureDetail from '@/structure/StructureDetail.vue'
import ScheduleOverview from '@/game/ScheduleOverview.vue'
import StandingsOverview from '@/standings/StandingsOverview.vue'
import type { TournamentDto } from '@/tournament/types'

const props = defineProps<{ id: string }>()

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()
const { smAndDown } = useDisplay()

const validTabs = ['details', 'teams', 'courts', 'structure', 'schedule', 'standings']
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
  standings: 'Standings',
}

const breadcrumbItems = computed(() => [
  { title: 'Tournaments', to: { name: 'home' }, disabled: false },
  { title: tournament.value?.name ?? '...', disabled: false },
  { title: tabLabels[activeTab.value] ?? activeTab.value, disabled: true },
])

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
    <TournamentBreadcrumb :items="breadcrumbItems" @navigate="activeTab = 'details'" />

    <v-progress-linear v-if="tournamentStore.loading" indeterminate color="primary" aria-label="Loading tournament" />

    <template v-if="tournament">
      <h2 class="section-title text-title-medium text-md-title-large text-lg-headline-medium mb-6">{{ tournament.name }}</h2>

      <v-tabs v-model="activeTab" color="primary" class="mb-4" slider-color="primary" aria-label="Tournament sections" :density="smAndDown ? 'compact' : 'comfortable'">
        <v-tab v-for="tab in validTabs" :key="tab" :value="tab" :size="smAndDown ? 'default' : 'large'">
          {{ tabLabels[tab] }}
        </v-tab>
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

        <v-tabs-window-item value="standings">
          <StandingsOverview :tournament-id="id" :is-owner="isOwner" :active="activeTab === 'standings'" />
        </v-tabs-window-item>

        <v-tabs-window-item value="schedule">
          <ScheduleOverview :tournament-id="id" :is-owner="isOwner" />
        </v-tabs-window-item>
      </v-tabs-window>
    </template>
  </div>
</template>

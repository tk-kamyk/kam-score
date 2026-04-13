<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useDisplay } from 'vuetify'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { parseErrorDetail } from '@/api/errors'
import TournamentBreadcrumb from '@/tournament/TournamentBreadcrumb.vue'
import TournamentInfo from '@/tournament/TournamentInfo.vue'
import FinalStandings from '@/standings/FinalStandings.vue'
import { useStandingsStore } from '@/standings/store'
import TeamList from '@/team/TeamList.vue'
import CourtList from '@/court/CourtList.vue'
import StructureDetail from '@/structure/StructureDetail.vue'
import ScheduleOverview from '@/game/ScheduleOverview.vue'
import StandingsOverview from '@/standings/StandingsOverview.vue'
import VolunteerList from '@/volunteer/VolunteerList.vue'
import type { TournamentDto } from '@/tournament/types'

const props = defineProps<{ id: string }>()

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()
const { smAndDown } = useDisplay()
const standingsStore = useStandingsStore()

const tournament = computed(() => tournamentStore.currentTournament)
const isOwner = computed(
  () => auth.isAuthenticated && (tournament.value?.ownerId === auth.username || auth.isAdmin),
)

const showVolunteersTab = computed(() => isOwner.value)

const validTabs = computed(() => {
  const tabs = ['overview', 'teams', 'courts', 'structure', 'schedule', 'standings']
  if (showVolunteersTab.value) tabs.push('volunteers')
  return tabs
})
const activeTab = ref(
  validTabs.value.includes(route.query.tab as string) ? (route.query.tab as string) : 'overview',
)

watch(validTabs, (tabs) => {
  const urlTab = route.query.tab as string
  if (urlTab && tabs.includes(urlTab) && activeTab.value !== urlTab) {
    activeTab.value = urlTab
  } else if (!tabs.includes(activeTab.value)) {
    activeTab.value = 'overview'
  }
})

watch(activeTab, (tab) => {
  router.replace({ query: { ...route.query, tab } })
  if (tab === 'overview') {
    standingsStore.fetchFinalStandings(props.id)
  }
})

const tabLabels: Record<string, string> = {
  overview: 'Overview',
  teams: 'Teams',
  courts: 'Courts',
  structure: 'Structure',
  schedule: 'Schedule',
  standings: 'Standings',
  volunteers: 'Volunteers',
}

const breadcrumbItems = computed(() => [
  { title: 'Tournaments', to: { name: 'home' }, disabled: false },
  { title: tournament.value?.name ?? '...', disabled: false },
  { title: tabLabels[activeTab.value] ?? activeTab.value, disabled: true },
])

onMounted(() => {
  tournamentStore.fetchTournament(props.id)
  standingsStore.fetchFinalStandings(props.id)
})

async function handleUpdate(dto: TournamentDto) {
  try {
    await tournamentStore.updateTournament(props.id, dto)
    showSuccess('Tournament updated')
  } catch (error) {
    showError(parseErrorDetail(error) ?? 'Failed to update tournament')
  }
}

const deleting = ref(false)

async function handleDelete() {
  deleting.value = true
  try {
    await tournamentStore.deleteTournament(props.id)
    showSuccess('Tournament deleted')
    router.push({ name: 'home' })
  } catch (error) {
    showError(parseErrorDetail(error) ?? 'Failed to delete tournament')
  } finally {
    deleting.value = false
  }
}
</script>

<template>
  <div>
    <TournamentBreadcrumb :items="breadcrumbItems" @navigate="activeTab = 'overview'" />

    <v-progress-linear
      v-if="tournamentStore.loading"
      indeterminate
      color="primary"
      aria-label="Loading tournament"
    />

    <template v-if="tournament">
      <h2 class="section-title text-title-medium text-md-title-large text-lg-headline-medium mb-6">
        {{ tournament.name }}
      </h2>

      <v-tabs
        v-model="activeTab"
        color="primary"
        class="mb-4"
        slider-color="primary"
        aria-label="Tournament sections"
        :density="smAndDown ? 'compact' : 'comfortable'"
      >
        <v-tab
          v-for="tab in validTabs"
          :key="tab"
          :value="tab"
          :size="smAndDown ? 'default' : 'large'"
        >
          {{ tabLabels[tab] }}
        </v-tab>
      </v-tabs>

      <v-tabs-window v-model="activeTab">
        <v-tabs-window-item value="overview">
          <TournamentInfo
            :tournament="tournament"
            :is-owner="isOwner"
            :deleting="deleting"
            @updated="handleUpdate"
            @deleted="handleDelete"
          />
          <FinalStandings
            :data="standingsStore.finalStandings"
            :loading="standingsStore.finalStandingsLoading"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="teams">
          <TeamList :tournament-id="id" :is-owner="isOwner" :active="activeTab === 'teams'" />
        </v-tabs-window-item>

        <v-tabs-window-item value="courts">
          <CourtList :tournament-id="id" :is-owner="isOwner" :active="activeTab === 'courts'" />
        </v-tabs-window-item>

        <v-tabs-window-item value="structure">
          <StructureDetail
            :tournament-id="id"
            :is-owner="isOwner"
            :active="activeTab === 'structure'"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="standings">
          <StandingsOverview
            :tournament-id="id"
            :is-owner="isOwner"
            :active="activeTab === 'standings'"
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="schedule">
          <ScheduleOverview
            :tournament-id="id"
            :is-owner="isOwner"
            :active="activeTab === 'schedule'"
          />
        </v-tabs-window-item>

        <v-tabs-window-item v-if="showVolunteersTab" value="volunteers">
          <VolunteerList :tournament-id="id" :active="activeTab === 'volunteers'" />
        </v-tabs-window-item>
      </v-tabs-window>
    </template>
  </div>
</template>

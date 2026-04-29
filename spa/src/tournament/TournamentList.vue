<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import { getErrorMessage } from '@/api/errors'
import TournamentCreateDialog from '@/tournament/TournamentCreateDialog.vue'
import LoadingBar from '@/components/LoadingBar.vue'
import type { TournamentDto } from '@/tournament/types'

const router = useRouter()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()

const showCreateDialog = ref(false)
const creating = ref(false)

const sortedTournaments = computed(() =>
  [...tournamentStore.tournaments].sort(
    (a, b) => new Date(b.lastModified ?? 0).getTime() - new Date(a.lastModified ?? 0).getTime(),
  ),
)

onMounted(() => {
  tournamentStore.fetchTournaments()
})

async function handleCreated(dto: TournamentDto) {
  creating.value = true
  try {
    const created = await tournamentStore.createTournament(dto)
    showCreateDialog.value = false
    showSuccess('Tournament created')
    router.push({ name: 'tournament', params: { id: created.id } })
  } catch (error) {
    showError(getErrorMessage(error, 'Failed to create tournament'))
  } finally {
    creating.value = false
  }
}

function navigateToTournament(id: string) {
  router.push({ name: 'tournament', params: { id } })
}
</script>

<template>
  <div>
    <div class="hero-section text-center mb-10">
      <img src="/volleyball.svg" alt="Volleyball" class="hero-icon mb-4" />
      <h1 class="hero-title text-title-large text-md-headline-large text-uppercase mb-2">
        Tournament Management
      </h1>
      <p class="hero-subtitle mb-6">Organize, schedule, and track your volleyball tournaments</p>
      <v-btn
        v-if="auth.isAuthenticated"
        color="primary"
        size="large"
        prepend-icon="mdi-plus"
        :disabled="creating"
        @click="showCreateDialog = true"
      >
        New Tournament
      </v-btn>
    </div>

    <LoadingBar :loading="tournamentStore.loading" class="mb-4" />

    <v-row>
      <v-col v-for="tournament in sortedTournaments" :key="tournament.id" cols="12" md="6">
        <v-card
          class="tournament-card h-100 pa-2"
          hover
          @click="navigateToTournament(tournament.id!)"
        >
          <v-card-title class="text-uppercase tournament-card-title">
            {{ tournament.name }}
          </v-card-title>
          <v-card-subtitle class="mt-1">{{ tournament.discipline }}</v-card-subtitle>
          <v-card-text class="pt-3 d-flex flex-row justify-space-between">
            <div class="d-flex flex-wrap ga-1">
              <v-chip size="small" variant="outlined" prepend-icon="mdi-account" color="primary">
                {{ tournament.ownerDisplayName ?? 'Unknown' }}
              </v-chip>
              <v-chip size="small" variant="outlined" prepend-icon="mdi-account-group-outline">
                {{ tournament.teamCount ?? 0 }} teams
              </v-chip>
              <v-chip size="small" variant="outlined" prepend-icon="mdi-volleyball">
                {{ tournament.courtCount ?? 0 }} courts
              </v-chip>
              <v-chip
                v-if="tournament.gameLength"
                size="small"
                variant="outlined"
                prepend-icon="mdi-clock"
              >
                {{ tournament.gameLength }} min
              </v-chip>
            </div>
            <div>
              <v-chip
                v-if="tournament.tournamentCode"
                size="small"
                prepend-icon="mdi-key"
                color="secondary"
                variant="tonal"
                class="font-weight-bold code-chip"
              >
                {{ tournament.tournamentCode }}
              </v-chip>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-card
      v-if="!tournamentStore.loading && tournamentStore.tournaments.length === 0"
      class="pa-12 text-center"
    >
      <v-icon size="72" color="secondary" class="mb-4 empty-icon" aria-hidden="true"
        >mdi-trophy-outline</v-icon
      >
      <p
        class="text-title-medium text-sm-headline-small text-uppercase mb-2 dialog-title empty-heading"
      >
        No tournaments yet
      </p>
      <p v-if="auth.isAuthenticated" class="text-body-medium empty-hint">
        Click the button above to create your first tournament.
      </p>
    </v-card>

    <TournamentCreateDialog
      v-model="showCreateDialog"
      :loading="creating"
      @created="handleCreated"
    />
  </div>
</template>

<style scoped>
.hero-section {
  padding-top: 24px;
}

.hero-icon {
  width: 80px;
  height: 80px;
}

.hero-title {
  font-weight: 700;
  letter-spacing: 2px;
}

.hero-subtitle {
  opacity: 0.5;
  font-size: 1rem;
}

.tournament-card {
  border: 1px solid var(--ks-border);
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease;
}

.tournament-card:hover {
  border-color: var(--ks-primary-border);
  box-shadow: 0 0 20px var(--ks-primary-glow);
}

.tournament-card-title {
  letter-spacing: 1px;
  white-space: pre-wrap;
}

.empty-icon {
  opacity: 0.6;
}

.empty-heading {
  opacity: 0.5;
}

.empty-hint {
  opacity: 0.4;
}
</style>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/auth/store'
import { useTournamentStore } from '@/tournament/store'
import { useSnackbar } from '@/composables/useSnackbar'
import TournamentInfo from '@/tournament/TournamentInfo.vue'
import TeamList from '@/team/TeamList.vue'
import type { TournamentDto } from '@/tournament/types'

const props = defineProps<{ id: string }>()

const router = useRouter()
const auth = useAuthStore()
const tournamentStore = useTournamentStore()
const { showSuccess, showError } = useSnackbar()

const activeTab = ref('details')

const tournament = computed(() => tournamentStore.currentTournament)
const isOwner = computed(() =>
  auth.isAuthenticated && tournament.value?.ownerId === auth.username
)

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
    <v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-4" @click="router.push({ name: 'home' })">
      Back to Tournaments
    </v-btn>

    <v-progress-linear v-if="tournamentStore.loading" indeterminate color="primary" />

    <template v-if="tournament">
      <h2 class="text-h5 mb-4">{{ tournament.name }}</h2>

      <v-tabs v-model="activeTab" color="primary" class="mb-4">
        <v-tab value="details">Details</v-tab>
        <v-tab value="teams">Teams</v-tab>
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
      </v-tabs-window>
    </template>
  </div>
</template>

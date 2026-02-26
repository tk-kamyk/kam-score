<script setup lang="ts">
import { useAuthStore } from '@/auth/store'
import { useSnackbar } from '@/composables/useSnackbar'
import LoginDialog from '@/auth/LoginDialog.vue'

const auth = useAuthStore()
const { show: snackbarShow, message: snackbarMessage, color: snackbarColor } = useSnackbar()
</script>

<template>
  <v-app>
    <v-app-bar flat color="transparent" density="comfortable" class="app-bar-border">
      <v-app-bar-title>
        <router-link to="/" class="brand-link text-decoration-none">
          <span class="brand-text">Kam<sup>2</sup> Score</span>
        </router-link>
      </v-app-bar-title>
      <template #append>
        <span v-if="auth.isAuthenticated" class="text-body-2 mr-3" style="color: rgba(var(--ks-text), 0.7);">
          {{ auth.displayName }}
        </span>
        <v-btn
          v-if="auth.isAuthenticated"
          icon="mdi-logout"
          variant="text"
          size="small"
          @click="auth.logout()"
        />
        <v-btn
          v-else
          icon="mdi-login"
          variant="text"
          size="small"
          @click="auth.showLoginDialog = true"
        />
      </template>
    </v-app-bar>

    <v-main>
      <v-container class="main-container">
        <router-view />
      </v-container>
    </v-main>

    <LoginDialog />

    <v-snackbar v-model="snackbarShow" :color="snackbarColor" :timeout="3000">
      {{ snackbarMessage }}
    </v-snackbar>
  </v-app>
</template>

<style scoped>
.app-bar-border {
    border-bottom: 1px solid rgba(var(--ks-surface), 0.6) !important;
}

.brand-link {
    color: inherit;
}

.brand-text {
    font-family: 'Dosis', sans-serif;
    font-weight: 700;
    font-size: 1.4rem;
    letter-spacing: 2px;
    text-transform: uppercase;
    color: #fff;
}

.main-container {
    max-width: 1200px;
    padding-left: 24px;
    padding-right: 24px;
}

@media (min-width: 1280px) {
    .main-container {
        max-width: 1400px;
        padding-left: 40px;
        padding-right: 40px;
    }
}

@media (min-width: 1920px) {
    .main-container {
        padding-left: 64px;
        padding-right: 64px;
    }
}
</style>

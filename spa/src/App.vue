<script setup lang="ts">
import { useAuthStore } from '@/auth/store'
import { useSnackbar } from '@/composables/useSnackbar'
import LoginDialog from '@/auth/LoginDialog.vue'

const auth = useAuthStore()
const { show: snackbarShow, message: snackbarMessage, color: snackbarColor } = useSnackbar()
</script>

<template>
  <v-app>
    <v-app-bar flat color="background" density="comfortable" class="app-bar-border">
      <v-app-bar-title>
        <router-link to="/" class="brand-link text-decoration-none">
          <span class="brand-text">Kam<sup>2</sup> Score</span>
        </router-link>
      </v-app-bar-title>
      <template #append>
        <span v-if="auth.isAuthenticated" class="text-body-medium mr-3 text-muted-color">
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

    <v-main class="ks-content">
      <v-container class="main-container">
        <router-view />
      </v-container>
    </v-main>

    <LoginDialog />

    <v-snackbar v-model="snackbarShow" :color="snackbarColor" :timeout="3000" class="ks-snackbar">
      {{ snackbarMessage }}
    </v-snackbar>
  </v-app>
</template>

<style scoped>
.app-bar-border {
    border-bottom: 1px solid var(--ks-border-strong);
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

@media (min-width: 1145px) {
    .main-container {
        max-width: 1400px;
        padding-left: 40px;
        padding-right: 40px;
    }
}

@media (min-width: 1545px) {
    .main-container {
        padding-left: 64px;
        padding-right: 64px;
    }
}

@media (max-width: 599px) {
    .brand-text {
        font-size: 1.15rem;
        letter-spacing: 1px;
    }

    .main-container {
        padding-left: 12px;
        padding-right: 12px;
    }
}
</style>

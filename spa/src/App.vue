<script setup lang="ts">
import { useAuthStore } from '@/auth/store'
import { useSnackbar } from '@/composables/useSnackbar'
import LoginDialog from '@/auth/LoginDialog.vue'

const auth = useAuthStore()
const { show: snackbarShow, message: snackbarMessage, color: snackbarColor } = useSnackbar()
</script>

<template>
  <v-app>
    <v-app-bar color="primary" density="comfortable">
      <v-app-bar-title>
        <router-link to="/" class="text-white text-decoration-none">KamScore</router-link>
      </v-app-bar-title>
      <template #append>
        <span v-if="auth.isAuthenticated" class="text-body-2 mr-4">
          {{ auth.displayName }}
        </span>
        <v-btn
          v-if="auth.isAuthenticated"
          icon="mdi-logout"
          variant="text"
          @click="auth.logout()"
        />
        <v-btn
          v-else
          icon="mdi-login"
          variant="text"
          @click="auth.showLoginDialog = true"
        />
      </template>
    </v-app-bar>

    <v-main>
      <v-container>
        <router-view />
      </v-container>
    </v-main>

    <LoginDialog />

    <v-snackbar v-model="snackbarShow" :color="snackbarColor" :timeout="3000">
      {{ snackbarMessage }}
    </v-snackbar>
  </v-app>
</template>

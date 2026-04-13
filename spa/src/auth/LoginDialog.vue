<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/auth/store'
import { useSnackbar } from '@/composables/useSnackbar'

const auth = useAuthStore()
const { showError } = useSnackbar()

const username = ref('')
const password = ref('')
const loading = ref(false)

async function handleLogin() {
  loading.value = true
  try {
    await auth.login({ username: username.value, password: password.value })
    username.value = ''
    password.value = ''
  } catch {
    showError('Invalid username or password')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <v-dialog
    v-model="auth.showLoginDialog"
    max-width="400"
    persistent
    aria-labelledby="login-dialog-title"
  >
    <v-card class="pa-2 login-card">
      <v-card-title id="login-dialog-title" class="text-uppercase dialog-title">Login</v-card-title>
      <v-card-text>
        <v-text-field v-model="username" label="Username" autofocus @keyup.enter="handleLogin" />
        <v-text-field
          v-model="password"
          label="Password"
          type="password"
          @keyup.enter="handleLogin"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="auth.showLoginDialog = false">Cancel</v-btn>
        <v-btn
          color="primary"
          variant="elevated"
          :loading="loading"
          :disabled="!username || !password"
          @click="handleLogin"
        >
          Login
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.login-card {
  border: 1px solid var(--ks-border);
}
</style>

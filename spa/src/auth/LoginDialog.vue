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
  <v-dialog v-model="auth.showLoginDialog" max-width="400" persistent>
    <v-card title="Login">
      <v-card-text>
        <v-text-field
          v-model="username"
          label="Username"
          variant="outlined"
          density="comfortable"
          autofocus
          @keyup.enter="handleLogin"
        />
        <v-text-field
          v-model="password"
          label="Password"
          type="password"
          variant="outlined"
          density="comfortable"
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

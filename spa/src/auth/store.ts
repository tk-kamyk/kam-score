import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiClient, { registerUnauthorizedHandler } from '@/api/client'
import { isTokenValid } from '@/auth/token'
import type { LoginRequest, LoginResponse } from '@/auth/types'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const username = ref<string | null>(localStorage.getItem('username'))
  const displayName = ref<string | null>(localStorage.getItem('displayName'))
  const role = ref<string | null>(localStorage.getItem('role'))
  const showLoginDialog = ref(false)

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => role.value === 'Admin')

  // Clear expired token on startup (page load/refresh)
  if (token.value && !isTokenValid(token.value)) {
    token.value = null
    username.value = null
    displayName.value = null
    role.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('username')
    localStorage.removeItem('displayName')
    localStorage.removeItem('role')
  }

  // Bridge Axios 401 interceptor to the store
  registerUnauthorizedHandler(() => {
    if (!token.value) return
    logout()
    showLoginDialog.value = true
  })

  async function login(credentials: LoginRequest) {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', credentials)
    token.value = data.token
    username.value = data.username
    displayName.value = data.displayName
    role.value = data.role
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
    localStorage.setItem('displayName', data.displayName)
    localStorage.setItem('role', data.role)
    showLoginDialog.value = false
  }

  function logout() {
    token.value = null
    username.value = null
    displayName.value = null
    role.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('username')
    localStorage.removeItem('displayName')
    localStorage.removeItem('role')
  }

  return {
    token,
    username,
    displayName,
    role,
    showLoginDialog,
    isAuthenticated,
    isAdmin,
    login,
    logout,
  }
})

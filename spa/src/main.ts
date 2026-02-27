import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'
import './styles/global.css'
import App from './App.vue'
import router from './router'

// Vuetify theme — hex values match CSS vars in global.css:
//   --ks-bg       → background
//   --ks-surface   → surface-light (surface & surface-bright are derived shades)
//   --ks-primary   → primary  (global.css also has --ks-primary-dark for filled buttons)
//   --ks-secondary → secondary
//   --ks-text      → on-background, on-surface
const kamScoreDark = {
  dark: true,
  colors: {
    background: '#2a2e33',        // --ks-bg
    surface: '#33383f',
    'surface-bright': '#3e454d',
    'surface-light': '#4a5259',   // --ks-surface
    primary: '#29b5d4',           // --ks-primary (lighter for readability on dark bg)
    secondary: '#E8B930',         // --ks-secondary
    error: '#e5534b',
    success: '#56d364',
    warning: '#e3b341',
    'on-background': '#e6edf3',   // --ks-text
    'on-surface': '#e6edf3',      // --ks-text
  },
}

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: 'kamScoreDark',
    themes: {
      kamScoreDark,
    },
  },
  defaults: {
    VCard: {
      rounded: 'lg',
      elevation: 0,
    },
    VBtn: {
      rounded: 'lg',
    },
    VTextField: {
      variant: 'outlined',
      density: 'comfortable',
    },
    VSelect: {
      variant: 'outlined',
      density: 'comfortable',
    },
    VDialog: {
      scrim: 'black',
    },
  },
})

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(vuetify)
app.mount('#app')

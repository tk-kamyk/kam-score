<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" class="text-center">
        <v-icon size="120" class="mb-4" style="opacity: 0.3;">mdi-alert-circle-outline</v-icon>
        <h1 class="text-h3 font-weight-bold text-uppercase mb-2" style="letter-spacing: 2px;">Page Not Found</h1>
        <p class="text-body-1 mb-8" style="opacity: 0.5;">The page you are looking for does not exist.</p>
        <v-btn color="secondary" size="large" variant="elevated" to="/">Back to Home</v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>

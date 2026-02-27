<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" class="text-center">
        <v-icon size="120" class="mb-4 not-found-icon">mdi-alert-circle-outline</v-icon>
        <h1 class="text-h3 font-weight-bold text-uppercase mb-2 not-found-heading">Page Not Found</h1>
        <p class="text-body-1 mb-8 not-found-text">The page you are looking for does not exist.</p>
        <v-btn color="secondary" size="large" variant="elevated" to="/">Back to Home</v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>

<style scoped>
.not-found-icon {
    opacity: 0.3;
}

.not-found-heading {
    letter-spacing: 2px;
}

.not-found-text {
    opacity: 0.5;
}
</style>

<script setup lang="ts">
defineProps<{
  items: Array<{ title: string; to?: object; disabled: boolean }>
}>()

const emit = defineEmits<{ navigate: [] }>()
</script>

<template>
  <v-breadcrumbs
    :items="items"
    class="breadcrumbs px-0 py-2 mb-4 text-body-small text-md-body-medium"
  >
    <template #divider>
      <v-icon icon="mdi-chevron-right" size="small" />
    </template>
    <template #item="{ item }">
      <v-breadcrumbs-item
        v-if="item.to"
        class="breadcrumb-clickable"
        :to="item.to"
        :disabled="item.disabled"
      >
        {{ item.title }}
      </v-breadcrumbs-item>
      <span v-else-if="!item.disabled" class="breadcrumb-clickable" @click="emit('navigate')">
        {{ item.title }}
      </span>
      <v-breadcrumbs-item v-else :disabled="item.disabled">
        {{ item.title }}
      </v-breadcrumbs-item>
    </template>
  </v-breadcrumbs>
</template>

<style scoped>
.breadcrumbs :deep(a) {
  color: rgb(var(--v-theme-primary));
  text-decoration: none;
}

.breadcrumbs :deep(.v-breadcrumbs-item--disabled) {
  opacity: 0.5;
}

.breadcrumb-clickable {
  color: rgb(var(--v-theme-primary));
  cursor: pointer;
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: inline-block;
  vertical-align: bottom;
}

.breadcrumb-clickable:hover {
  text-decoration: underline;
}
</style>

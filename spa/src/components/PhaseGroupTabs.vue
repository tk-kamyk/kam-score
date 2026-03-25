<script setup lang="ts">
import { computed } from 'vue'
import type { GroupDto, LevelDto } from '@/structure/types'

interface TabItem {
  type: 'tab'
  id: string
  label: string
}

interface LabelItem {
  type: 'label'
  id: string
  text: string
}

type TabEntry = TabItem | LabelItem

const props = defineProps<{
  groups: GroupDto[]
  levels: LevelDto[]
  selectedGroupId: string | null
}>()

const emit = defineEmits<{
  'select-group': [groupId: string]
}>()

const hasMultipleGroups = computed(() => props.groups.length > 1)

const entries = computed<TabEntry[]>(() => {
  const hasLevels = props.levels.length > 0

  if (!hasLevels) {
    return props.groups.map(g => ({
      type: 'tab' as const,
      id: g.id!,
      label: `Group ${g.name}`,
    }))
  }

  const result: TabEntry[] = []
  for (const level of props.levels) {
    const levelGroups = props.groups.filter(g => g.levelId === level.id)
    if (levelGroups.length === 1) {
      result.push({ type: 'tab', id: levelGroups[0].id!, label: level.name })
    } else if (levelGroups.length > 1) {
      result.push({ type: 'label', id: `label-${level.id}`, text: level.name })
      for (const g of levelGroups) {
        result.push({ type: 'tab', id: g.id!, label: `Group ${g.name}` })
      }
    }
  }
  return result
})
</script>

<template>
  <div v-if="hasMultipleGroups" class="mb-4">
    <v-tabs
      :model-value="selectedGroupId"
      @update:model-value="(val: unknown) => { if (typeof val === 'string') emit('select-group', val) }"
      show-arrows
      grow
      density="compact"
      color="primary"
      aria-label="Select group"
    >
      <template v-for="entry in entries" :key="entry.id">
        <v-chip v-if="entry.type === 'label'"
          size="default"
          color="secondary"
          variant="outlined"
          class="level-divider ml-12"
          prepend-icon="mdi-sitemap"
          append-icon="mdi-chevron-right">
          {{ entry.text }}
        </v-chip>
        <v-tab v-else :value="entry.id">
          {{ entry.label }}
        </v-tab>
      </template>
    </v-tabs>
  </div>
</template>

<style scoped>
.level-divider {
  align-self: center;
  font-weight: bold;
  border: 0;
  opacity: .8;
}

.level-divider:first-child {
  margin-left: 0;
}
</style>

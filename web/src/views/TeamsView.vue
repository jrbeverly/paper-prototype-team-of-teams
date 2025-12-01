<template>
  <div>
    <h1 class="text-h4 mb-4">Teams</h1>
    <v-text-field
      v-model="search"
      label="Search by name or skill"
      prepend-inner-icon="mdi-magnify"
      clearable
      hide-details
      class="mb-6"
    />
    <div v-if="loading" class="d-flex justify-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>
    <v-alert v-else-if="error" type="error" class="mb-4">{{ error }}</v-alert>
    <template v-else>
      <v-row v-if="filtered.length">
        <v-col v-for="team in filtered" :key="team.teamId" cols="12" sm="6" lg="4">
          <v-card height="100%" :to="`/teams/${team.teamId}`">
            <v-card-title class="d-flex align-center justify-space-between">
              <span>{{ team.name }}</span>
              <v-btn
                v-if="isAuthenticated"
                :icon="isObserved(team.teamId) ? 'mdi-eye' : 'mdi-eye-outline'"
                :color="isObserved(team.teamId) ? 'primary' : undefined"
                size="small"
                variant="text"
                :loading="toggling === team.teamId"
                @click.stop="toggleObserve(team.teamId)"
              />
            </v-card-title>
            <v-card-subtitle v-if="team.purpose">{{ team.purpose }}</v-card-subtitle>
            <v-card-text>
              <div v-if="team.skills.length" class="mb-3">
                <v-chip v-for="skill in team.skills" :key="skill" size="small" class="mr-1 mb-1">
                  {{ skill }}
                </v-chip>
              </div>
              <div v-if="team.openPositions.length">
                <span class="text-caption text-medium-emphasis d-block mb-1">Open positions</span>
                <v-chip
                  v-for="pos in team.openPositions"
                  :key="pos"
                  size="small"
                  color="primary"
                  class="mr-1 mb-1"
                >
                  {{ pos }}
                </v-chip>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
      <p v-else class="text-body-1 text-medium-emphasis">
        {{ search ? 'No teams match your search.' : 'No teams found.' }}
      </p>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { listTeams, getMyObservations, observeTeam, unobserveTeam } from '../api/client.js'
import { getIdToken } from '../auth/cognito.js'

const teams = ref([])
const search = ref('')
const loading = ref(true)
const error = ref(null)
const isAuthenticated = ref(false)
const observations = ref([])
const toggling = ref(null)

const filtered = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return teams.value
  return teams.value.filter(
    (t) =>
      t.name.toLowerCase().includes(q) ||
      t.skills.some((s) => s.toLowerCase().includes(q)),
  )
})

const isObserved = (teamId) => observations.value.some((o) => o.teamId === teamId)

onMounted(async () => {
  isAuthenticated.value = !!(await getIdToken())
  const [teamsResult, obsResult] = await Promise.allSettled([
    listTeams(),
    isAuthenticated.value ? getMyObservations() : Promise.reject(),
  ])
  if (teamsResult.status === 'fulfilled') {
    teams.value = teamsResult.value
  } else {
    error.value = 'Failed to load teams.'
  }
  if (obsResult.status === 'fulfilled') {
    observations.value = obsResult.value
  }
  loading.value = false
})

async function toggleObserve(teamId) {
  toggling.value = teamId
  try {
    if (isObserved(teamId)) {
      await unobserveTeam(teamId)
    } else {
      await observeTeam(teamId)
    }
    observations.value = await getMyObservations()
  } finally {
    toggling.value = null
  }
}
</script>

<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" to="/teams" class="mb-4">All Teams</v-btn>
    <div v-if="loading" class="d-flex justify-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>
    <v-alert v-else-if="notFound" type="warning">Team not found.</v-alert>
    <v-alert v-else-if="error" type="error">Failed to load team.</v-alert>
    <template v-else-if="team">
      <div class="d-flex align-center justify-space-between mb-1 flex-wrap gap-2">
        <h1 class="text-h4">{{ team.name }}</h1>
        <div v-if="isAuthenticated" class="d-flex gap-2 flex-wrap align-center">
          <v-btn
            variant="outlined"
            :loading="observing"
            @click="isObserving ? handleUnobserve() : handleObserve()"
          >
            {{ isObserving ? 'Unobserve' : 'Observe' }}
          </v-btn>
          <v-btn
            v-if="isActiveMember"
            color="error"
            variant="outlined"
            :loading="joining"
            @click="handleLeave"
          >
            Leave Team
          </v-btn>
          <template v-else-if="hasPendingTransfer">
            <v-chip color="warning" prepend-icon="mdi-clock-outline">Transfer Pending</v-chip>
            <v-btn
              variant="outlined"
              size="small"
              :loading="cancellingTransfer"
              @click="handleCancelTransfer"
            >
              Cancel Transfer
            </v-btn>
          </template>
          <v-btn
            v-else
            color="primary"
            variant="flat"
            :loading="joining"
            @click="handleJoinClick"
          >
            {{ membership?.activeTeam ? 'Transfer to this team' : 'Join Team' }}
          </v-btn>
        </div>
      </div>
      <v-alert v-if="joinError" type="error" class="mb-4" closable @click:close="joinError = null">
        {{ joinError }}
      </v-alert>

      <p v-if="team.purpose" class="text-body-1 text-medium-emphasis mb-2">{{ team.purpose }}</p>
      <p v-if="team.mission" class="text-body-1 mb-6">{{ team.mission }}</p>

      <v-row>
        <v-col v-if="team.openPositions.length" cols="12" md="6">
          <h2 class="text-h6 mb-2">Open Positions</h2>
          <v-chip
            v-for="pos in team.openPositions"
            :key="pos"
            color="primary"
            class="mr-1 mb-1"
          >
            {{ pos }}
          </v-chip>
        </v-col>
        <v-col v-if="team.skills.length" cols="12" md="6">
          <h2 class="text-h6 mb-2">Skills</h2>
          <v-chip v-for="skill in team.skills" :key="skill" class="mr-1 mb-1">{{ skill }}</v-chip>
        </v-col>
        <v-col v-if="team.objectives.length" cols="12">
          <h2 class="text-h6 mb-2">Objectives</h2>
          <v-list lines="one" density="compact">
            <v-list-item v-for="obj in team.objectives" :key="obj" :title="obj" />
          </v-list>
        </v-col>
        <v-col v-if="team.responsibilities.length" cols="12" md="6">
          <h2 class="text-h6 mb-2">Responsibilities</h2>
          <v-list lines="one" density="compact">
            <v-list-item v-for="r in team.responsibilities" :key="r" :title="r" />
          </v-list>
        </v-col>
        <v-col v-if="team.commitments.length" cols="12" md="6">
          <h2 class="text-h6 mb-2">Commitments</h2>
          <v-list lines="one" density="compact">
            <v-list-item v-for="c in team.commitments" :key="c" :title="c" />
          </v-list>
        </v-col>
      </v-row>

      <v-dialog v-model="confirmDialog" max-width="400">
        <v-card>
          <v-card-title class="text-h6">Transfer to {{ team.name }}?</v-card-title>
          <v-card-text>
            You are currently on <strong>{{ fromTeamName }}</strong>. Joining
            <strong>{{ team.name }}</strong> will immediately transfer you there
            and end your membership on your current team.
          </v-card-text>
          <v-card-actions class="justify-end">
            <v-btn variant="text" @click="confirmDialog = false">Cancel</v-btn>
            <v-btn color="primary" variant="flat" :loading="joining" @click="confirmTransfer">
              Transfer
            </v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>

      <v-divider class="my-6" />

      <h2 class="text-h6 mb-2">Members ({{ team.members.length }})</h2>
      <p v-if="!team.members.length" class="text-body-2 text-medium-emphasis">No members yet.</p>
      <v-chip
        v-for="m in team.members"
        :key="m.personId"
        :to="`/people/${m.personId}`"
        class="mr-1 mb-1"
      >
        {{ m.name }}
      </v-chip>

      <v-divider class="my-6" />

      <div class="d-flex align-center justify-space-between mb-2">
        <h2 class="text-h6">Roles</h2>
        <v-btn
          v-if="isAuthenticated && team.members.length"
          size="small"
          variant="text"
          prepend-icon="mdi-plus"
          @click="showRoleForm = !showRoleForm"
        >
          Assign
        </v-btn>
      </div>

      <v-expand-transition>
        <v-card v-if="showRoleForm" variant="outlined" class="mb-4 pa-3">
          <v-row dense>
            <v-col cols="12" sm="5">
              <v-combobox
                v-model="newRoleName"
                label="Role"
                density="compact"
                :items="roleNameSuggestions"
                hide-details
              />
            </v-col>
            <v-col cols="12" sm="5">
              <v-select
                v-model="newRolePersonId"
                label="Member"
                density="compact"
                :items="team.members.map((m) => ({ title: m.name, value: m.personId }))"
                hide-details
              />
            </v-col>
            <v-col cols="12" sm="2" class="d-flex align-center">
              <v-btn size="small" color="primary" :loading="roleLoading" @click="handleAssignRole">
                Assign
              </v-btn>
            </v-col>
          </v-row>
          <v-alert v-if="roleError" type="error" density="compact" class="mt-2">
            {{ roleError }}
          </v-alert>
        </v-card>
      </v-expand-transition>

      <p v-if="!team.roles.length" class="text-body-2 text-medium-emphasis">No roles assigned.</p>
      <v-list v-else density="compact" class="pa-0">
        <v-list-item
          v-for="r in team.roles"
          :key="`${r.roleName}-${r.personId}`"
          class="px-0"
        >
          <template #title>
            <span class="font-weight-medium">{{ r.roleName }}</span>
            <span class="text-medium-emphasis ml-2 text-body-2">{{ memberName(r.personId) }}</span>
          </template>
          <template #append>
            <v-btn
              v-if="isAuthenticated"
              icon="mdi-close"
              size="x-small"
              variant="text"
              :loading="removingRole === `${r.roleName}-${r.personId}`"
              @click="handleRemoveRole(r)"
            />
          </template>
        </v-list-item>
      </v-list>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import {
  getTeam,
  joinTeam,
  leaveTeam,
  getMyMembership,
  getMyObservations,
  observeTeam,
  unobserveTeam,
  assignRole,
  removeRole,
  cancelTransfer,
} from '../api/client.js'
import { getIdToken } from '../auth/cognito.js'

const route = useRoute()
const team = ref(null)
const membership = ref(null)
const observations = ref([])
const loading = ref(true)
const notFound = ref(false)
const error = ref(false)
const isAuthenticated = ref(false)
const joining = ref(false)
const joinError = ref(null)
const observing = ref(false)
const confirmDialog = ref(false)
const fromTeamName = ref('')
const showRoleForm = ref(false)
const newRoleName = ref('')
const newRolePersonId = ref('')
const roleLoading = ref(false)
const roleError = ref(null)
const removingRole = ref(null)
const cancellingTransfer = ref(false)

const roleNameSuggestions = [
  'DRI',
  'Quality Champion',
  'Security Champion',
  'Accessibility Champion',
  'Documentation Champion',
  'Operations Champion',
]

const isActiveMember = computed(
  () => membership.value?.activeTeam?.teamId === route.params.id,
)

const pendingTransfer = computed(() =>
  membership.value?.transfers?.find(
    (t) => t.toTeamId === route.params.id && t.status === 'PENDING',
  ),
)

const hasPendingTransfer = computed(() => !!pendingTransfer.value)

const isObserving = computed(
  () => observations.value.some((o) => o.teamId === route.params.id),
)

onMounted(async () => {
  isAuthenticated.value = !!(await getIdToken())
  const [teamResult, membershipResult, observationsResult] = await Promise.allSettled([
    getTeam(route.params.id),
    isAuthenticated.value ? getMyMembership() : Promise.reject(),
    isAuthenticated.value ? getMyObservations() : Promise.reject(),
  ])
  if (teamResult.status === 'fulfilled') {
    team.value = teamResult.value
  } else {
    const msg = teamResult.reason?.message ?? ''
    if (msg === 'HTTP 404') notFound.value = true
    else error.value = true
  }
  if (membershipResult.status === 'fulfilled') {
    membership.value = membershipResult.value
  }
  if (observationsResult.status === 'fulfilled') {
    observations.value = observationsResult.value
  }
  if (membership.value?.activeTeam && membership.value.activeTeam.teamId !== route.params.id) {
    try {
      const fromTeam = await getTeam(membership.value.activeTeam.teamId)
      fromTeamName.value = fromTeam.name
    } catch {
      fromTeamName.value = membership.value.activeTeam.teamId
    }
  }
  loading.value = false
})

function handleJoinClick() {
  if (membership.value?.activeTeam) {
    confirmDialog.value = true
  } else {
    handleJoin()
  }
}

async function confirmTransfer() {
  confirmDialog.value = false
  await handleJoin()
}

async function handleJoin() {
  joining.value = true
  joinError.value = null
  try {
    membership.value = await joinTeam(route.params.id).then(() => getMyMembership())
    team.value = await getTeam(route.params.id)
  } catch {
    joinError.value = 'Failed to join team.'
  } finally {
    joining.value = false
  }
}

async function handleLeave() {
  joining.value = true
  joinError.value = null
  try {
    await leaveTeam(route.params.id)
    membership.value = await getMyMembership()
    team.value = await getTeam(route.params.id)
  } catch {
    joinError.value = 'Failed to leave team.'
  } finally {
    joining.value = false
  }
}

async function handleCancelTransfer() {
  if (!pendingTransfer.value) return
  cancellingTransfer.value = true
  joinError.value = null
  try {
    await cancelTransfer(pendingTransfer.value.transferId)
    membership.value = await getMyMembership()
  } catch {
    joinError.value = 'Failed to cancel transfer.'
  } finally {
    cancellingTransfer.value = false
  }
}

async function handleObserve() {
  observing.value = true
  try {
    await observeTeam(route.params.id)
    observations.value = await getMyObservations()
  } catch {
    // no visible error — state stays unchanged
  } finally {
    observing.value = false
  }
}

async function handleUnobserve() {
  observing.value = true
  try {
    await unobserveTeam(route.params.id)
    observations.value = await getMyObservations()
  } catch {
    // no visible error — state stays unchanged
  } finally {
    observing.value = false
  }
}

async function handleAssignRole() {
  roleError.value = null
  if (!newRoleName.value || !newRolePersonId.value) {
    roleError.value = 'Role name and member are required.'
    return
  }
  roleLoading.value = true
  try {
    await assignRole(route.params.id, { roleName: newRoleName.value, personId: newRolePersonId.value })
    team.value = await getTeam(route.params.id)
    newRoleName.value = ''
    newRolePersonId.value = ''
    showRoleForm.value = false
  } catch {
    roleError.value = 'Failed to assign role.'
  } finally {
    roleLoading.value = false
  }
}

function memberName(personId) {
  return team.value?.members.find((m) => m.personId === personId)?.name ?? personId
}

async function handleRemoveRole(r) {
  const key = `${r.roleName}-${r.personId}`
  removingRole.value = key
  try {
    await removeRole(route.params.id, r.roleName, r.personId)
    team.value = await getTeam(route.params.id)
  } catch {
    // no visible error — role stays in list
  } finally {
    removingRole.value = null
  }
}
</script>

<template>
  <div>
    <h1 class="text-h4 mb-6">My Profile</h1>
    <div v-if="loading" class="d-flex justify-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>
    <v-alert v-else-if="loadError" type="error" class="mb-4">{{ loadError }}</v-alert>
    <template v-else-if="profile">
      <v-alert v-if="saveError" type="error" class="mb-4">{{ saveError }}</v-alert>
      <v-card>
        <v-card-text>
          <v-row>
            <v-col cols="12" md="8">
              <v-text-field
                v-model="form.name"
                label="Display Name"
                :readonly="!editing"
                class="mb-2"
              />
              <v-textarea
                v-model="form.bio"
                label="Bio"
                :readonly="!editing"
                rows="3"
                class="mb-2"
              />
              <v-combobox
                v-model="form.skills"
                label="Skills"
                multiple
                chips
                closable-chips
                :readonly="!editing"
                hint="Type a skill and press Enter to add"
                :persistent-hint="editing"
              />
            </v-col>
            <v-col cols="12" md="4">
              <p class="text-caption text-medium-emphasis mb-1">Email</p>
              <p class="text-body-1 mb-4">{{ profile.email }}</p>
              <v-chip v-if="profile.isAdmin" color="warning" size="small">Admin</v-chip>
            </v-col>
          </v-row>
        </v-card-text>
        <v-card-actions class="px-4 pb-4">
          <v-btn v-if="!editing" color="primary" variant="flat" @click="startEdit">
            Edit Profile
          </v-btn>
          <template v-else>
            <v-btn color="primary" variant="flat" :loading="saving" @click="save">Save</v-btn>
            <v-btn variant="text" :disabled="saving" @click="cancelEdit">Cancel</v-btn>
          </template>
        </v-card-actions>
      </v-card>

      <v-card class="mt-4">
        <v-card-title>Active Team</v-card-title>
        <v-card-text>
          <template v-if="membership?.activeTeam">
            <div class="d-flex align-center flex-wrap gap-2 mb-3">
              <v-chip :to="`/teams/${membership.activeTeam.teamId}`" color="primary">
                {{ activeTeamDetail?.name ?? membership.activeTeam.teamId }}
              </v-chip>
              <span class="text-caption text-medium-emphasis">
                Member since {{ membership.activeTeam.joinedAt }}
              </span>
            </div>
            <div v-if="myRoles().length">
              <p class="text-caption text-medium-emphasis mb-1">Your roles</p>
              <v-chip
                v-for="r in myRoles()"
                :key="r.roleName"
                size="small"
                class="mr-1 mb-1"
              >
                {{ r.roleName }}
              </v-chip>
            </div>
          </template>
          <p v-else class="text-body-2 text-medium-emphasis">
            You are not a member of any team.
            <router-link to="/teams">Discover teams</router-link> to find one that fits.
          </p>
        </v-card-text>
        <v-card-actions v-if="membership?.activeTeam" class="px-4 pb-4">
          <v-btn
            color="error"
            variant="outlined"
            :loading="leaving"
            @click="handleLeave"
          >
            Leave Team
          </v-btn>
        </v-card-actions>
      </v-card>

      <v-card class="mt-4">
        <v-card-title>Observed Teams</v-card-title>
        <v-card-text>
          <p v-if="!observations.length" class="text-body-2 text-medium-emphasis">
            Not observing any teams.
            <router-link to="/teams">Browse teams</router-link> to observe one.
          </p>
          <template v-else>
            <v-chip
              v-for="o in observations"
              :key="o.teamId"
              :to="`/teams/${o.teamId}`"
              class="mr-2 mb-2"
            >
              {{ o.teamId }}
            </v-chip>
          </template>
        </v-card-text>
      </v-card>

      <template v-if="membership?.transfers.length">
        <h2 class="text-h6 mt-6 mb-2">Transfer History</h2>
        <v-list lines="two" class="pa-0">
          <v-list-item
            v-for="t in membership.transfers"
            :key="t.transferId"
            :subtitle="`${t.fromTeamId} → ${t.toTeamId}`"
            class="px-0"
          >
            <template #title>
              <span class="text-body-2">{{ t.requestedAt }}</span>
              <v-chip size="x-small" class="ml-2" :color="statusColor(t.status)">
                {{ t.status }}
              </v-chip>
            </template>
          </v-list-item>
        </v-list>
      </template>

      <template v-if="profile.isAdmin">
        <h2 class="text-h6 mt-6 mb-2">Pending Transfers</h2>
        <div v-if="pendingLoading" class="d-flex justify-center py-4">
          <v-progress-circular indeterminate color="primary" size="24" />
        </div>
        <p v-else-if="!pendingTransfers.length" class="text-body-2 text-medium-emphasis">
          No pending transfers.
        </p>
        <v-list v-else lines="two" class="pa-0">
          <v-list-item
            v-for="t in pendingTransfers"
            :key="t.transferId"
            :subtitle="`${t.fromTeamName} → ${t.toTeamName}`"
            class="px-0"
          >
            <template #title>
              <span class="text-body-2 font-weight-medium">{{ t.personName }}</span>
              <span class="text-caption text-medium-emphasis ml-2">{{ t.requestedAt }}</span>
            </template>
            <template #append>
              <v-btn
                size="small"
                color="success"
                variant="outlined"
                class="mr-2"
                :loading="actioning === t.transferId + '-APPROVE'"
                @click="handleAction(t.transferId, 'APPROVE')"
              >
                Approve
              </v-btn>
              <v-btn
                size="small"
                color="error"
                variant="outlined"
                :loading="actioning === t.transferId + '-REJECT'"
                @click="handleAction(t.transferId, 'REJECT')"
              >
                Reject
              </v-btn>
            </template>
          </v-list-item>
        </v-list>
      </template>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import {
  getMe,
  patchMe,
  getMyMembership,
  getMyObservations,
  leaveTeam,
  getTeam,
  getPendingTransfers,
  actionTransfer,
} from '../api/client.js'

const profile = ref(null)
const membership = ref(null)
const observations = ref([])
const activeTeamDetail = ref(null)
const loading = ref(true)
const loadError = ref(null)
const editing = ref(false)
const saving = ref(false)
const saveError = ref(null)
const leaving = ref(false)
const form = ref({ name: '', bio: '', skills: [] })
const pendingTransfers = ref([])
const pendingLoading = ref(false)
const actioning = ref(null)

function myRoles() {
  return activeTeamDetail.value?.roles.filter((r) => r.personId === profile.value?.personId) ?? []
}

function statusColor(status) {
  if (status === 'APPROVED') return 'success'
  if (status === 'PENDING') return 'warning'
  if (status === 'REJECTED') return 'error'
  return undefined
}

onMounted(async () => {
  try {
    const [profileResult, membershipResult, observationsResult] = await Promise.allSettled([
      getMe(),
      getMyMembership(),
      getMyObservations(),
    ])
    if (profileResult.status === 'fulfilled') {
      profile.value = profileResult.value
      resetForm()
    } else {
      loadError.value = 'Failed to load profile.'
    }
    if (membershipResult.status === 'fulfilled') {
      membership.value = membershipResult.value
    }
    if (observationsResult.status === 'fulfilled') {
      observations.value = observationsResult.value
    }
    if (membership.value?.activeTeam) {
      try {
        activeTeamDetail.value = await getTeam(membership.value.activeTeam.teamId)
      } catch {
        // non-critical — team name falls back to teamId, roles hidden
      }
    }
    if (profile.value?.isAdmin) {
      pendingLoading.value = true
      try {
        pendingTransfers.value = await getPendingTransfers(profile.value.personId)
      } catch {
        // non-critical
      } finally {
        pendingLoading.value = false
      }
    }
  } finally {
    loading.value = false
  }
})

function resetForm() {
  form.value = {
    name: profile.value.name,
    bio: profile.value.bio ?? '',
    skills: [...profile.value.skills],
  }
}

function startEdit() {
  saveError.value = null
  editing.value = true
}

function cancelEdit() {
  editing.value = false
  resetForm()
}

async function save() {
  saving.value = true
  saveError.value = null
  try {
    profile.value = await patchMe({
      name: form.value.name || null,
      bio: form.value.bio || null,
      skills: form.value.skills,
    })
    editing.value = false
    resetForm()
  } catch {
    saveError.value = 'Failed to save profile.'
  } finally {
    saving.value = false
  }
}

async function handleAction(transferId, action) {
  actioning.value = `${transferId}-${action}`
  try {
    await actionTransfer(transferId, action, profile.value.personId)
    pendingTransfers.value = await getPendingTransfers(profile.value.personId)
  } catch {
    // non-critical — list stays unchanged
  } finally {
    actioning.value = null
  }
}

async function handleLeave() {
  if (!membership.value?.activeTeam) return
  leaving.value = true
  try {
    await leaveTeam(membership.value.activeTeam.teamId)
    membership.value = await getMyMembership()
    activeTeamDetail.value = null
  } catch {
    // ignore — user stays on the page
  } finally {
    leaving.value = false
  }
}
</script>

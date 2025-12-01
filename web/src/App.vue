<template>
  <v-app>
    <v-app-bar color="primary">
      <v-app-bar-title>Team of Teams</v-app-bar-title>
      <v-btn variant="text" to="/">Home</v-btn>
      <v-btn variant="text" to="/teams">Teams</v-btn>
      <v-btn variant="text" to="/people">People</v-btn>
      <template v-if="isAuthenticated">
        <v-btn variant="text" to="/profile">Profile</v-btn>
        <v-btn variant="text" @click="handleSignOut">Sign Out</v-btn>
      </template>
      <v-btn v-else variant="text" to="/sign-in">Sign In</v-btn>
    </v-app-bar>
    <v-main>
      <v-container>
        <router-view />
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { getIdToken, signOut } from './auth/cognito.js'

const router = useRouter()
const route = useRoute()
const isAuthenticated = ref(false)

async function refreshAuth() {
  isAuthenticated.value = !!(await getIdToken())
}

onMounted(refreshAuth)
watch(route, refreshAuth)

async function handleSignOut() {
  signOut()
  isAuthenticated.value = false
  router.push('/sign-in')
}
</script>

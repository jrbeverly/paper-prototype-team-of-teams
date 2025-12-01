<template>
  <div style="max-width: 400px; margin: 0 auto">
    <h1 class="text-h4 mb-6">Sign In</h1>
    <v-alert v-if="error" type="error" class="mb-4">{{ error }}</v-alert>
    <v-text-field v-model="email" label="Email" type="email" autocomplete="email" class="mb-2" />
    <v-text-field
      v-model="password"
      label="Password"
      type="password"
      autocomplete="current-password"
      class="mb-4"
      @keyup.enter="handleSignIn"
    />
    <v-btn color="primary" block :loading="loading" @click="handleSignIn">Sign In</v-btn>
    <p class="text-center mt-4 text-body-2">
      Don't have an account? <router-link to="/sign-up">Sign up</router-link>
    </p>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { signIn } from '../auth/cognito.js'

const router = useRouter()
const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref(null)

async function handleSignIn() {
  error.value = null
  loading.value = true
  try {
    await signIn(email.value, password.value)
    router.push('/')
  } catch (e) {
    error.value = e.message || 'Sign in failed.'
  } finally {
    loading.value = false
  }
}
</script>

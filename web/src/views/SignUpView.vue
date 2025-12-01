<template>
  <div style="max-width: 400px; margin: 0 auto">
    <h1 class="text-h4 mb-6">Sign Up</h1>
    <v-alert v-if="error" type="error" class="mb-4">{{ error }}</v-alert>

    <template v-if="!needsConfirmation">
      <v-text-field v-model="email" label="Email" type="email" autocomplete="email" class="mb-2" />
      <v-text-field
        v-model="password"
        label="Password"
        type="password"
        autocomplete="new-password"
        hint="Minimum 8 characters with uppercase, lowercase, and numbers"
        persistent-hint
        class="mb-4"
      />
      <v-btn color="primary" block :loading="loading" @click="handleSignUp">Sign Up</v-btn>
      <p class="text-center mt-4 text-body-2">
        Already have an account? <router-link to="/sign-in">Sign in</router-link>
      </p>
    </template>

    <template v-else>
      <p class="text-body-1 mb-4">
        Check your email for a verification code and enter it below.
      </p>
      <v-text-field
        v-model="code"
        label="Verification Code"
        class="mb-4"
        @keyup.enter="handleConfirm"
      />
      <v-btn color="primary" block :loading="loading" @click="handleConfirm">Confirm</v-btn>
    </template>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { signUp, confirmSignUp } from '../auth/cognito.js'

const router = useRouter()
const email = ref('')
const password = ref('')
const code = ref('')
const loading = ref(false)
const error = ref(null)
const needsConfirmation = ref(false)

async function handleSignUp() {
  error.value = null
  loading.value = true
  try {
    await signUp(email.value, password.value)
    needsConfirmation.value = true
  } catch (e) {
    error.value = e.message || 'Sign up failed.'
  } finally {
    loading.value = false
  }
}

async function handleConfirm() {
  error.value = null
  loading.value = true
  try {
    await confirmSignUp(email.value, code.value)
    router.push('/sign-in')
  } catch (e) {
    error.value = e.message || 'Confirmation failed.'
  } finally {
    loading.value = false
  }
}
</script>

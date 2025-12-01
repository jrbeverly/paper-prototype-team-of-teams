<template>
  <div>
    <h1 class="text-h4 mb-6">Welcome to Team of Teams</h1>
    <v-chip :color="statusColor" prepend-icon="mdi-circle" size="large">
      Backend: {{ statusText }}
    </v-chip>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { healthCheck } from '../api/client.js'

const statusText = ref('checking…')
const statusColor = ref('default')

onMounted(async () => {
  try {
    const { status } = await healthCheck()
    statusText.value = status
    statusColor.value = 'success'
  } catch {
    statusText.value = 'unreachable'
    statusColor.value = 'error'
  }
})
</script>

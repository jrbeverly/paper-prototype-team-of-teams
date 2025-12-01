<template>
  <div>
    <v-btn variant="text" prepend-icon="mdi-arrow-left" to="/people" class="mb-4">
      All People
    </v-btn>
    <div v-if="loading" class="d-flex justify-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>
    <v-alert v-else-if="notFound" type="warning">Person not found.</v-alert>
    <v-alert v-else-if="error" type="error">Failed to load profile.</v-alert>
    <template v-else-if="person">
      <h1 class="text-h4 mb-1">{{ person.name }}</h1>
      <p v-if="person.bio" class="text-body-1 text-medium-emphasis mb-4">{{ person.bio }}</p>
      <div v-if="person.skills.length" class="mt-4">
        <h2 class="text-h6 mb-2">Skills</h2>
        <v-chip v-for="skill in person.skills" :key="skill" class="mr-1 mb-1">
          {{ skill }}
        </v-chip>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getPerson } from '../api/client.js'

const route = useRoute()
const person = ref(null)
const loading = ref(true)
const notFound = ref(false)
const error = ref(false)

onMounted(async () => {
  try {
    person.value = await getPerson(route.params.id)
  } catch (e) {
    if (e.message === 'HTTP 404') notFound.value = true
    else error.value = true
  } finally {
    loading.value = false
  }
})
</script>

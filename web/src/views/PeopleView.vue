<template>
  <div>
    <h1 class="text-h4 mb-4">People</h1>
    <div v-if="loading" class="d-flex justify-center py-8">
      <v-progress-circular indeterminate color="primary" />
    </div>
    <v-alert v-else-if="error" type="error" class="mb-4">{{ error }}</v-alert>
    <template v-else>
      <v-row v-if="people.length">
        <v-col v-for="person in people" :key="person.personId" cols="12" sm="6" lg="4">
          <v-card height="100%" :to="`/people/${person.personId}`">
            <v-card-title>{{ person.name }}</v-card-title>
            <v-card-subtitle v-if="person.bio">{{ person.bio }}</v-card-subtitle>
            <v-card-text v-if="person.skills.length">
              <v-chip
                v-for="skill in person.skills"
                :key="skill"
                size="small"
                class="mr-1 mb-1"
              >
                {{ skill }}
              </v-chip>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
      <p v-else class="text-body-1 text-medium-emphasis">No people found.</p>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { listPeople } from '../api/client.js'

const people = ref([])
const loading = ref(true)
const error = ref(null)

onMounted(async () => {
  try {
    people.value = await listPeople()
  } catch {
    error.value = 'Failed to load people.'
  } finally {
    loading.value = false
  }
})
</script>

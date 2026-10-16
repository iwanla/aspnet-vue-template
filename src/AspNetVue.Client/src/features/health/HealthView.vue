<template>
  <section>
    <h1>Health</h1>
    <button :disabled="loading" @click="load">Refresh</button>
    <p v-if="loading">Loading…</p>
    <p v-else-if="error" style="color: red">{{ error }}</p>
    <pre v-else>{{ result }}</pre>
    <p><small>GET <code>/health</code> (AspNetCore HealthChecks + Npgsql).</small></p>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getJson } from '@/shared/api/http'

const loading = ref(false)
const error = ref('')
const result = ref<unknown>(null)

async function load() {
  loading.value = true
  error.value = ''
  try {
    result.value = await getJson('/health')
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

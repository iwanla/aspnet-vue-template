<template>
  <section class="page-section">
    <header class="section-head">
      <h1 class="title">Database health</h1>
      <p class="lede">
        The API answers this page from <code>/api/health</code>. The only registered check opens the
        Npgsql connection, so an unhealthy result means PostgreSQL is not reachable.
      </p>
    </header>

    <div class="report" :data-state="state">
      <div class="report__bar">
        <span class="signal">
          <span class="signal__dot" aria-hidden="true"></span>
          <span class="signal__text" role="status">{{ stateLabel }}</span>
        </span>

        <span v-if="report" class="report__timing">Reported in {{ report.totalDuration }}</span>
        <span v-else-if="httpStatus" class="report__timing">HTTP {{ httpStatus }}</span>

        <button class="button button--quiet" type="button" :disabled="loading" @click="reload">
          {{ loading ? 'Checking…' : 'Check again' }}
        </button>
      </div>

      <p v-if="error" class="report__message">{{ error }}</p>

      <ul v-else-if="report && report.entries.length" class="checks">
        <li
          v-for="entry in report.entries"
          :key="entry.key"
          class="checks__item"
          :data-state="stateFromStatus(entry.status)"
        >
          <span class="checks__name">{{ entry.key }}</span>
          <span class="checks__status">{{ entry.status }}</span>
          <span class="checks__duration">{{ entry.duration }}</span>
          <span v-if="entry.description" class="checks__description">{{ entry.description }}</span>
        </li>
      </ul>

      <p v-else-if="!loading" class="report__message">The report contained no checks.</p>
    </div>

    <details v-if="raw" class="raw">
      <summary class="raw__summary">Response body</summary>
      <pre class="raw__body">{{ raw }}</pre>
    </details>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { stateFromStatus, useHealthCheck } from './useHealthCheck'

const { loading, error, report, httpStatus, raw, state, reload } = useHealthCheck()

const stateLabel = computed(() => {
  switch (state.value) {
    case 'checking':
      return 'Checking'
    case 'ok':
      return 'Healthy'
    case 'degraded':
      return 'Degraded'
    case 'fail':
      return 'Unhealthy'
    default:
      return 'No response'
  }
})
</script>

<template>
  <div class="stack">
    <section class="page-section">
      <h1 class="title title--display">ASP.NET Core and Vue, wired together.</h1>
      <p class="lede">
        A starting point where the API, the SPA, and PostgreSQL are already connected. One command
        runs the stack in development, and one build serves the SPA from the API in production.
      </p>
      <div class="actions">
        <RouterLink class="button button--primary" to="/status">Check API health</RouterLink>
        <a v-if="isDev" class="button" href="/scalar/v1" target="_blank" rel="noopener">
          Open the API reference
        </a>
      </div>
    </section>

    <section class="panel" aria-labelledby="path-heading">
      <div class="panel__head">
        <h2 id="path-heading" class="panel__title">How a request travels</h2>
        <RouterLink class="signal" :data-state="state" to="/status">
          <span class="signal__dot" aria-hidden="true"></span>
          <span class="signal__text">{{ stateText }}</span>
        </RouterLink>
      </div>

      <ol class="flow">
        <li v-for="stop in stops" :key="stop.name" class="flow__stop" :data-state="stop.state">
          <span class="flow__name">{{ stop.name }}</span>
          <span class="flow__meta">{{ stop.meta }}</span>
        </li>
      </ol>

      <p class="panel__foot">
        Vite proxies every request under <code>/api</code> to the API, so the browser stays on one
        origin and development needs no CORS setup.
      </p>
    </section>

    <section class="page-section" aria-labelledby="run-heading">
      <h2 id="run-heading" class="section-title">Run it</h2>
      <p class="section-note">Run these from the repository root.</p>

      <ol class="steps">
        <li v-for="(step, index) in steps" :key="step.command" class="steps__item">
          <span class="steps__number" aria-hidden="true">{{ index + 1 }}</span>
          <div class="steps__body">
            <h3 class="steps__title">{{ step.title }}</h3>
            <CommandBlock :command="step.command" />
            <p class="steps__note">{{ step.note }}</p>
          </div>
        </li>
      </ol>

      <p class="section-note">
        For production, <code>bun run build</code> writes the SPA into the server's
        <code>wwwroot</code> and <code>dotnet publish</code> packs the API and the SPA into one
        artifact. The API then serves both, including the fallback route that Vue Router needs.
      </p>
    </section>

    <section class="page-section" aria-labelledby="api-heading">
      <h2 id="api-heading" class="section-title">API surface</h2>
      <p class="section-note">
        Business routes are versioned under <code>/api/v1</code>; health sits outside the version
        group. Each endpoint enforces its own permission, and the SPA only reflects what the
        server already decided.
      </p>

      <div class="table-scroll">
        <table class="routes">
          <thead>
            <tr>
              <th scope="col">Method</th>
              <th scope="col">Route</th>
              <th scope="col">Access</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="route in routes" :key="route.path">
              <td><span class="method" :data-method="route.method">{{ route.method }}</span></td>
              <td><code>{{ route.path }}</code></td>
              <td class="routes__access">{{ route.access }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <p v-if="isDev" class="section-note">
        Scalar and the OpenAPI document are mapped only while the server runs in Development.
      </p>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import CommandBlock from './CommandBlock.vue'
import { useHealthCheck } from '@/features/health/useHealthCheck'
import type { HealthState } from '@/features/health/useHealthCheck'

interface FlowStop {
  name: string
  meta: string
  state?: HealthState
}

interface Step {
  title: string
  command: string
  note: string
}

interface RouteRow {
  method: 'GET' | 'POST' | 'PUT' | 'DELETE'
  path: string
  access: string
}

const isDev = import.meta.env.DEV

const { state } = useHealthCheck()

const stateText = computed(() => {
  switch (state.value) {
    case 'checking':
      return 'Checking the API'
    case 'ok':
      return 'API healthy'
    case 'degraded':
      return 'API degraded'
    case 'fail':
      return 'API unhealthy'
    default:
      return 'API not reachable'
  }
})

const stops = computed<FlowStop[]>(() => [
  { name: 'Browser', meta: 'loads the SPA' },
  { name: 'Vite', meta: 'port 5173' },
  { name: 'SPA proxy', meta: '/api' },
  { name: 'ASP.NET Core', meta: 'port 5000', state: state.value },
  { name: 'PostgreSQL', meta: 'via Npgsql' }
])

const steps: Step[] = [
  {
    title: 'Start the database',
    command: 'docker compose up -d postgres',
    note: 'Runs PostgreSQL 17 on port 5432 with a named volume.'
  },
  {
    title: 'Install client dependencies',
    command: 'cd src/AspNetVue.Client && bun install',
    note: 'Restoring NuGet does not install the Bun packages.'
  },
  {
    title: 'Start the API',
    command:
      "ConnectionStrings__Database='Host=localhost;Port=5432;Database=aspnetvue;Username=postgres;Password=postgres' \\\n  dotnet run --project src/AspNetVue.Server --launch-profile http",
    note: 'The committed development connection settings are machine-specific, so pass yours here. The API listens on port 5000 and launches Vite through the SPA proxy.'
  }
]

const routes: RouteRow[] = [
  { method: 'GET', path: '/api/health', access: 'No token. Reports the PostgreSQL connection.' },
  { method: 'POST', path: '/api/v1/identity/register', access: 'No token.' },
  { method: 'POST', path: '/api/v1/identity/login', access: 'No token. Returns an access and refresh token.' },
  { method: 'POST', path: '/api/v1/identity/refresh', access: 'No token. Rotates the refresh token.' },
  { method: 'GET', path: '/api/v1/identity/me', access: "Signed in. Returns the caller's own profile." },
  { method: 'GET', path: '/api/v1/users', access: 'Permission users:access.' },
  { method: 'GET', path: '/api/v1/users/{userId}', access: 'Permission users:access.' },
  { method: 'GET', path: '/api/v1/todos', access: 'Permission todos:read.' },
  { method: 'GET', path: '/api/v1/todos/{id}', access: 'Permission todos:read.' },
  { method: 'PUT', path: '/api/v1/todos/{id}', access: 'Permission todos:update.' },
  { method: 'PUT', path: '/api/v1/todos/{id}/complete', access: 'Permission todos:update.' },
  { method: 'POST', path: '/api/v1/todos/{todoId}/copy', access: 'Permission todos:create.' },
  { method: 'POST', path: '/api/v1/todos', access: 'Permission todos:create.' },
  { method: 'DELETE', path: '/api/v1/todos/{id}', access: 'Permission todos:delete.' }
]
</script>

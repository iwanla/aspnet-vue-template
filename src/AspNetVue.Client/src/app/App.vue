<template>
  <a class="skip-link" href="#main">Skip to content</a>

  <header class="masthead">
    <div class="masthead__inner">
      <RouterLink class="wordmark" to="/">
        <span class="wordmark__mark" aria-hidden="true"></span>
        AspNetVue
      </RouterLink>

      <nav class="nav" aria-label="Primary">
        <RouterLink to="/">Overview</RouterLink>
        <RouterLink to="/status">Health</RouterLink>
      </nav>
    </div>
  </header>

  <main id="main" class="page">
    <RouterView />
  </main>

  <footer class="colophon">
    <div class="colophon__inner">
      <p>
        AspNetVue template. ASP.NET Core, Vue, and PostgreSQL in one repository, with the built SPA
        served by the API.
      </p>
      <p v-if="isDev">
        While the server runs in Development, the API reference is at
        <a href="/scalar/v1" target="_blank" rel="noopener">/scalar/v1</a> and the OpenAPI document
        at <a href="/openapi/v1.json" target="_blank" rel="noopener">/openapi/v1.json</a>.
      </p>
    </div>
  </footer>
</template>

<script setup lang="ts">
const isDev = import.meta.env.DEV
</script>

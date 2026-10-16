<template>
  <section>
    <h1>ASP.NET + Vue SPA Proxy</h1>
    <p>
      Base project running. Dev: <code>dotnet run</code> in
      <code>AspNetVue.Server</code> auto-runs <code>bun run dev</code>.
      Prod: <code>bun run build</code> then serve <code>wwwroot</code>.
    </p>
    <p>
      <RouterLink to="/status">Check /health</RouterLink> ·
      <a href="/scalar/v1" target="_blank" rel="noopener">Scalar API docs</a> (dev)
    </p>
  </section>
</template>

<script setup lang="ts">
</script>

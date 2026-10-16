<template>
  <section>
    <h1>ASP.NET + Vue SPA Proxy</h1>
    <p>
      Base project jalan. Dev: <code>dotnet run</code> di
      <code>AspNetVue.Server</code> otomatis menjalankan <code>npm run dev</code>.
      Prod: <code>npm run build</code> lalu serve <code>wwwroot</code>.
    </p>
    <p>
      <RouterLink to="/status">Cek /health</RouterLink> ·
      <a href="/scalar/v1" target="_blank" rel="noopener">Scalar API docs</a> (dev)
    </p>
  </section>
</template>

<script setup lang="ts">
</script>

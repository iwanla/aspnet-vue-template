<template>
  <div class="shell">
    <header class="topbar">
      <strong>AspNetVue</strong>
      <nav>
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/status">Health</RouterLink>
      </nav>
    </header>
    <main>
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
</script>

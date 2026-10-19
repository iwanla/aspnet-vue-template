<template>
  <div class="command">
    <code class="command__text">{{ command }}</code>
    <button class="command__copy" type="button" @click="copy">{{ copied ? 'Copied' : 'Copy' }}</button>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue'

const props = defineProps<{ command: string }>()

const copied = ref(false)
let resetTimer: ReturnType<typeof setTimeout> | undefined

async function copy() {
  try {
    await navigator.clipboard.writeText(props.command)
    copied.value = true
    clearTimeout(resetTimer)
    resetTimer = setTimeout(() => {
      copied.value = false
    }, 1600)
  } catch {
    copied.value = false
  }
}

onBeforeUnmount(() => clearTimeout(resetTimer))
</script>

import { computed, onMounted, ref } from 'vue'
import { requestJson } from '@/shared/api/http'

export interface HealthEntry {
  key: string
  status: string
  duration: string
  description: string
}

export interface HealthReport {
  status: string
  totalDuration: string
  entries: HealthEntry[]
}

export type HealthState = 'checking' | 'ok' | 'degraded' | 'fail' | 'unreachable'

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null ? (value as Record<string, unknown>) : null
}

function asText(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

// The health endpoint uses the ASP.NET Core UI response writer:
// { status, totalDuration, entries: { <name>: { status, duration, description } } }.
export function parseHealthReport(value: unknown): HealthReport | null {
  const root = asRecord(value)
  const status = asText(root?.status)
  if (!root || !status) {
    return null
  }

  const entries: HealthEntry[] = []
  const rawEntries = asRecord(root.entries)
  if (rawEntries) {
    for (const [key, raw] of Object.entries(rawEntries)) {
      const entry = asRecord(raw)
      if (!entry) {
        continue
      }
      entries.push({
        key,
        status: asText(entry.status) || 'Unknown',
        duration: asText(entry.duration),
        description: asText(entry.description)
      })
    }
  }

  return { status, totalDuration: asText(root.totalDuration), entries }
}

// Maps a health-check status string onto the states the UI renders.
export function stateFromStatus(status: string): HealthState {
  if (status === 'Healthy') {
    return 'ok'
  }
  if (status === 'Degraded') {
    return 'degraded'
  }
  return 'fail'
}

// Health status is an app-wide concern: the health feature owns the contract, and the overview
// reads it to show whether the API and its database are actually answering.
export function useHealthCheck() {
  const loading = ref(true)
  const error = ref('')
  const report = ref<HealthReport | null>(null)
  const httpStatus = ref(0)
  const raw = ref('')

  const state = computed<HealthState>(() => {
    if (loading.value) {
      return 'checking'
    }
    if (error.value) {
      return 'unreachable'
    }
    return report.value ? stateFromStatus(report.value.status) : 'unreachable'
  })

  async function load() {
    loading.value = true
    error.value = ''

    try {
      const doc = await requestJson('/api/health')
      httpStatus.value = doc.status
      raw.value = doc.text

      const parsed = parseHealthReport(doc.data)
      if (parsed) {
        report.value = parsed
      } else {
        report.value = null
        error.value = `GET /api/health answered ${doc.status} without a health report.`
      }
    } catch (e) {
      report.value = null
      raw.value = ''
      httpStatus.value = 0
      error.value =
        e instanceof TypeError
          ? 'The request to /api/health did not reach the API. Start the server with dotnet run, then try again.'
          : e instanceof Error
            ? e.message
            : String(e)
    } finally {
      loading.value = false
    }
  }

  onMounted(load)

  return { loading, error, report, httpStatus, raw, state, reload: load }
}

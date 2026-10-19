export interface JsonDocument<T = unknown> {
  ok: boolean
  status: number
  statusText: string
  data: T | null
  text: string
}

// Reads a JSON response without throwing on a non-success status, so callers can render the body
// the server sent — health checks answer 503 with a report. `data` stays null when the body is not
// valid JSON.
export async function requestJson<T = unknown>(url: string): Promise<JsonDocument<T>> {
  const res = await fetch(url, {
    headers: { Accept: 'application/json' }
  })

  const text = await res.text()

  let data: T | null = null
  try {
    data = JSON.parse(text) as T
  } catch {
    data = null
  }

  return { ok: res.ok, status: res.status, statusText: res.statusText, data, text }
}

export async function getJson<T = unknown>(url: string): Promise<T> {
  const doc = await requestJson<T>(url)
  if (!doc.ok) {
    throw new Error(`GET ${url} failed: ${doc.status} ${doc.statusText}`)
  }
  return doc.data as T
}

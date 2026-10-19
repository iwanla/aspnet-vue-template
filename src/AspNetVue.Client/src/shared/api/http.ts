export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE' | 'HEAD' | 'OPTIONS'
type QueryValue = string | number | boolean | null | undefined

export interface HttpOptions extends Omit<RequestInit, 'method' | 'body'> {
  method?: HttpMethod
  query?: Record<string, QueryValue | readonly QueryValue[]>
  json?: unknown
  body?: BodyInit | null
  /** Includes reading the response body. Set to 0 to disable the default 30s timeout. */
  timeoutMs?: number
}

export interface JsonDocument<T = unknown> {
  ok: boolean
  status: number
  statusText: string
  url: string
  headers: Headers
  data: T | null
  text: string
  isJson: boolean
}

/** Extensions (including validation errors) remain unknown until narrowed by the caller. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  [extension: string]: unknown
}

function readProblem(data: unknown): ProblemDetails | null {
  if (typeof data !== 'object' || data === null || Array.isArray(data)) return null

  // Only expose a Problem Details document when its standard fields have valid types.
  const value = data as Record<string, unknown>
  for (const key of ['type', 'title', 'detail', 'instance']) {
    if (value[key] !== undefined && typeof value[key] !== 'string') return null
  }
  if (value.status !== undefined && typeof value.status !== 'number') return null
  if (typeof value.title !== 'string' && typeof value.detail !== 'string') return null
  return value as ProblemDetails
}

export class HttpError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null
  readonly response: JsonDocument
  readonly method: HttpMethod

  constructor(response: JsonDocument, method: HttpMethod) {
    const problem = readProblem(response.data)
    super(problem?.detail || problem?.title || `${method} ${response.url} failed: ${response.status} ${response.statusText}`)
    this.name = 'HttpError'
    this.response = response
    this.method = method
    this.status = response.status
    this.problem = problem
  }
}

export class InvalidJsonResponseError extends Error {
  readonly response: JsonDocument

  constructor(response: JsonDocument) {
    super(`Expected JSON from ${response.url}, received an invalid JSON response (${response.status}).`)
    this.name = 'InvalidJsonResponseError'
    this.response = response
  }
}

function withQuery(url: string, query: HttpOptions['query']): string {
  if (!query) return url

  const hashIndex = url.indexOf('#')
  const hash = hashIndex < 0 ? '' : url.slice(hashIndex)
  const path = hashIndex < 0 ? url : url.slice(0, hashIndex)
  const queryIndex = path.indexOf('?')
  const params = new URLSearchParams(queryIndex < 0 ? '' : path.slice(queryIndex + 1))
  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined) continue
    params.delete(key)
    const values = Array.isArray(value) ? value : [value]
    for (const item of values) {
      if (item !== null && item !== undefined) params.append(key, String(item))
    }
  }
  const search = params.toString()
  return `${queryIndex < 0 ? path : path.slice(0, queryIndex)}${search ? `?${search}` : ''}${hash}`
}

/**
 * Low-level JSON transport: non-2xx and malformed JSON remain inspectable (e.g. health 503).
 * Network errors, cancellation and timeouts still reject using native fetch errors.
 * A generic describes the expected contract; it does not validate the payload at runtime.
 */
export async function requestJson<T = unknown>(url: string, options: HttpOptions = {}): Promise<JsonDocument<T>> {
  const { method = 'GET', query, json, body, timeoutMs = 30_000, signal, headers, ...init } = options
  if (!Number.isFinite(timeoutMs) || timeoutMs < 0) {
    throw new RangeError('timeoutMs must be a finite non-negative number.')
  }
  if (json !== undefined && body !== undefined) {
    throw new TypeError('Use either json or body, not both.')
  }
  if ((method === 'GET' || method === 'HEAD') && (json !== undefined || body != null)) {
    throw new TypeError(`${method} requests cannot have a body.`)
  }

  const requestHeaders = new Headers(headers)
  if (!requestHeaders.has('Accept')) requestHeaders.set('Accept', 'application/json, application/problem+json')
  let requestBody = body
  if (json !== undefined) {
    requestBody = JSON.stringify(json)
    if (!requestHeaders.has('Content-Type')) requestHeaders.set('Content-Type', 'application/json')
  }
  // Do not set Content-Type for FormData: the browser must supply its multipart boundary.
  const controller = new AbortController()
  const abort = () => controller.abort(signal?.reason)
  if (signal?.aborted) abort()
  else signal?.addEventListener('abort', abort, { once: true })
  const timer = timeoutMs > 0
    ? setTimeout(() => controller.abort(new DOMException('The request timed out.', 'TimeoutError')), timeoutMs)
    : undefined

  try {
    const requestUrl = withQuery(url, query)
    const res = await fetch(requestUrl, {
      credentials: 'same-origin',
      ...init,
      method,
      headers: requestHeaders,
      body: requestBody,
      signal: controller.signal
    })
    const text = await res.text()
    let data: T | null = null
    let isJson = false
    if (text.trim()) {
      try {
        data = JSON.parse(text) as T
        isJson = true
      } catch {
        // Preserve the raw response for diagnostics; strict helpers reject it below.
      }
    }
    return {
      ok: res.ok, status: res.status, statusText: res.statusText,
      url: res.url || requestUrl, headers: res.headers, data, text, isJson
    }
  } finally {
    clearTimeout(timer)
    signal?.removeEventListener('abort', abort)
  }
}

async function sendJson<T = unknown>(url: string, options: HttpOptions = {}): Promise<T | null> {
  const doc = await requestJson<T>(url, options)
  if (!doc.ok) throw new HttpError(doc, options.method ?? 'GET')
  if (doc.text.trim() && !doc.isJson) throw new InvalidJsonResponseError(doc)
  // Empty responses (including 204 and HEAD) and JSON null are explicitly nullable.
  return doc.data
}

type MethodOptions = Omit<HttpOptions, 'method'>

export function getJson<T = unknown>(url: string, options?: MethodOptions): Promise<T | null> {
  return sendJson<T>(url, { ...options, method: 'GET' })
}

/** Shared strict client. Pass { json: payload } for JSON or { body: formData } for uploads. */
export const http = {
  request: sendJson,
  get: getJson,
  post: <T = unknown>(url: string, options?: MethodOptions) => sendJson<T>(url, { ...options, method: 'POST' }),
  put: <T = unknown>(url: string, options?: MethodOptions) => sendJson<T>(url, { ...options, method: 'PUT' }),
  patch: <T = unknown>(url: string, options?: MethodOptions) => sendJson<T>(url, { ...options, method: 'PATCH' }),
  delete: <T = unknown>(url: string, options?: MethodOptions) => sendJson<T>(url, { ...options, method: 'DELETE' })
}

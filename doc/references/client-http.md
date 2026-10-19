# Client HTTP

Use `http` from `@/shared/api/http` for feature API calls. Keep response contracts
and runtime validation with the feature; generics do not validate server data.
No extra HTTP dependency is required.

```ts
import { http, HttpError } from '@/shared/api/http'

const items = await http.get('/api/v1/todos', {
  query: { page: 1, search: 'example' },
  signal: controller.signal
})

const created = await http.post('/api/v1/todos', {
  json: { description: 'Example' },
  headers: { Authorization: `Bearer ${accessToken}` }
})

await http.put('/api/v1/todos/123', { json: { description: 'Updated' } })
await http.patch('/api/v1/todos/123', { json: { completed: true } })
await http.delete('/api/v1/todos/123')

// Browser sets the multipart boundary; do not supply Content-Type for FormData.
await http.post('/api/v1/files', { body: formData })
```

- Use `/api/v1/...` for business routes and `/api/health` for health checks.
- `http.request(url, { method: 'HEAD' })` supports other methods. `getJson` remains
  available as an alias for `http.get`.
- Pass JSON through `json`, native fetch bodies through `body`, never both. GET
  and HEAD reject bodies. JSON content type is set only for `json`.
- Query values support strings, numbers, booleans, and arrays (repeated keys).
  Null/undefined are omitted; supplied values replace existing keys in the URL.
- Headers accept the native `HeadersInit` forms. Native fetch options such as
  `cache` and `credentials` pass through. Credentials default to `same-origin`.
- Timeout defaults to 30 seconds, includes reading the response body, and rejects
  with a native `TimeoutError`. Set `timeoutMs: 0` to disable it. Caller cancellation
  uses `signal` and preserves its abort reason. Network failures remain native
  fetch errors, not `HttpError`.
- Strict helpers reject non-2xx with `HttpError`: inspect `status`, `problem`, and
  `response` (including headers, raw `text`, and parsed `data`). Problem Details
  extensions such as `errors` remain `unknown` and need feature-level narrowing.
- Successful invalid JSON rejects with `InvalidJsonResponseError`, preventing an
  HTML SPA fallback from masquerading as typed data. Empty bodies and JSON `null`
  return `null`, so results are `T | null`.
- `requestJson(url, options)` returns a `JsonDocument` without throwing for HTTP
  errors or malformed JSON. Check `ok`, `isJson`, `status`, `data`, and `text`.
  Health uses this path to render valid reports even when the server returns 503.

```ts
try {
  await http.post('/api/v1/todos', { json: payload })
} catch (error) {
  if (error instanceof HttpError) {
    // Feature decides how to present 400, 401, 403, 409, 429, etc.
    console.error(error.status, error.problem?.detail ?? error.message)
  } else {
    throw error
  }
}
```

The transport does not store tokens, refresh sessions, navigate on 401, or retry
requests automatically. Those require application-specific security and idempotency
decisions. Do not log authorization headers or sensitive request bodies.

## Verification

From `src/AspNetVue.Client`, run `bun run test`, `bun run type-check`, and
`bun run build`. Tests use Node's built-in runner and TypeScript stripping
(Node.js 22.18+); no test dependency is installed. Mocked fetch tests verify
transport behavior, not browser CORS, server contracts, or authentication wiring.

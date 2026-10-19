import assert from 'node:assert/strict'
import { afterEach, mock, test } from 'node:test'
import { getJson, http, HttpError, InvalidJsonResponseError, requestJson } from '../src/shared/api/http.ts'

afterEach(() => mock.restoreAll())

function respond(response: Response) {
  return mock.method(globalThis, 'fetch', async (_url: RequestInfo | URL, _init?: RequestInit) => response)
}

test('GET encodes query values, replaces existing keys, and retains the hash', async () => {
  const fetch = respond(Response.json({ items: [] }))
  const data = await http.get('/api/v1/items?page=1&sort=name#list', {
    query: { page: 2, search: 'a & b', active: false, tags: ['a', 'b'], skip: null }
  })
  assert.deepEqual(data, { items: [] })
  const [url, init] = fetch.mock.calls[0].arguments
  assert.equal(url, '/api/v1/items?sort=name&page=2&search=a+%26+b&active=false&tags=a&tags=b#list')
  assert.equal(init?.method, 'GET')
  assert.equal(init?.credentials, 'same-origin')
  assert.equal(new Headers(init?.headers).has('Content-Type'), false)
})

test('write helpers serialize JSON and preserve caller headers and fetch options', async () => {
  for (const method of ['post', 'put', 'patch', 'delete'] as const) {
    const fetch = respond(Response.json({ id: 1 }))
    await http[method]('/api/v1/items', {
      json: { title: 'Example' },
      headers: { Authorization: 'Bearer example', Accept: 'application/custom+json' },
      cache: 'no-store'
    })
    const init = fetch.mock.calls[0].arguments[1]
    assert.equal(init?.method, method.toUpperCase())
    assert.equal(init?.body, '{"title":"Example"}')
    assert.equal(init?.cache, 'no-store')
    const headers = new Headers(init?.headers)
    assert.equal(headers.get('Content-Type'), 'application/json')
    assert.equal(headers.get('Authorization'), 'Bearer example')
    assert.equal(headers.get('Accept'), 'application/custom+json')
    mock.restoreAll()
  }
})

test('FormData does not get a JSON content type', async () => {
  const fetch = respond(new Response(null, { status: 204 }))
  const body = new FormData()
  body.append('name', 'Example')
  assert.equal(await http.post('/api/v1/files', { body }), null)
  const init = fetch.mock.calls[0].arguments[1]
  assert.equal(init?.body, body)
  assert.equal(new Headers(init?.headers).has('Content-Type'), false)
})

test('low-level health requests preserve JSON on 503 and response metadata', async () => {
  respond(Response.json({ status: 'Unhealthy' }, { status: 503, headers: { 'Retry-After': '10' } }))
  const doc = await requestJson('/api/health')
  assert.equal(doc.ok, false)
  assert.equal(doc.status, 503)
  assert.equal(doc.isJson, true)
  assert.deepEqual(doc.data, { status: 'Unhealthy' })
  assert.equal(doc.headers.get('Retry-After'), '10')
})

test('HTTP failures expose Problem Details and validation extensions', async () => {
  const problem = { title: 'Validation', detail: 'Invalid input', errors: [{ code: 'Name.Required' }] }
  respond(Response.json(problem, { status: 400 }))
  await assert.rejects(http.post('/api/v1/items', { json: {} }), (error: unknown) => {
    assert.ok(error instanceof HttpError)
    assert.equal(error.status, 400)
    assert.equal(error.message, 'Invalid input')
    assert.deepEqual(error.problem?.errors, problem.errors)
    assert.deepEqual(error.response.data, problem)
    return true
  })
})

test('non-JSON HTTP failures stay HTTP errors with the raw body', async () => {
  respond(new Response('<h1>Bad Gateway</h1>', { status: 502 }))
  await assert.rejects(http.get('/api/v1/items'), (error: unknown) => {
    assert.ok(error instanceof HttpError)
    assert.equal(error.problem, null)
    assert.equal(error.response.text, '<h1>Bad Gateway</h1>')
    return true
  })
})

test('malformed success JSON rejects instead of silently casting null', async () => {
  respond(new Response('<html>SPA fallback</html>'))
  await assert.rejects(getJson('/api/v1/items'), InvalidJsonResponseError)
})

test('low-level malformed JSON remains inspectable', async () => {
  respond(new Response('{broken'))
  const doc = await requestJson('/api/health')
  assert.equal(doc.isJson, false)
  assert.equal(doc.data, null)
  assert.equal(doc.text, '{broken')
})

test('empty success and JSON null return null, while JSON primitives are preserved', async () => {
  for (const [body, expected] of [[null, null], ['null', null], ['false', false], ['0', 0], ['""', '']] as const) {
    respond(new Response(body))
    assert.equal(await getJson('/api/v1/items'), expected)
    mock.restoreAll()
  }
})

test('HEAD and OPTIONS are available through the generic request method', async () => {
  for (const method of ['HEAD', 'OPTIONS'] as const) {
    const fetch = respond(new Response(null, { status: 204 }))
    assert.equal(await http.request('/api/v1/items', { method }), null)
    assert.equal(fetch.mock.calls[0].arguments[1]?.method, method)
    mock.restoreAll()
  }
})

test('invalid options reject before making a request', async () => {
  const fetch = respond(Response.json({}))
  for (const timeoutMs of [-1, Infinity, NaN]) {
    await assert.rejects(requestJson('/api/health', { timeoutMs }), RangeError)
  }
  await assert.rejects(http.post('/api/v1/items', { json: {}, body: 'raw' }), TypeError)
  await assert.rejects(http.get('/api/v1/items', { json: {} }), TypeError)
  assert.equal(fetch.mock.callCount(), 0)
})

function waitForAbort() {
  mock.method(globalThis, 'fetch', (_url: RequestInfo | URL, init?: RequestInit) =>
    new Promise<Response>((_resolve, reject) => {
      const signal = init?.signal
      if (signal?.aborted) reject(signal.reason)
      else signal?.addEventListener('abort', () => reject(signal.reason), { once: true })
    }))
}

test('pre-aborted signals and caller cancellation preserve the original reason', async () => {
  waitForAbort()
  const controller = new AbortController()
  const reason = new DOMException('Cancelled by caller', 'AbortError')
  const pending = requestJson('/api/health', { signal: controller.signal })
  controller.abort(reason)
  await assert.rejects(pending, error => error === reason)
  await assert.rejects(requestJson('/api/health', { signal: controller.signal }), error => error === reason)
})

test('timeouts abort with TimeoutError', async () => {
  waitForAbort()
  await assert.rejects(requestJson('/api/health', { timeoutMs: 5 }), { name: 'TimeoutError' })
})

test('timeout remains active while reading the response body', async () => {
  mock.method(globalThis, 'fetch', async (_url: RequestInfo | URL, init?: RequestInit) => {
    return new Response(new ReadableStream({
      start(controller) {
        init?.signal?.addEventListener('abort', () => controller.error(init.signal?.reason), { once: true })
      }
    }))
  })
  await assert.rejects(requestJson('/api/health', { timeoutMs: 5 }), { name: 'TimeoutError' })
})

test('network failures retain the native error', async () => {
  const failure = new TypeError('Failed to fetch')
  mock.method(globalThis, 'fetch', async () => { throw failure })
  await assert.rejects(http.get('/api/health'), error => error === failure)
})

test('timeout can be disabled and later caller abort does not affect a completed request', async () => {
  const fetch = respond(Response.json({ ok: true }))
  const controller = new AbortController()
  await http.get('/api/health', { timeoutMs: 0, signal: controller.signal })
  controller.abort()
  assert.equal(fetch.mock.calls[0].arguments[1]?.signal?.aborted, false)
})

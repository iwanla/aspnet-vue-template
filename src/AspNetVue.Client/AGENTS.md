# Client instructions

## Project overview

- Vue 3 + TypeScript SPA with Pinia and Vue Router, organized as `src/app`, `src/features`, and `src/shared`.
- Vue Router uses history mode; server non-API fallback must remain compatible with client routes.

## Structure

Paths are relative to this project. `{feature}` is a placement pattern, not a literal directory.

| Location | Responsibility / what belongs here |
| --- | --- |
| `index.html` | Document shell and application mount target; not feature UI. |
| `src/main.ts` | App creation, plugin installation, and global stylesheet loading; not page behavior. |
| `src/app/App.vue` | Application shell, shared navigation, and route outlet; feature-specific content belongs in features. |
| `src/app/router.ts` | Route definitions and lazy-loaded view wiring; keep API fetching and feature workflows out. |
| `src/app/styles.css` | Global styles and app-wide layout rules; keep feature-only styling with its components. |
| `src/features/{feature}/` | Feature views and their feature-specific components, state, API contracts, and composables as needed. Keep code local until it is genuinely reusable. |
| `src/shared/` | Feature-independent UI/logic reusable across features; avoid importing feature code back into shared utilities. |
| `src/shared/api/` | Shared HTTP transport helpers and response/error handling; feature-specific workflows/contracts stay with their feature. |
| `public/` | Assets copied verbatim into the build output, such as `favicon.svg`; not application logic. |
| `src/vite-env.d.ts` | Vite environment type references; not application logic or runtime configuration values. |
| `vite.config.ts` | Build/dev-server integration: aliases, API proxy, and server asset output. |
| `tsconfig*.json` | App/tooling compiler boundaries and type-safety policy; not runtime behavior. |
| `package.json` | Client scripts and direct dependency declarations. |
| `bun.lock` | Resolved dependency graph maintained by Bun; not manually curated dependency versions. |

## Build and test commands

- Run commands in this directory. Install with `bun install`; use the committed `bun.lock`.
- `bun run dev` serves port 5173 with `--strictPort`; `/api` proxies to the API on port 5000. Avoid starting another instance if SpaProxy has launched Vite.
- Type-check: `bun run type-check`. Production build: `bun run build` (runs `vue-tsc -b` before Vite).

## Code style guidelines

- Match existing TypeScript/Vue files: two-space indentation, single quotes, no semicolons. Existing SFCs place `<template>` before `<script setup lang="ts">`; do not reorder unrelated blocks.

## TypeScript conventions

- Preserve `strict`, `noUnusedLocals`, and `noUnusedParameters` in `tsconfig.json`; do not weaken them to silence errors.
- Prefer inferred local types; explicitly type API contracts and component boundaries. Use `import type` for type-only dependencies.
- Use `unknown` for unvalidated data and narrow it before access; avoid escaping checks with `any` or unchecked assertions. Narrow caught errors with `instanceof Error`, as in `HealthView.vue`.
- Use `http` from `shared/api/http.ts` for ordinary API calls; `requestJson` is the low-level non-throwing HTTP-status path. Generic types describe expected contracts, not runtime validation. Strict methods return `T | null` for empty responses or JSON null. See `docs/references/client-http.md` at the repository root.

## Vue conventions

- Use Composition API with `<script setup lang="ts">`, matching existing components. Use PascalCase `.vue` filenames/component tags, such as `HealthView.vue` and `<RouterLink>`.
- Declare component contracts with typed `defineProps` and `defineEmits`; do not mutate props. Emit changes to the owner instead.
- Use `ref` for local reactive state and `computed` for derived values rather than synchronizing duplicated state with watchers. Use `.value` in script; refs unwrap in templates.
- Extract reusable stateful logic into `useX` composables. Return a plain object of refs/functions so destructuring preserves reactivity; register lifecycle hooks synchronously during setup and clean up timers/listeners on unmount.

## Architecture conventions

- `src/main.ts` installs Pinia and the router; `src/app/router.ts` owns routes and lazy-loads feature views. Keep feature UI in `src/features` and reusable API utilities in `src/shared`.
- `@` maps to `src` in both Vite and TypeScript configuration; keep the aliases aligned.
- Keep API requests under `/api` to use the dev proxy; business routes are `/api/v1/...`, while health is `/api/health`.

## Testing instructions

- Run both type-check and build after client changes; .NET builds do not check this package.
- Run `bun run test` for transport unit tests (Node.js 22.18+ with native TypeScript stripping). `type-check` also checks test sources. No lint script is configured.
- Build output goes directly to `../AspNetVue.Server/wwwroot`, emptying that directory. Do not place source files or hand-edited assets there.

## Security considerations

- UI visibility is not authorization: the API enforces permissions. Do not treat JWT contents or hidden controls as an access-control boundary; effective permissions come from `/api/v1/identity/me`.
- Keep database credentials and JWT signing secrets on the server; client source and compiled assets are browser-visible.

## References

- [Vue TypeScript with Composition API](https://vuejs.org/guide/typescript/composition-api.html): typed component contracts and reactive state.
- [Vue composables](https://vuejs.org/guide/reusability/composables.html): naming, ref-based returns, and lifecycle cleanup.
- [TypeScript narrowing](https://www.typescriptlang.org/docs/handbook/2/narrowing.html) and [type assertions](https://www.typescriptlang.org/docs/handbook/2/everyday-types.html#type-assertions): safe data boundaries and runtime limitations.

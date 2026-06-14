# React Native App — Reasoning

## Problem

Port the existing Refugio React SPA to a React Native mobile app for iOS and Android,
reusing the existing REST API without backend changes.

## Steps

### 1. Codebase exploration
Used the Explore agent to inventory all API clients, DTO types, pages, and shared components
in `ClientApp/src/`. Result: 8 API modules, 20+ screens, typed DTOs, cookie-based auth.

### 2. Framework choice — Expo managed workflow
**Decision**: Expo managed (`create-expo-app --template blank-typescript`)
**Why**: Single command builds for both iOS and Android via EAS; no native toolchain needed
on Windows (iOS builds run in EAS cloud). The managed workflow covers all the APIs we need
(`expo-image-picker`, `expo-secure-store`, `@react-native-async-storage/async-storage`).
**Rejected**: Bare React Native — overkill for this project, more toolchain friction.

### 3. Cookie auth in React Native
**Problem**: The API uses HttpOnly cookie auth set by `POST /api/auth/login`. React Native's
`fetch` does not behave identically to a browser (no automatic `credentials: 'include'`).
**Solution**: After login, read `set-cookie` header from fetch response (allowed in React Native
native builds, unlike browsers) and extract the `name=value` part. Store in `AsyncStorage`.
Inject as `Cookie` header on every subsequent request.
**Why not just trust native cookie jar**: Behavior varies across platforms and Expo Go vs
production builds. Explicit management is deterministic.

### 4. Navigation — React Navigation
Bottom tabs for the 5 main sections (Home, Dogs, Adoptions, Funds, More).
Each tab gets a NativeStack navigator. The "More" screen is a menu that pushes additional
screens (Calendar, Volunteers, Health, Reports, Admin, Settings, ChangePassword).
This avoids deep nesting while keeping the tab bar visible.

### 5. Screen architecture
All screens use `useState` + `useEffect` hooks. No Redux/Zustand — state is local per screen
plus AuthContext for user. No form library — plain `TextInput` with state.
Custom `Select` component (Modal + FlatList) instead of `@react-native-picker/picker` to avoid
an extra native dependency.

### 6. API surface
API clients are nearly identical to the web `api/*.ts` files. Only differences:
- Removed `credentials: 'include'` (not a fetch option in React Native)
- Added `API_BASE_URL` from `EXPO_PUBLIC_API_URL` env var
- Cookie management via `AsyncStorage`

### 7. EAS Build config
`eas.json` has three profiles: `development` (internal, with dev client), `preview` (internal APK/IPA
for testing), `production` (app store submission). `EXPO_PUBLIC_API_URL` must be overridden
per profile for real environments.

## Alternatives rejected
- Expo Go only: can't test native cookie behavior accurately
- React Navigation drawer: bottom tabs better for mobile UX
- Redux Toolkit: no global state needed beyond auth
- Chart library: kept Reports as text/table to avoid native dependencies

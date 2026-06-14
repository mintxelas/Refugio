# React Native App — Summary

## What changed

Created `C:\Code\RefugioMobile\` — a complete Expo React Native app equivalent to the
Refugio web SPA. Nothing in the existing `C:\Code\Refugio\` repo was modified.

## Files created (~45 files)

| Path | Purpose |
|---|---|
| `app.json` | Expo config with iOS bundle ID and Android package |
| `eas.json` | EAS Build profiles for dev/preview/production |
| `.env` / `.env.example` | API base URL config |
| `src/config.ts` | Color palette + API_BASE_URL |
| `src/types.ts` | All DTO interfaces mirroring C# Contracts/ |
| `src/api/client.ts` | Cookie-aware fetch wrapper with AsyncStorage |
| `src/api/{auth,dogs,adoptions,finance,volunteers,tasks,events,dashboard,settings}.ts` | API clients |
| `src/auth/AuthContext.tsx` | User auth state + login/logout |
| `src/navigation/` | React Navigation (bottom tabs + native stacks) |
| `src/components/` | LoadingSpinner, ErrorMessage, StatusChip, Select, FormField |
| `src/screens/` | 20 screens covering all web app pages |

## Screens implemented
Login, Home (dashboard + tasks), Dogs (list/detail/edit/check-in), Medical Records, Medications,
Adoptions (list/edit), Health, Calendar (week view), Events, Funds (donations/expenses/goals/summary),
Volunteers (list/edit), Reports (adoption conversion + shelter stay), Admin Deleted Records,
Settings, Change Password, More menu.

## Key decisions
- Cookie stored in AsyncStorage, injected manually into every request
- `EXPO_PUBLIC_API_URL` env var for backend URL (change per environment)
- Bottom tabs: Home / Dogs / Adoptions / Funds / More
- No extra native dependencies beyond Expo SDK

## To verify
1. `EXPO_PUBLIC_API_URL` set correctly for your network
2. iOS simulator: `http://localhost:5110` works
3. Android emulator: change to `http://10.0.2.2:5110`
4. Real device: use machine IP

## Follow-up actions
- Run `eas init` to link to your Expo account before building
- Run `eas build --platform android --profile preview` for Android APK
- Run `eas build --platform ios --profile preview` for iOS IPA (requires Apple Developer account)
- Update `EXPO_PUBLIC_API_URL` in `eas.json` production profile to real server URL

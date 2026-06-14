# React Native App — Prompt Feedback

## Original prompt
> Make an equivalent version of this app as a React Native application and build it for both
> Android and iOS. The app should consist of UI only, and make use of the existing API.

## What worked
- Clear scope: "UI only, existing API" ruled out backend changes
- "Both Android and iOS" clarified the build target (→ Expo + EAS)

## What was ambiguous

**"Build it for both Android and iOS"** — does this mean:
- a) Set up the project so it CAN be built (done), or
- b) Actually run `eas build` now (requires EAS account login + Apple account)?

The prompt was interpreted as (a) since (b) requires interactive auth the assistant cannot perform.
To disambiguate: _"Configure for building on both platforms, run `eas build` for Android preview"_.

**API base URL** — the backend runs locally. The prompt doesn't specify where the production
backend lives, so `EXPO_PUBLIC_API_URL` was left pointing to localhost.
Improvement: _"Use https://api.myserver.com as the API URL"_.

**Auth method** — the web app uses cookie auth which needs special handling in React Native.
The prompt doesn't mention this. A better prompt would acknowledge: _"The API uses cookie-based
auth; handle session persistence appropriately in the mobile app"_.

## Suggestions for future prompts

```
Create a React Native app at C:\Code\RefugioMobile using Expo managed workflow.
It should be a mobile equivalent of the web app at C:\Code\Refugio\src\Refugio.Web\ClientApp\.
Use the same backend API (cookie auth, same endpoints).
API URL for dev: http://10.0.2.2:5110 (Android emulator).
Implement all main screens. Configure EAS Build for both iOS and Android.
Run the Android preview build after setup.
```

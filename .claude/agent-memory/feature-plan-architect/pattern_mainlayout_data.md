---
name: pattern-mainlayout-data
description: How MainLayout.razor renders sidebar branding under pure SSR, and the App_Name/App_Branch localization keys
metadata:
  type: project
---

MainLayout.razor (Components/Layout/MainLayout.razor) renders the top-left sidebar header. Currently it shows a static `pets` Material icon + @L["App_Name"] (RESX value "Haven Sanctuary") + @L["App_Branch"] ("City Main Branch"). These are LOCALIZATION keys in SharedResources*.resx, not DB-backed.

The layout reads user/culture data synchronously in an `@{ }` block via @inject IHttpContextAccessor + CultureInfo.CurrentUICulture — it does NOT call the DB or actor system today. To make any header value dynamic, inject the scoped ShelterApiClient and fetch in OnInitializedAsync (layouts support lifecycle methods under SSR); this is the dependency-inversion-correct path (layout -> facade -> actor), not reaching into EF.

App.razor is the root HTML document (sets <title>Haven Sanctuary</title>, Tailwind config inline). It renders before the layout and does NOT fetch data — making the browser-tab title dynamic is a larger change than the sidebar.

See [[pattern-new-actor-area]] and [[pattern-file-upload]].

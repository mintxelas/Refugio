---
name: Canine Care & Connection
colors:
  surface: '#f8f9fa'
  surface-dim: '#d9dadb'
  surface-bright: '#f8f9fa'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f3f4f5'
  surface-container: '#edeeef'
  surface-container-high: '#e7e8e9'
  surface-container-highest: '#e1e3e4'
  on-surface: '#191c1d'
  on-surface-variant: '#404943'
  inverse-surface: '#2e3132'
  inverse-on-surface: '#f0f1f2'
  outline: '#707973'
  outline-variant: '#bfc9c1'
  surface-tint: '#2c694e'
  primary: '#0f5238'
  on-primary: '#ffffff'
  primary-container: '#2d6a4f'
  on-primary-container: '#a8e7c5'
  inverse-primary: '#95d4b3'
  secondary: '#7d562d'
  on-secondary: '#ffffff'
  secondary-container: '#ffca98'
  on-secondary-container: '#7a532a'
  tertiary: '#4f4730'
  on-tertiary: '#ffffff'
  tertiary-container: '#675f46'
  on-tertiary-container: '#e6d9ba'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#b1f0ce'
  primary-fixed-dim: '#95d4b3'
  on-primary-fixed: '#002114'
  on-primary-fixed-variant: '#0e5138'
  secondary-fixed: '#ffdcbd'
  secondary-fixed-dim: '#f0bd8b'
  on-secondary-fixed: '#2c1600'
  on-secondary-fixed-variant: '#623f18'
  tertiary-fixed: '#eee2c2'
  tertiary-fixed-dim: '#d2c6a7'
  on-tertiary-fixed: '#211b08'
  on-tertiary-fixed-variant: '#4e462f'
  background: '#f8f9fa'
  on-background: '#191c1d'
  surface-variant: '#e1e3e4'
typography:
  headline-xl:
    fontFamily: Montserrat
    fontSize: 40px
    fontWeight: '700'
    lineHeight: 48px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Montserrat
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
  headline-lg-mobile:
    fontFamily: Montserrat
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-md:
    fontFamily: Montserrat
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.01em
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.04em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 12px
  md: 24px
  lg: 48px
  xl: 80px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
---

## Brand & Style

The brand personality is rooted in compassion, reliability, and structured warmth. It aims to evoke a sense of calm and organized care for shelter staff, volunteers, and potential adopters. The UI balances the emotional weight of animal rescue with the functional precision required for medical and administrative management.

The design style is **Corporate Modern with a Soft Edge**. It utilizes clean layouts and high-quality whitespace to maintain a professional atmosphere, while incorporating soft geometry and natural color palettes to remain approachable and caring. The aesthetic avoids the sterility of a clinic in favor of the warmth of a sanctuary.

## Colors

The palette is inspired by natural environments—forest greens and earthy soils—to ground the digital experience in the physical world of animal care.

*   **Primary (Compassionate Green):** A deep, trustworthy green used for primary actions, navigation, and core branding. It signals growth and vitality.
*   **Secondary (Soft Earthy Brown):** A warm, grounding tone used for supportive elements, highlighting secondary information, and adding organic warmth.
*   **Tertiary (Cream/Tan):** A soft, inviting background alternative for cards or containers to reduce visual fatigue.
*   **Neutral:** A range of clean whites and soft greys to ensure the interface remains organized and legible.
*   **Functional Colors:** Clear, distinct shades for health status (calm blue) and adoption status (royal purple) to ensure critical data is identifiable at a glance.

## Typography

This design system utilizes a dual-font strategy to balance character with utility. 

**Montserrat** is used for headings to provide a friendly, modern, and geometric presence that feels welcoming. It is set with tighter letter-spacing in larger sizes to maintain a professional, high-end editorial feel.

**Inter** is used for all body text, inputs, and labels. Its high x-height and systematic design ensure maximum legibility for dense medical records and operational lists. 

On mobile devices, headline scales are reduced to prevent excessive wrapping while maintaining hierarchy.

## Layout & Spacing

The layout follows a **Fixed-Fluid Hybrid** grid. On desktop, content is contained within a maximum width of 1440px to ensure readability, while sidebars and navigation may stretch. 

*   **Grid:** A 12-column grid is used for desktop, 8-column for tablet, and 4-column for mobile.
*   **Rhythm:** An 8px base unit governs all spatial relationships. 
*   **Margins:** Generous outer margins (40px on desktop) create a sense of breathing room, reinforcing the "calm" brand pillar.
*   **Adaptation:** On mobile, cards reflow to full width, and padding is tightened to 16px to maximize the utility of the smaller screen real estate.

## Elevation & Depth

Hierarchy is established through **Tonal Layers** supplemented by **Ambient Shadows**. 

Surfaces are primarily flat or slightly tinted (using the Tertiary color) to denote different functional zones. To indicate interactivity and depth (such as a profile card for a dog), a very soft, diffused shadow is applied:
*   **Shadow Style:** Low opacity (8-12%), large blur radius (16px+), and a slight vertical offset to simulate a natural top-down light source.
*   **Interactivity:** On hover, buttons and cards should subtly increase their elevation (longer shadow) rather than changing color dramatically.
*   **Overlays:** Modals and drawers use a soft backdrop blur (8px) to maintain context while focusing the user's attention.

## Shapes

The shape language is consistently **Rounded**, avoiding harsh 90-degree angles to maintain the "caring" and "friendly" aesthetic. 

*   **Standard Elements:** Buttons, input fields, and tags use a 0.5rem (8px) radius.
*   **Containers:** Larger cards and modals use a 1rem (16px) radius to create a soft, framed appearance for dog photos and data sets.
*   **Avatar/Images:** Dog profile photos should use a 1.5rem (24px) radius or be fully circular to emphasize the personal, living nature of the subjects.

## Components

*   **Buttons:** Primary buttons use the Compassionate Green with white text. Secondary buttons use a Subtle Brown outline. All buttons feature the 8px rounded corner.
*   **Status Chips:** Used for health and adoption status. They should have a light background tint of the status color with high-contrast text and a small leading icon (e.g., a heart for adoption, a medical cross for health).
*   **Cards:** Dog profile cards are the hero component. They feature a large image, a soft 16px radius, a subtle ambient shadow, and a clear "Status Chip" in the top right corner.
*   **Input Fields:** Use a light grey border that transitions to the Primary Green on focus. Labels should be positioned above the field using the `label-md` typography.
*   **Lists:** Animal logs and medical history should use "Zebra Striping" with the Tertiary color at 20% opacity to ensure legibility in data-heavy views.
*   **Shelter Progress Bars:** A specialized component to show capacity or adoption goals, using a thick, rounded bar with the Primary Green.
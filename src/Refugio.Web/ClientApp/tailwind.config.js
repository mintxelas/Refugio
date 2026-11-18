import forms from '@tailwindcss/forms';
import containerQueries from '@tailwindcss/container-queries';

/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  plugins: [forms, containerQueries],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        "outline": "#707973",
        "inverse-primary": "#95d4b3",
        "secondary": "#7d562d",
        "on-secondary": "#ffffff",
        "on-error": "#ffffff",
        "error-container": "#ffdad6",
        "on-error-container": "#93000a",
        "error": "#ba1a1a",
        "on-background": "#191c1d",
        "tertiary": "#4f4730",
        "on-tertiary": "#ffffff",
        "primary-fixed": "#b1f0ce",
        "on-primary": "#ffffff",
        "inverse-surface": "#2e3132",
        "surface-container": "#edeeef",
        "surface-container-highest": "#e1e3e4",
        "on-secondary-container": "#7a532a",
        "background": "#f8f9fa",
        "primary": "#0f5238",
        "on-surface": "#191c1d",
        "on-surface-variant": "#404943",
        "surface-container-high": "#e7e8e9",
        "surface-container-low": "#f3f4f5",
        "tertiary-container": "#675f46",
        "surface-container-lowest": "#ffffff",
        "tertiary-fixed": "#eee2c2",
        "on-tertiary-fixed": "#211b08",
        "on-secondary-fixed": "#2c1600",
        "primary-container": "#2d6a4f",
        "secondary-container": "#ffca98",
        "outline-variant": "#bfc9c1",
        "surface": "#f8f9fa",
        "primary-fixed-dim": "#95d4b3",
        "on-primary-fixed": "#002114",
        "secondary-fixed": "#ffdcbd",
        "secondary-fixed-dim": "#f0bd8b",
        "on-primary-container": "#a8e7c5",
        "surface-dim": "#d9dadb",
        "surface-bright": "#f8f9fa",
        "surface-variant": "#e1e3e4",
      },
      borderRadius: {
        "DEFAULT": "0.25rem",
        "lg": "0.5rem",
        "xl": "0.75rem",
        "full": "9999px"
      },
      spacing: {
        "md": "24px",
        "xl": "80px",
        "margin-desktop": "40px",
        "xs": "4px",
        "margin-mobile": "16px",
        "gutter": "24px",
        "sm": "12px",
        "lg": "48px",
        "base": "8px"
      },
      fontFamily: {
        "headline-xl": ["Montserrat"],
        "headline-lg": ["Montserrat"],
        "headline-md": ["Montserrat"],
        "body-lg": ["Inter"],
        "body-md": ["Inter"],
        "body-sm": ["Inter"],
        "label-md": ["Inter"],
        "label-sm": ["Inter"],
      },
      fontSize: {
        "headline-xl": ["40px", { lineHeight: "48px", letterSpacing: "-0.02em", fontWeight: "700" }],
        "headline-lg": ["32px", { lineHeight: "40px", fontWeight: "600" }],
        "headline-md": ["24px", { lineHeight: "32px", fontWeight: "600" }],
        "body-lg": ["18px", { lineHeight: "28px", fontWeight: "400" }],
        "body-md": ["16px", { lineHeight: "24px", fontWeight: "400" }],
        "body-sm": ["14px", { lineHeight: "20px", fontWeight: "400" }],
        "label-md": ["14px", { lineHeight: "16px", letterSpacing: "0.01em", fontWeight: "600" }],
        "label-sm": ["12px", { lineHeight: "16px", letterSpacing: "0.04em", fontWeight: "500" }],
      }
    }
  }

};

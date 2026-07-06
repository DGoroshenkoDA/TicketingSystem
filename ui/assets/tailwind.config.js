module.exports = {
  content: [
    "./js/**/*.js",
    "../lib/ticketing_ui_web.ex",
    "../lib/ticketing_ui_web/**/*.*ex",
    "../lib/ticketing_ui_web/**/*.heex"
  ],
  theme: {
    extend: {
      colors: {
        // Driven by CSS variables so the theme can be switched at runtime.
        brand: "var(--c-brand)",
        "brand-hover": "var(--c-brand-hover)",
        "brand-light": "var(--c-brand-light)",
        success: "var(--c-success)",
        warning: "var(--c-warning)",
        error: "var(--c-error)",
        info: "var(--c-info)"
      }
    }
  },
  plugins: [require("@tailwindcss/forms")]
}

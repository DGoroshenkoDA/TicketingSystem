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
        brand: "#6C5CE7",
        "brand-hover": "#5A4BD1",
        "brand-light": "#A29BFE",
        success: "#00B894",
        warning: "#FDCB6E",
        error: "#FF7675",
        info: "#74B9FF"
      }
    }
  },
  plugins: [require("@tailwindcss/forms")]
}

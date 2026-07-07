import "phoenix_html"
import { Socket } from "phoenix"
import { LiveSocket } from "phoenix_live_view"

const Hooks = {}

// Render a UTC ISO-8601 timestamp in the visitor's local timezone.
// The element keeps the raw ISO string in `datetime`/`data-utc`, so if JS is
// off (or parsing fails) the server-rendered value stays as the fallback.
function formatLocalTime(el) {
  const iso = el.getAttribute("datetime") || el.getAttribute("data-utc")
  if (!iso) return

  const date = new Date(iso)
  if (isNaN(date.getTime())) return // leave the raw ISO string in place

  try {
    el.textContent = new Intl.DateTimeFormat(undefined, {
      day: "numeric",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    }).format(date)
  } catch (_e) {
    // Keep the raw string on any unexpected Intl failure.
  }
}

// LiveView-rendered <time> elements: localize on mount and re-localize after
// every server patch (the server always sends the raw UTC string).
Hooks.LocalTime = {
  mounted() {
    formatLocalTime(this.el)
  },
  updated() {
    formatLocalTime(this.el)
  }
}

// Dead controller views: localize any static time[data-utc]. Runs immediately
// if the DOM is already parsed (deferred script), otherwise on DOMContentLoaded.
function localizeDeadViewTimes() {
  document.querySelectorAll("time[data-utc]").forEach(formatLocalTime)
}

if (document.readyState === "loading") {
  window.addEventListener("DOMContentLoaded", localizeDeadViewTimes)
} else {
  localizeDeadViewTimes()
}

// Native HTML5 drag & drop for the Kanban board.
// Cards carry data-ticket-id; columns carry data-state. Dropping a card on a
// column pushes "move_ticket" so the server can persist the new state.
Hooks.Board = {
  mounted() {
    this.setup()
  },
  updated() {
    this.setup()
  },
  setup() {
    const el = this.el

    el.querySelectorAll("[data-ticket-id]").forEach((card) => {
      card.setAttribute("draggable", "true")
      card.addEventListener("dragstart", (e) => {
        e.dataTransfer.setData("text/plain", card.dataset.ticketId)
        e.dataTransfer.effectAllowed = "move"
        card.classList.add("opacity-50")
      })
      card.addEventListener("dragend", () => card.classList.remove("opacity-50"))
    })

    el.querySelectorAll("[data-state]").forEach((col) => {
      col.addEventListener("dragover", (e) => {
        e.preventDefault()
        e.dataTransfer.dropEffect = "move"
        col.classList.add("ring-2", "ring-brand")
      })
      col.addEventListener("dragleave", () => col.classList.remove("ring-2", "ring-brand"))
      col.addEventListener("drop", (e) => {
        e.preventDefault()
        col.classList.remove("ring-2", "ring-brand")
        const id = e.dataTransfer.getData("text/plain")
        const state = col.dataset.state
        if (id && state) {
          this.pushEvent("move_ticket", { id: id, state: state })
        }
      })
    })
  }
}

// Authentication screens (dead views): password show/hide, live password-criteria
// checklist, and submit gating. Per the spec the account rule is "at least 8
// characters" + matching confirmation; the uppercase/special rows are shown as
// guidance and turn green when met, but do not block submission.
function setupAuthForms() {
  document.querySelectorAll("[data-pw-toggle]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const input = document.getElementById(btn.getAttribute("data-pw-toggle"))
      if (!input) return
      const reveal = input.type === "password"
      input.type = reveal ? "text" : "password"
      const eye = btn.querySelector(".pw-eye")
      const eyeOff = btn.querySelector(".pw-eye-off")
      if (eye) eye.classList.toggle("hidden", reveal)
      if (eyeOff) eyeOff.classList.toggle("hidden", !reveal)
    })
  })

  const pw = document.getElementById("signup-password")
  if (!pw) return

  const confirm = document.getElementById("signup-password-confirm")
  const submit = document.querySelector("[data-signup-submit]")
  const rows = document.querySelectorAll("[data-criterion]")

  const evaluate = () => {
    const v = pw.value || ""
    const checks = {
      len: v.length >= 8,
      upper: /[A-Z]/.test(v),
      special: /[^A-Za-z0-9]/.test(v)
    }
    checks.all = checks.len && checks.upper && checks.special

    rows.forEach((row) => {
      const key = row.getAttribute("data-criterion")
      row.setAttribute("data-met", checks[key] ? "true" : "false")
    })

    if (submit) {
      const matches = !confirm || confirm.value === v
      submit.disabled = !(checks.len && matches)
    }
  }

  pw.addEventListener("input", evaluate)
  if (confirm) confirm.addEventListener("input", evaluate)
  evaluate()
}

setupAuthForms()

const csrfToken = document
  .querySelector("meta[name='csrf-token']")
  .getAttribute("content")

const liveSocket = new LiveSocket("/live", Socket, {
  params: { _csrf_token: csrfToken },
  hooks: Hooks
})

liveSocket.connect()
window.liveSocket = liveSocket

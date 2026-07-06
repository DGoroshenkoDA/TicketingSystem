import "phoenix_html"
import { Socket } from "phoenix"
import { LiveSocket } from "phoenix_live_view"

const Hooks = {}

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

const csrfToken = document
  .querySelector("meta[name='csrf-token']")
  .getAttribute("content")

const liveSocket = new LiveSocket("/live", Socket, {
  params: { _csrf_token: csrfToken },
  hooks: Hooks
})

liveSocket.connect()
window.liveSocket = liveSocket

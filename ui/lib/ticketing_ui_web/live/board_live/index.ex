defmodule TicketingUiWeb.BoardLive.Index do
  use TicketingUiWeb, :live_view

  alias TicketingUi.Api.{EpicsApi, TeamsApi, TicketsApi}

  @states [
    {"new", "New"},
    {"ready_for_implementation", "Ready for implementation"},
    {"in_progress", "In progress"},
    {"ready_for_acceptance", "Ready for acceptance"},
    {"done", "Done"}
  ]

  @types ["bug", "feature", "fix"]

  @impl true
  def mount(_params, _session, socket) do
    teams = list_teams(socket)
    selected = teams |> List.first() |> team_id()

    socket =
      socket
      |> assign(
        teams: teams,
        selected_team_id: selected,
        epics: [],
        tickets: [],
        filter_type: "",
        filter_epic: "",
        filter_search: "",
        modal: nil,
        modal_form: %{},
        modal_epics: [],
        modal_error: nil
      )
      |> load_board()

    {:ok, socket}
  end

  # --- team + filters ---

  @impl true
  def handle_event("select_team", %{"team_id" => team_id}, socket) do
    {:noreply,
     socket
     |> assign(selected_team_id: nilify(team_id), filter_epic: "")
     |> load_board()}
  end

  def handle_event("filter", %{"type" => type, "epic_id" => epic_id, "search" => search}, socket) do
    {:noreply,
     socket
     |> assign(filter_type: type, filter_epic: epic_id, filter_search: search)
     |> load_tickets()}
  end

  # --- modal open/close ---

  def handle_event("open_new", _params, socket) do
    team_id = socket.assigns.selected_team_id

    form = %{
      "team_id" => team_id,
      "type" => "bug",
      "epic_id" => "",
      "title" => "",
      "body" => "",
      "state" => "new"
    }

    {:noreply,
     assign(socket,
       modal: %{mode: :new, id: nil, meta: nil},
       modal_form: form,
       modal_epics: epics_for(socket, team_id),
       modal_error: nil
     )}
  end

  def handle_event("open_ticket", %{"id" => id}, socket) do
    case TicketsApi.get(token(socket), id) do
      {:ok, t} ->
        form = %{
          "team_id" => t["teamId"],
          "type" => t["type"],
          "epic_id" => t["epicId"] || "",
          "title" => t["title"],
          "body" => t["body"],
          "state" => t["state"]
        }

        meta = %{
          created_by: t["createdByName"],
          created_at: t["createdAt"],
          modified_at: t["modifiedAt"]
        }

        {:noreply,
         assign(socket,
           modal: %{mode: :edit, id: t["id"], meta: meta},
           modal_form: form,
           modal_epics: epics_for(socket, t["teamId"]),
           modal_error: nil
         )}

      {:error, err} ->
        {:noreply, put_flash(socket, :error, err[:detail] || "Could not open ticket.")}
    end
  end

  def handle_event("close_modal", _params, socket), do: {:noreply, assign(socket, modal: nil)}

  # Track modal field changes; reloading epics and clearing the epic when team changes.
  def handle_event("modal_change", params, socket) do
    prev = socket.assigns.modal_form
    form = Map.merge(prev, Map.take(params, ~w(team_id type epic_id title body state)))

    if params["team_id"] && params["team_id"] != prev["team_id"] do
      {:noreply,
       assign(socket,
         modal_form: Map.put(form, "epic_id", ""),
         modal_epics: epics_for(socket, params["team_id"])
       )}
    else
      {:noreply, assign(socket, modal_form: form)}
    end
  end

  # --- save / delete ---

  def handle_event("save", params, socket) do
    attrs = %{
      team_id: params["team_id"],
      type: params["type"],
      epic_id: params["epic_id"],
      title: params["title"],
      body: params["body"],
      state: params["state"]
    }

    result =
      case socket.assigns.modal do
        %{mode: :new} -> TicketsApi.create(token(socket), attrs)
        %{mode: :edit, id: id} -> TicketsApi.update(token(socket), id, attrs)
      end

    case result do
      {:ok, _} ->
        {:noreply,
         socket
         |> assign(modal: nil)
         |> put_flash(:info, "Ticket saved.")
         |> load_tickets()}

      {:error, err} ->
        {:noreply, assign(socket, modal_error: err[:detail] || "Could not save ticket.")}
    end
  end

  def handle_event("delete_ticket", %{"id" => id}, socket) do
    case TicketsApi.delete(token(socket), id) do
      {:ok, _} ->
        {:noreply, socket |> assign(modal: nil) |> put_flash(:info, "Ticket deleted.") |> load_tickets()}

      {:error, err} ->
        {:noreply, put_flash(socket, :error, err[:detail] || "Could not delete ticket.")}
    end
  end

  # --- data loading ---

  defp token(socket), do: socket.assigns.current_user.access_token

  defp list_teams(socket) do
    case TeamsApi.list(token(socket)) do
      {:ok, teams} when is_list(teams) -> teams
      _ -> []
    end
  end

  defp epics_for(_socket, nil), do: []

  defp epics_for(socket, team_id) do
    case EpicsApi.list(token(socket), team_id) do
      {:ok, epics} when is_list(epics) -> epics
      _ -> []
    end
  end

  defp load_board(%{assigns: %{selected_team_id: team_id}} = socket) do
    socket |> assign(epics: epics_for(socket, team_id)) |> load_tickets()
  end

  defp load_tickets(%{assigns: %{selected_team_id: nil}} = socket), do: assign(socket, tickets: [])

  defp load_tickets(%{assigns: assigns} = socket) do
    filters = %{
      team_id: assigns.selected_team_id,
      type: assigns.filter_type,
      epic_id: assigns.filter_epic,
      search: assigns.filter_search
    }

    case TicketsApi.list(token(socket), filters) do
      {:ok, tickets} when is_list(tickets) -> assign(socket, tickets: tickets)
      _ -> assign(socket, tickets: [])
    end
  end

  defp team_id(nil), do: nil
  defp team_id(team), do: team["id"]

  defp nilify(""), do: nil
  defp nilify(value), do: value

  defp tickets_in(tickets, state), do: Enum.filter(tickets, &(&1["state"] == state))

  defp type_class("bug"), do: "bg-red-50 text-red-700"
  defp type_class("feature"), do: "bg-green-50 text-green-700"
  defp type_class("fix"), do: "bg-blue-50 text-blue-700"
  defp type_class(_), do: "bg-gray-50 text-gray-700"

  @impl true
  def render(assigns) do
    assigns = assign(assigns, states: @states, types: @types)

    ~H"""
    <div class="py-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Board</h1>
          <div class="mt-2 flex gap-3 text-sm">
            <.link navigate={~p"/teams"} class="text-brand hover:underline">Teams</.link>
            <.link navigate={~p"/epics"} class="text-brand hover:underline">Epics</.link>
          </div>
        </div>

        <form :if={@teams != []} id="board-team-select" phx-change="select_team">
          <label class="block text-xs font-medium text-gray-500">Team</label>
          <select name="team_id" class="mt-1 rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
            <option :for={team <- @teams} value={team["id"]} selected={team["id"] == @selected_team_id}>
              {team["name"]}
            </option>
          </select>
        </form>
      </div>

      <p :if={@teams == []} class="mt-8 text-gray-500">
        Create a team first on the <.link navigate={~p"/teams"} class="text-brand underline">Teams</.link> page.
      </p>

      <div :if={@selected_team_id} class="mt-4 flex flex-wrap items-end gap-3">
        <form id="board-filters" phx-change="filter" class="flex flex-wrap items-end gap-3">
          <div>
            <label class="block text-xs font-medium text-gray-500">Type</label>
            <select name="type" class="mt-1 rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
              <option value="" selected={@filter_type == ""}>All</option>
              <option :for={t <- @types} value={t} selected={@filter_type == t}>{t}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-500">Epic</label>
            <select name="epic_id" class="mt-1 rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
              <option value="" selected={@filter_epic == ""}>All</option>
              <option :for={e <- @epics} value={e["id"]} selected={@filter_epic == e["id"]}>{e["title"]}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-500">Search title</label>
            <input
              type="text"
              name="search"
              value={@filter_search}
              phx-debounce="300"
              class="mt-1 rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
            />
          </div>
        </form>

        <button phx-click="open_new" class="ml-auto rounded-lg bg-brand px-4 py-2 font-medium text-white hover:bg-brand-hover">
          New ticket
        </button>
      </div>

      <div :if={@selected_team_id} class="mt-6 grid grid-cols-1 gap-4 md:grid-cols-5">
        <div :for={{state, label} <- @states} class="rounded-lg bg-gray-50 p-3">
          <h2 class="flex items-center justify-between text-sm font-semibold text-gray-700">
            {label}
            <span class="rounded-full bg-gray-200 px-2 text-xs text-gray-600">
              {length(tickets_in(@tickets, state))}
            </span>
          </h2>

          <div class="mt-3 space-y-2">
            <button
              :for={ticket <- tickets_in(@tickets, state)}
              phx-click="open_ticket"
              phx-value-id={ticket["id"]}
              class="block w-full rounded-lg border border-gray-200 bg-white p-3 text-left shadow-sm hover:border-brand"
            >
              <div class="flex items-center gap-2">
                <span class={["rounded px-1.5 py-0.5 text-xs font-medium", type_class(ticket["type"])]}>
                  {ticket["type"]}
                </span>
                <span :if={ticket["epicTitle"]} class="truncate text-xs text-gray-400">
                  {ticket["epicTitle"]}
                </span>
              </div>
              <p class="mt-1 text-sm font-medium text-gray-900">{ticket["title"]}</p>
            </button>
          </div>
        </div>
      </div>

      <.ticket_modal :if={@modal} modal={@modal} form={@modal_form} epics={@modal_epics} teams={@teams} states={@states} types={@types} error={@modal_error} />
    </div>
    """
  end

  defp ticket_modal(assigns) do
    ~H"""
    <div class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4">
      <div class="mt-10 w-full max-w-lg rounded-xl bg-white p-6 shadow-xl">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-gray-900">
            {if @modal.mode == :new, do: "New ticket", else: "Edit ticket"}
          </h2>
          <button phx-click="close_modal" class="text-gray-400 hover:text-gray-600">✕</button>
        </div>

        <dl :if={@modal.meta} class="mt-2 text-xs text-gray-400">
          <span>Created by {@modal.meta.created_by || "—"}</span>
          · <span>Created {@modal.meta.created_at}</span>
          · <span>Modified {@modal.meta.modified_at}</span>
        </dl>

        <p :if={@error} class="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800 ring-1 ring-red-200">
          {@error}
        </p>

        <form id="ticket-form" phx-change="modal_change" phx-submit="save" class="mt-4 space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-gray-700">Team</label>
              <select name="team_id" class="mt-1 w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
                <option :for={team <- @teams} value={team["id"]} selected={team["id"] == @form["team_id"]}>
                  {team["name"]}
                </option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Type</label>
              <select name="type" class="mt-1 w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
                <option :for={t <- @types} value={t} selected={t == @form["type"]}>{t}</option>
              </select>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Epic</label>
            <select name="epic_id" class="mt-1 w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
              <option value="" selected={@form["epic_id"] in [nil, ""]}>— none —</option>
              <option :for={e <- @epics} value={e["id"]} selected={e["id"] == @form["epic_id"]}>{e["title"]}</option>
            </select>
          </div>

          <div :if={@modal.mode == :edit}>
            <label class="block text-sm font-medium text-gray-700">State</label>
            <select name="state" class="mt-1 w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
              <option :for={{s, label} <- @states} value={s} selected={s == @form["state"]}>{label}</option>
            </select>
          </div>
          <input :if={@modal.mode == :new} type="hidden" name="state" value="new" />

          <div>
            <label class="block text-sm font-medium text-gray-700">Title</label>
            <input
              type="text"
              name="title"
              value={@form["title"]}
              required
              class="mt-1 w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Body</label>
            <textarea
              name="body"
              rows="4"
              required
              class="mt-1 w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
            >{@form["body"]}</textarea>
          </div>

          <div class="flex items-center justify-between pt-2">
            <button
              :if={@modal.mode == :edit}
              type="button"
              phx-click="delete_ticket"
              phx-value-id={@modal.id}
              data-confirm="Delete this ticket? Its comments will also be removed."
              class="text-sm text-error hover:underline"
            >
              Delete
            </button>
            <div class="ml-auto flex gap-2">
              <button type="button" phx-click="close_modal" class="rounded-lg px-4 py-2 text-gray-600 hover:bg-gray-100">
                Cancel
              </button>
              <button type="submit" class="rounded-lg bg-brand px-4 py-2 font-medium text-white hover:bg-brand-hover">
                Save
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
    """
  end
end

defmodule TicketingUiWeb.BoardLive.Index do
  use TicketingUiWeb, :live_view

  alias TicketingUi.Api.{CommentsApi, EpicsApi, HistoryApi, TeamsApi, TicketsApi}

  @states [
    {"new", "New"},
    {"ready_for_implementation", "Ready for implementation"},
    {"in_progress", "In progress"},
    {"ready_for_acceptance", "Ready for acceptance"},
    {"done", "Done"}
  ]

  # Lookup for prettifying state values in the change history.
  @state_labels Map.new(@states)

  @types ["bug", "feature", "fix"]

  @page_path "/board"

  @impl true
  def mount(_params, _session, socket) do
    socket =
      assign(socket,
        teams: [],
        selected_team_id: nil,
        epics: [],
        tickets: [],
        filter_type: "",
        filter_epic: "",
        filter_search: "",
        modal: nil,
        modal_form: %{},
        modal_epics: [],
        modal_error: nil,
        comments: [],
        history: [],
        history_error: nil,
        modal_tab: :comments,
        loading: true,
        load_error: nil
      )

    if connected?(socket) do
      {:ok, load_initial(socket)}
    else
      {:ok, socket}
    end
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

  def handle_event("set_modal_tab", %{"tab" => tab}, socket) do
    {:noreply, assign(socket, modal_tab: tab_atom(tab))}
  end

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
       modal_error: nil,
       comments: [],
       history: [],
       history_error: nil,
       modal_tab: :comments
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

        # Only the creator + created date survive here; the rest of the timeline
        # lives in the History section fed by the change-history endpoint.
        meta = %{
          creator: t["createdByName"],
          created_at: t["createdAt"]
        }

        socket =
          socket
          |> assign(
            modal: %{mode: :edit, id: t["id"], meta: meta},
            modal_form: form,
            modal_epics: epics_for(socket, t["teamId"]),
            modal_error: nil,
            modal_tab: :comments,
            comments: load_comment_list(socket, t["id"])
          )
          |> load_ticket_history(t["id"])

        {:noreply, socket}

      {:error, err} ->
        {:noreply, put_api_error(socket, err, "Could not open ticket.")}
    end
  end

  def handle_event("close_modal", _params, socket), do: {:noreply, assign(socket, modal: nil)}

  # Drag & drop: persist the new state immediately.
  def handle_event("move_ticket", %{"id" => id, "state" => state}, socket) do
    case TicketsApi.update_state(token(socket), id, state) do
      {:ok, _} ->
        # Reload so the moved card lands in the target column (top, by modified_at).
        {:noreply, load_tickets(socket)}

      {:error, err} ->
        # Server state is unchanged; the re-render returns the card to its column.
        {:noreply, put_api_error(socket, err, "Could not move the ticket.")}
    end
  end

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
        {:noreply, put_modal_error(socket, err, "Could not save ticket.")}
    end
  end

  def handle_event("add_comment", %{"body" => body}, socket) do
    case socket.assigns.modal do
      %{mode: :edit, id: id} ->
        case CommentsApi.create(token(socket), id, body) do
          {:ok, _} ->
            {:noreply, assign(socket, comments: load_comment_list(socket, id))}

          {:error, err} ->
            {:noreply, put_modal_error(socket, err, "Could not add comment.")}
        end

      _ ->
        {:noreply, socket}
    end
  end

  def handle_event("delete_ticket", %{"id" => id}, socket) do
    case TicketsApi.delete(token(socket), id) do
      {:ok, _} ->
        {:noreply, socket |> assign(modal: nil) |> put_flash(:info, "Ticket deleted.") |> load_tickets()}

      {:error, err} ->
        {:noreply, put_api_error(socket, err, "Could not delete ticket.")}
    end
  end

  # --- data loading ---

  defp token(socket), do: socket.assigns.current_user.access_token

  # Initial load (connected mount): teams first, then epics + tickets.
  defp load_initial(socket) do
    case TeamsApi.list(token(socket)) do
      {:ok, teams} when is_list(teams) ->
        selected = teams |> List.first() |> team_id()

        socket
        |> assign(teams: teams, selected_team_id: selected, loading: false, load_error: nil)
        |> load_board()

      other ->
        handle_load_error(socket, other)
    end
  end

  # Epics/comments fetched for the modal only; failures fall back to empty and
  # the interactive handlers surface their own errors.
  defp load_comment_list(socket, ticket_id) do
    case CommentsApi.list(token(socket), ticket_id) do
      {:ok, list} when is_list(list) -> list
      _ -> []
    end
  end

  # Change history for the modal. 401 bounces through the refresh endpoint like
  # other loads; any other failure keeps the section empty and shows an inline
  # note rather than a blank timeline.
  defp load_ticket_history(socket, ticket_id) do
    if socket.redirected do
      socket
    else
      case HistoryApi.list(token(socket), ticket_id) do
        {:ok, list} when is_list(list) ->
          assign(socket, history: list, history_error: nil)

        {:error, %{status: 401}} ->
          redirect_to_refresh(socket)

        {:error, err} ->
          assign(socket, history: [], history_error: err[:detail] || "Could not load history.")

        _ ->
          assign(socket, history: [], history_error: "Could not load history.")
      end
    end
  end

  defp epics_for(_socket, nil), do: []

  defp epics_for(socket, team_id) do
    case EpicsApi.list(token(socket), team_id) do
      {:ok, epics} when is_list(epics) -> epics
      _ -> []
    end
  end

  defp load_board(socket) do
    socket |> load_board_epics() |> load_tickets()
  end

  defp load_board_epics(%{assigns: %{selected_team_id: nil}} = socket), do: assign(socket, epics: [])

  defp load_board_epics(%{assigns: %{selected_team_id: team_id}} = socket) do
    if socket.redirected do
      socket
    else
      case EpicsApi.list(token(socket), team_id) do
        {:ok, epics} when is_list(epics) -> assign(socket, epics: epics, load_error: nil)
        other -> handle_load_error(socket, other)
      end
    end
  end

  defp load_tickets(%{assigns: %{selected_team_id: nil}} = socket), do: assign(socket, tickets: [])

  defp load_tickets(%{assigns: assigns} = socket) do
    if socket.redirected do
      socket
    else
      filters = %{
        team_id: assigns.selected_team_id,
        type: assigns.filter_type,
        epic_id: assigns.filter_epic,
        search: assigns.filter_search
      }

      case TicketsApi.list(token(socket), filters) do
        {:ok, tickets} when is_list(tickets) -> assign(socket, tickets: tickets, load_error: nil)
        other -> handle_load_error(socket, other)
      end
    end
  end

  # 401 on a page load -> bounce through the refresh endpoint; other failures
  # surface a banner instead of a misleading empty board.
  defp handle_load_error(socket, {:error, %{status: 401}}), do: redirect_to_refresh(socket)

  defp handle_load_error(socket, {:error, err}),
    do: assign(socket, loading: false, load_error: err[:detail] || "Could not load the board.")

  defp handle_load_error(socket, _),
    do: assign(socket, loading: false, load_error: "Could not load the board.")

  # Interactive API failures: 401 -> refresh, anything else -> flash / modal error.
  defp put_api_error(socket, %{status: 401}, _fallback), do: redirect_to_refresh(socket)
  defp put_api_error(socket, err, fallback), do: put_flash(socket, :error, err[:detail] || fallback)

  defp put_modal_error(socket, %{status: 401}, _fallback), do: redirect_to_refresh(socket)
  defp put_modal_error(socket, err, fallback), do: assign(socket, modal_error: err[:detail] || fallback)

  defp redirect_to_refresh(socket),
    do: redirect(socket, to: ~p"/session/refresh?#{[return_to: @page_path]}")

  defp team_id(nil), do: nil
  defp team_id(team), do: team["id"]

  defp nilify(""), do: nil
  defp nilify(value), do: value

  defp tickets_in(tickets, state), do: Enum.filter(tickets, &(&1["state"] == state))

  defp type_class("bug"), do: "bg-red-50 text-red-700"
  defp type_class("feature"), do: "bg-green-50 text-green-700"
  defp type_class("fix"), do: "bg-blue-50 text-blue-700"
  defp type_class(_), do: "bg-gray-50 text-gray-700"

  # --- change-history display helpers ---

  # "epic" -> "Epic", "state" -> "State".
  defp tab_atom("history"), do: :history
  defp tab_atom(_), do: :comments

  defp field_label("body"), do: "Description"
  defp field_label(field), do: field |> to_string() |> String.capitalize()

  # Prettify enum values; null/blank -> "None". State/type get human labels,
  # free-text fields (title/body/epic/team) render as-is.
  defp pretty_value(_field, value) when value in [nil, ""], do: "None"
  defp pretty_value("state", value), do: @state_labels[value] || humanize_enum(value)
  defp pretty_value("type", value), do: humanize_enum(value)
  defp pretty_value(_field, value), do: value

  defp humanize_enum(value),
    do: value |> to_string() |> String.replace("_", " ") |> String.capitalize()

  @impl true
  def render(assigns) do
    assigns = assign(assigns, states: @states, types: @types)

    ~H"""
    <div class="py-6">
      <p
        :if={@load_error}
        class="mb-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-800 ring-1 ring-red-200"
        role="alert"
      >
        {@load_error}
      </p>

      <p :if={@loading} class="mt-2 text-gray-500">Loading board…</p>

      <p :if={not @loading and @teams == []} class="mt-2 text-gray-500">
        Create a team first on the <.link navigate={~p"/teams"} class="text-brand underline">Teams</.link> page.
      </p>

      <div :if={@selected_team_id} class="flex flex-wrap items-end gap-3">
        <form id="board-team-select" phx-change="select_team">
          <label class="block text-xs font-medium text-gray-500">Team</label>
          <select name="team_id" class="mt-1 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
            <option :for={team <- @teams} value={team["id"]} selected={team["id"] == @selected_team_id}>
              {team["name"]}
            </option>
          </select>
        </form>

        <form id="board-filters" phx-change="filter" class="flex flex-wrap items-end gap-3">
          <div>
            <label class="block text-xs font-medium text-gray-500">Type</label>
            <select name="type" class="mt-1 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
              <option value="" selected={@filter_type == ""}>All</option>
              <option :for={t <- @types} value={t} selected={@filter_type == t}>{t}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-500">Epic</label>
            <select name="epic_id" class="mt-1 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
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
              class="mt-1 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand"
            />
          </div>
        </form>

        <button phx-click="open_new" class="ml-auto rounded-lg bg-brand px-4 py-2 font-medium text-white hover:bg-brand-hover">
          New ticket
        </button>
      </div>

      <div :if={@selected_team_id} id="kanban" phx-hook="Board" class="mt-6 grid grid-cols-1 gap-4 md:grid-cols-5">
        <div :for={{state, label} <- @states} data-state={state} class="rounded-lg bg-gray-50 p-3">
          <h2 class="flex items-center justify-between text-sm font-semibold text-gray-700">
            {label}
            <span class="rounded-full bg-gray-200 px-2 text-xs text-gray-600">
              {length(tickets_in(@tickets, state))}
            </span>
          </h2>

          <div class="mt-3 space-y-2">
            <button
              :for={ticket <- tickets_in(@tickets, state)}
              data-ticket-id={ticket["id"]}
              phx-click="open_ticket"
              phx-value-id={ticket["id"]}
              class="block w-full cursor-grab rounded-lg border border-gray-200 bg-white p-3 text-left shadow-sm hover:border-brand active:cursor-grabbing"
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

      <.ticket_modal
        :if={@modal}
        modal={@modal}
        form={@modal_form}
        epics={@modal_epics}
        teams={@teams}
        states={@states}
        types={@types}
        error={@modal_error}
        comments={@comments}
        history={@history}
        history_error={@history_error}
        modal_tab={@modal_tab}
      />
    </div>
    """
  end

  defp ticket_modal(assigns) do
    ~H"""
    <div class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4">
      <div class={[
        "relative mt-10 w-full rounded-xl bg-white p-6 shadow-xl",
        (@modal.mode == :edit && "max-w-4xl") || "max-w-lg"
      ]}>
        <p :if={@error} class="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-800 ring-1 ring-red-200">
          {@error}
        </p>

        <form id="ticket-form" phx-change="modal_change" phx-submit="save">
          <div class="flex items-start gap-2">
            <input
              type="text"
              name="title"
              value={@form["title"]}
              placeholder="Ticket title"
              required
              aria-label="Ticket title"
              class="flex-1 rounded-lg border border-transparent bg-transparent px-2 py-1.5 text-xl font-semibold text-gray-900 placeholder:text-gray-400 hover:border-gray-200 focus:border-gray-300 focus:bg-white focus:outline-none focus:ring-1 focus:ring-brand"
            />
            <button
              type="button"
              phx-click="close_modal"
              aria-label="Close"
              class="mt-1.5 shrink-0 rounded-lg p-1.5 text-gray-400 hover:bg-gray-100 hover:text-gray-600"
            >
              <.icon name="x" class="h-5 w-5" />
            </button>
          </div>

          <div class={["mt-4 grid grid-cols-1 gap-6", @modal.mode == :edit && "md:grid-cols-3"]}>
            <%!-- LEFT (main): description --%>
            <div class="md:col-span-2">
              <label class="mb-1 block text-sm font-medium text-gray-700">Description</label>
              <textarea
                name="body"
                rows="10"
                required
                placeholder="Describe the ticket…"
                class="w-full rounded-lg border-gray-300 text-sm focus:border-brand focus:ring-brand"
              >{@form["body"]}</textarea>
            </div>

            <%!-- RIGHT (sidebar): properties + actions --%>
            <div class="space-y-3 md:col-span-1">
              <div>
                <label class="mb-1 block text-sm font-medium text-gray-700">Team</label>
                <select name="team_id" class="w-full rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
                  <option :for={team <- @teams} value={team["id"]} selected={team["id"] == @form["team_id"]}>
                    {team["name"]}
                  </option>
                </select>
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-gray-700">Type</label>
                <select name="type" class="w-full rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
                  <option :for={t <- @types} value={t} selected={t == @form["type"]}>{t}</option>
                </select>
              </div>

              <div>
                <label class="mb-1 block text-sm font-medium text-gray-700">Epic</label>
                <select name="epic_id" class="w-full rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
                  <option value="" selected={@form["epic_id"] in [nil, ""]}>— none —</option>
                  <option :for={e <- @epics} value={e["id"]} selected={e["id"] == @form["epic_id"]}>{e["title"]}</option>
                </select>
              </div>

              <div :if={@modal.mode == :edit}>
                <label class="mb-1 block text-sm font-medium text-gray-700">State</label>
                <select name="state" class="w-full rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
                  <option :for={{s, label} <- @states} value={s} selected={s == @form["state"]}>{label}</option>
                </select>
              </div>

              <div class="flex items-center justify-end gap-2 pt-2">
                <button
                  :if={@modal.mode == :edit}
                  type="button"
                  phx-click="delete_ticket"
                  phx-value-id={@modal.id}
                  data-confirm="Delete this ticket? Its comments will also be removed."
                  title="Delete"
                  aria-label="Delete"
                  class="mr-auto inline-flex items-center justify-center rounded-lg p-2 text-error hover:bg-red-50"
                >
                  <.icon name="trash" class="h-5 w-5" />
                </button>
                <button
                  type="button"
                  phx-click="close_modal"
                  title="Cancel"
                  aria-label="Cancel"
                  class="inline-flex items-center justify-center rounded-lg p-2 text-gray-500 hover:bg-gray-100"
                >
                  <.icon name="x" class="h-5 w-5" />
                </button>
                <button
                  type="submit"
                  title="Save"
                  aria-label="Save"
                  class="inline-flex items-center justify-center rounded-lg bg-brand p-2 text-white hover:bg-brand-hover"
                >
                  <.icon name="check" class="h-5 w-5" />
                </button>
              </div>
            </div>
          </div>
        </form>

        <%!-- Activity: Comments / History tabs (edit only), aligned under the description --%>
        <div :if={@modal.mode == :edit} class="mt-6 grid grid-cols-1 gap-6 md:grid-cols-3">
          <div class="md:col-span-2">
            <div class="flex gap-1 border-b border-gray-200">
              <button
                type="button"
                phx-click="set_modal_tab"
                phx-value-tab="comments"
                class={[
                  "-mb-px border-b-2 px-3 py-2 text-sm font-medium",
                  (@modal_tab == :comments && "border-brand text-brand") ||
                    "border-transparent text-gray-500 hover:text-gray-700"
                ]}
              >
                Comments
              </button>
              <button
                type="button"
                phx-click="set_modal_tab"
                phx-value-tab="history"
                class={[
                  "-mb-px border-b-2 px-3 py-2 text-sm font-medium",
                  (@modal_tab == :history && "border-brand text-brand") ||
                    "border-transparent text-gray-500 hover:text-gray-700"
                ]}
              >
                History
              </button>
            </div>

            <%!-- Comments panel --%>
            <div class={["mt-3", @modal_tab != :comments && "hidden"]}>
              <form id="comment-form" phx-submit="add_comment" class="flex gap-2">
                <input
                  type="text"
                  name="body"
                  placeholder="Add a comment"
                  required
                  class="flex-1 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand"
                />
                <button type="submit" class="rounded-lg bg-brand px-3 py-1.5 text-sm text-white hover:bg-brand-hover">
                  Add
                </button>
              </form>
              <p :if={@comments == []} class="mt-3 text-sm text-gray-400">No comments yet.</p>
              <ul class="mt-3 space-y-2">
                <li :for={c <- @comments} class="rounded-lg bg-gray-50 p-2">
                  <div class="text-xs text-gray-400">
                    {c["authorName"] || "—"} · <.local_time at={c["createdAt"]} id={"lt-comment-#{c["id"]}"} />
                  </div>
                  <p class="text-sm text-gray-800">{c["body"]}</p>
                </li>
              </ul>
            </div>

            <%!-- History panel --%>
            <div class={["mt-3", @modal_tab != :history && "hidden"]}>
              <p :if={@history_error} class="text-xs text-error">{@history_error}</p>
              <ul class="space-y-3">
                <li :for={h <- @history}>
                  <div class="text-xs text-gray-400">
                    {h["changedByName"] || "Someone"} · <.local_time at={h["changedAt"]} id={"lt-hist-#{h["id"]}"} />
                  </div>
                  <div class="text-sm text-gray-700">
                    {field_label(h["field"])}:
                    <span class="text-gray-500">“{pretty_value(h["field"], h["oldValue"])}”</span>
                    → <span class="font-medium">“{pretty_value(h["field"], h["newValue"])}”</span>
                  </div>
                </li>
                <li>
                  <div class="text-xs text-gray-400">
                    {@modal.meta.creator || "—"} · <.local_time at={@modal.meta.created_at} id={"lt-created-#{@modal.id}"} />
                  </div>
                  <div class="text-sm text-gray-500">Created ticket</div>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
    """
  end
end

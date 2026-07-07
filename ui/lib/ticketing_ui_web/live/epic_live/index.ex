defmodule TicketingUiWeb.EpicLive.Index do
  use TicketingUiWeb, :live_view

  alias TicketingUi.Api.{EpicsApi, TeamsApi}

  @page_path "/epics"

  @impl true
  def mount(_params, _session, socket) do
    socket =
      assign(socket,
        teams: [],
        selected_team_id: nil,
        epics: [],
        panel_mode: nil,
        panel_form: %{},
        loading: true,
        load_error: nil
      )

    if connected?(socket) do
      {:ok, load_initial(socket)}
    else
      {:ok, socket}
    end
  end

  defp load_initial(socket) do
    case TeamsApi.list(token(socket)) do
      {:ok, teams} when is_list(teams) ->
        selected = teams |> List.first() |> team_id()

        socket
        |> assign(teams: teams, selected_team_id: selected, loading: false, load_error: nil)
        |> load_epics()

      other ->
        handle_load_error(socket, other)
    end
  end

  @impl true
  def handle_event("select_team", %{"team_id" => team_id}, socket) do
    {:noreply,
     socket
     |> assign(selected_team_id: nilify(team_id), panel_mode: nil, panel_form: %{})
     |> load_epics()}
  end

  def handle_event("open_create", _params, socket) do
    if is_nil(socket.assigns.selected_team_id) do
      {:noreply, put_flash(socket, :error, "Select a team first.")}
    else
      {:noreply, assign(socket, panel_mode: :new, panel_form: %{"title" => "", "description" => ""})}
    end
  end

  def handle_event("start_edit", %{"id" => id}, socket) do
    case Enum.find(socket.assigns.epics, &(&1["id"] == id)) do
      nil ->
        {:noreply, socket}

      epic ->
        form = %{"id" => id, "title" => epic["title"], "description" => epic["description"] || ""}
        {:noreply, assign(socket, panel_mode: :edit, panel_form: form)}
    end
  end

  def handle_event("cancel_edit", _params, socket) do
    {:noreply, assign(socket, panel_mode: nil, panel_form: %{})}
  end

  def handle_event("create", %{"title" => title} = params, socket) do
    team_id = socket.assigns.selected_team_id

    if is_nil(team_id) do
      {:noreply, put_flash(socket, :error, "Select a team first.")}
    else
      attrs = %{team_id: team_id, title: title, description: params["description"] || ""}

      case EpicsApi.create(token(socket), attrs) do
        {:ok, _} ->
          {:noreply,
           socket |> assign(panel_mode: nil, panel_form: %{}) |> put_flash(:info, "Epic created.") |> load_epics()}

        {:error, err} ->
          {:noreply, put_api_error(socket, err, "Could not create epic.")}
      end
    end
  end

  def handle_event("save_edit", %{"epic_id" => id, "title" => title} = params, socket) do
    attrs = %{title: title, description: params["description"] || ""}

    case EpicsApi.update(token(socket), id, attrs) do
      {:ok, _} ->
        {:noreply,
         socket |> assign(panel_mode: nil, panel_form: %{}) |> put_flash(:info, "Epic updated.") |> load_epics()}

      {:error, err} ->
        {:noreply, put_api_error(socket, err, "Could not update epic.")}
    end
  end

  def handle_event("delete", %{"id" => id}, socket) do
    case EpicsApi.delete(token(socket), id) do
      {:ok, _} -> {:noreply, socket |> put_flash(:info, "Epic deleted.") |> load_epics()}
      {:error, err} -> {:noreply, put_api_error(socket, err, "Could not delete epic.")}
    end
  end

  defp token(socket), do: socket.assigns.current_user.access_token

  defp load_epics(%{assigns: %{selected_team_id: nil}} = socket), do: assign(socket, epics: [])

  defp load_epics(%{assigns: %{selected_team_id: team_id}} = socket) do
    if socket.redirected do
      socket
    else
      case EpicsApi.list(token(socket), team_id) do
        {:ok, epics} when is_list(epics) -> assign(socket, epics: epics, load_error: nil)
        other -> handle_load_error(socket, other)
      end
    end
  end

  defp handle_load_error(socket, {:error, %{status: 401}}), do: redirect_to_refresh(socket)

  defp handle_load_error(socket, {:error, err}),
    do: assign(socket, loading: false, load_error: err[:detail] || "Could not load epics.")

  defp handle_load_error(socket, _),
    do: assign(socket, loading: false, load_error: "Could not load epics.")

  defp put_api_error(socket, %{status: 401}, _fallback), do: redirect_to_refresh(socket)
  defp put_api_error(socket, err, fallback), do: put_flash(socket, :error, err[:detail] || fallback)

  defp redirect_to_refresh(socket),
    do: redirect(socket, to: ~p"/session/refresh?#{[return_to: @page_path]}")

  defp team_id(nil), do: nil
  defp team_id(team), do: team["id"]

  defp nilify(""), do: nil
  defp nilify(value), do: value

  @impl true
  def render(assigns) do
    ~H"""
    <div class="py-8">
      <div class="flex items-start justify-between gap-4">
        <h1 class="text-2xl font-bold text-gray-900">Epics</h1>
        <button
          :if={not @loading and @teams != []}
          phx-click="open_create"
          class="inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-brand-hover"
        >
          <.icon name="plus" class="h-4 w-4" /> Create epic
        </button>
      </div>

      <p
        :if={@load_error}
        class="mt-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-800 ring-1 ring-red-200"
        role="alert"
      >
        {@load_error}
      </p>

      <p :if={@loading} class="mt-8 text-gray-500">Loading epics…</p>

      <p :if={not @loading and @teams == []} class="mt-8 text-gray-500">
        Create a team first, then add epics to it.
      </p>

      <div :if={not @loading and @teams != []} class="mt-4">
        <form phx-change="select_team">
          <label class="block text-sm font-medium text-gray-700">Team</label>
          <select name="team_id" class="mt-1 w-64 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand">
            <option :for={team <- @teams} value={team["id"]} selected={team["id"] == @selected_team_id}>
              {team["name"]}
            </option>
          </select>
        </form>

        <div class="mt-6 grid grid-cols-1 gap-6 lg:grid-cols-3">
          <%!-- LEFT: epics table --%>
          <div class="lg:col-span-2">
            <p :if={@epics == []} class="text-gray-500">No epics in this team yet.</p>

            <div :if={@epics != []} class="overflow-hidden rounded-xl border border-gray-200 shadow-sm">
              <table class="w-full text-left text-sm">
                <thead>
                  <tr class="border-b border-gray-200 bg-gray-50 text-xs font-medium uppercase tracking-wide text-gray-500">
                    <th class="px-4 py-2.5">Title</th>
                    <th class="px-4 py-2.5">Tickets</th>
                    <th class="px-4 py-2.5">Modified</th>
                    <th class="px-4 py-2.5 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr :for={epic <- @epics} class="hover:bg-gray-50">
                    <td class="px-4 py-3">
                      <div class="font-medium text-gray-900">{epic["title"]}</div>
                      <p :if={epic["description"] not in [nil, ""]} class="mt-0.5 text-xs text-gray-400">
                        {epic["description"]}
                      </p>
                    </td>
                    <td class="px-4 py-3 text-gray-500">{epic["ticketCount"]}</td>
                    <td class="px-4 py-3 text-gray-500">
                      <.local_time at={epic["modifiedAt"]} id={"lt-epic-#{epic["id"]}"} />
                    </td>
                    <td class="px-4 py-3">
                      <div class="flex items-center justify-end gap-1">
                        <button
                          phx-click="start_edit"
                          phx-value-id={epic["id"]}
                          class="rounded-lg px-2.5 py-1.5 text-sm text-gray-600 hover:bg-gray-100"
                        >
                          Edit
                        </button>
                        <button
                          phx-click="delete"
                          phx-value-id={epic["id"]}
                          data-confirm="Delete this epic?"
                          disabled={not epic["canDelete"]}
                          title={epic["canDelete"] == false && "Epic is referenced by tickets" || nil}
                          aria-label="Delete"
                          class="inline-flex items-center justify-center rounded-lg p-1.5 text-error hover:bg-red-50 disabled:cursor-not-allowed disabled:text-gray-300 disabled:hover:bg-transparent"
                        >
                          <.icon name="x" class="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <p :if={@epics != []} class="mt-3 text-xs text-gray-400">
              Delete is disabled while tickets reference the epic.
            </p>
          </div>

          <%!-- RIGHT: create / edit panel --%>
          <div class="lg:col-span-1">
            <div
              :if={@panel_mode == nil}
              class="rounded-xl border border-dashed border-gray-200 p-6 text-center text-sm text-gray-400"
            >
              Select an epic to edit, or create a new one.
            </div>

            <div :if={@panel_mode != nil} class="rounded-xl border border-gray-200 p-5 shadow-sm">
              <h2 class="text-lg font-semibold text-gray-900">
                {(@panel_mode == :new && "Create epic") || "Edit epic"}
              </h2>

              <form
                phx-submit={(@panel_mode == :new && "create") || "save_edit"}
                class="mt-4 space-y-3"
              >
                <input :if={@panel_mode == :edit} type="hidden" name="epic_id" value={@panel_form["id"]} />

                <div>
                  <label class="mb-1 block text-sm font-medium text-gray-700">Title</label>
                  <input
                    type="text"
                    name="title"
                    value={@panel_form["title"]}
                    required
                    class="w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
                  />
                </div>

                <div>
                  <label class="mb-1 block text-sm font-medium text-gray-700">Description (optional)</label>
                  <textarea
                    name="description"
                    rows="4"
                    class="w-full rounded-lg border-gray-300 text-sm focus:border-brand focus:ring-brand"
                  >{@panel_form["description"]}</textarea>
                </div>

                <div class="flex justify-end gap-2 pt-1">
                  <button
                    type="button"
                    phx-click="cancel_edit"
                    class="rounded-lg px-4 py-2 text-sm text-gray-600 hover:bg-gray-100"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    class="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-hover"
                  >
                    Save
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
    """
  end
end

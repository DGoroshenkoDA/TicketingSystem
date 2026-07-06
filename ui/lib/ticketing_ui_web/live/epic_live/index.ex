defmodule TicketingUiWeb.EpicLive.Index do
  use TicketingUiWeb, :live_view

  alias TicketingUi.Api.{EpicsApi, TeamsApi}

  @impl true
  def mount(_params, _session, socket) do
    socket = assign(socket, editing_id: nil, epics: [])
    teams = list_teams(socket)
    selected = teams |> List.first() |> team_id()

    {:ok, socket |> assign(teams: teams, selected_team_id: selected) |> load_epics()}
  end

  @impl true
  def handle_event("select_team", %{"team_id" => team_id}, socket) do
    {:noreply, socket |> assign(selected_team_id: nilify(team_id), editing_id: nil) |> load_epics()}
  end

  def handle_event("create", %{"title" => title} = params, socket) do
    team_id = socket.assigns.selected_team_id

    if is_nil(team_id) do
      {:noreply, put_flash(socket, :error, "Select a team first.")}
    else
      attrs = %{team_id: team_id, title: title, description: params["description"] || ""}

      case EpicsApi.create(token(socket), attrs) do
        {:ok, _} -> {:noreply, socket |> put_flash(:info, "Epic created.") |> load_epics()}
        {:error, err} -> {:noreply, put_flash(socket, :error, err[:detail] || "Could not create epic.")}
      end
    end
  end

  def handle_event("start_edit", %{"id" => id}, socket), do: {:noreply, assign(socket, editing_id: id)}
  def handle_event("cancel_edit", _params, socket), do: {:noreply, assign(socket, editing_id: nil)}

  def handle_event("save_edit", %{"epic_id" => id, "title" => title} = params, socket) do
    attrs = %{title: title, description: params["description"] || ""}

    case EpicsApi.update(token(socket), id, attrs) do
      {:ok, _} ->
        {:noreply, socket |> assign(editing_id: nil) |> put_flash(:info, "Epic updated.") |> load_epics()}

      {:error, err} ->
        {:noreply, put_flash(socket, :error, err[:detail] || "Could not update epic.")}
    end
  end

  def handle_event("delete", %{"id" => id}, socket) do
    case EpicsApi.delete(token(socket), id) do
      {:ok, _} -> {:noreply, socket |> put_flash(:info, "Epic deleted.") |> load_epics()}
      {:error, err} -> {:noreply, put_flash(socket, :error, err[:detail] || "Could not delete epic.")}
    end
  end

  defp token(socket), do: socket.assigns.current_user.access_token

  defp list_teams(socket) do
    case TeamsApi.list(token(socket)) do
      {:ok, teams} when is_list(teams) -> teams
      _ -> []
    end
  end

  defp load_epics(%{assigns: %{selected_team_id: nil}} = socket), do: assign(socket, epics: [])

  defp load_epics(%{assigns: %{selected_team_id: team_id}} = socket) do
    case EpicsApi.list(token(socket), team_id) do
      {:ok, epics} when is_list(epics) -> assign(socket, epics: epics)
      _ -> assign(socket, epics: [])
    end
  end

  defp team_id(nil), do: nil
  defp team_id(team), do: team["id"]

  defp nilify(""), do: nil
  defp nilify(value), do: value

  @impl true
  def render(assigns) do
    ~H"""
    <div class="py-8">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold text-gray-900">Epics</h1>
        <.link navigate={~p"/teams"} class="text-sm font-medium text-brand hover:underline">
          Manage teams
        </.link>
      </div>

      <p :if={@teams == []} class="mt-8 text-gray-500">
        Create a team first, then add epics to it.
      </p>

      <div :if={@teams != []} class="mt-4">
        <form phx-change="select_team">
          <label class="block text-sm font-medium text-gray-700">Team</label>
          <select name="team_id" class="mt-1 w-64 rounded-lg border-gray-300 focus:border-brand focus:ring-brand">
            <option :for={team <- @teams} value={team["id"]} selected={team["id"] == @selected_team_id}>
              {team["name"]}
            </option>
          </select>
        </form>

        <form phx-submit="create" class="mt-6 space-y-3 rounded-lg border border-gray-100 p-4">
          <h2 class="font-medium text-gray-900">New epic</h2>
          <input
            type="text"
            name="title"
            placeholder="Title"
            required
            class="w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
          />
          <textarea
            name="description"
            placeholder="Description (optional)"
            rows="2"
            class="w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
          ></textarea>
          <button type="submit" class="rounded-lg bg-brand px-4 py-2 font-medium text-white hover:bg-brand-hover">
            Add epic
          </button>
        </form>

        <p :if={@epics == []} class="mt-6 text-gray-500">No epics in this team yet.</p>

        <ul :if={@epics != []} class="mt-6 divide-y divide-gray-100 rounded-lg border border-gray-100">
          <li :for={epic <- @epics} class="px-4 py-3">
            <div :if={@editing_id != epic["id"]} class="flex items-start justify-between">
              <div>
                <div class="flex items-center gap-3">
                  <span class="font-medium text-gray-900">{epic["title"]}</span>
                  <span class="text-xs text-gray-400">{epic["ticketCount"]} tickets</span>
                </div>
                <p :if={epic["description"]} class="mt-1 text-sm text-gray-500">{epic["description"]}</p>
              </div>
              <div class="flex items-center gap-3">
                <button phx-click="start_edit" phx-value-id={epic["id"]} class="text-sm text-gray-600 hover:underline">
                  Edit
                </button>
                <button
                  phx-click="delete"
                  phx-value-id={epic["id"]}
                  data-confirm="Delete this epic?"
                  disabled={not epic["canDelete"]}
                  title={epic["canDelete"] == false && "Epic is referenced by tickets" || nil}
                  class="text-sm text-error hover:underline disabled:cursor-not-allowed disabled:text-gray-300 disabled:no-underline"
                >
                  Delete
                </button>
              </div>
            </div>

            <form :if={@editing_id == epic["id"]} id={"epic-edit-#{epic["id"]}"} phx-submit="save_edit" class="space-y-2">
              <input type="hidden" name="epic_id" value={epic["id"]} />
              <input
                type="text"
                name="title"
                value={epic["title"]}
                required
                class="w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
              />
              <textarea
                name="description"
                rows="2"
                class="w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
              >{epic["description"]}</textarea>
              <div class="flex gap-2">
                <button type="submit" class="rounded-lg bg-brand px-3 py-1 text-sm text-white">Save</button>
                <button type="button" phx-click="cancel_edit" class="text-sm text-gray-500">Cancel</button>
              </div>
            </form>
          </li>
        </ul>
      </div>
    </div>
    """
  end
end

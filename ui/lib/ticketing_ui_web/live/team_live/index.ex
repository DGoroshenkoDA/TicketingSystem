defmodule TicketingUiWeb.TeamLive.Index do
  use TicketingUiWeb, :live_view

  alias TicketingUi.Api.TeamsApi

  @page_path "/teams"

  @impl true
  def mount(_params, _session, socket) do
    socket = assign(socket, editing_id: nil, teams: [], loading: true, load_error: nil)

    if connected?(socket) do
      {:ok, load_teams(socket)}
    else
      {:ok, socket}
    end
  end

  @impl true
  def handle_event("create", %{"name" => name}, socket) do
    case TeamsApi.create(token(socket), name) do
      {:ok, _team} ->
        {:noreply, socket |> put_flash(:info, "Team created.") |> load_teams()}

      {:error, err} ->
        {:noreply, put_api_error(socket, err, "Could not create team.")}
    end
  end

  def handle_event("start_edit", %{"id" => id}, socket) do
    {:noreply, assign(socket, editing_id: id)}
  end

  def handle_event("cancel_edit", _params, socket) do
    {:noreply, assign(socket, editing_id: nil)}
  end

  def handle_event("save_edit", %{"team_id" => id, "name" => name}, socket) do
    case TeamsApi.update(token(socket), id, name) do
      {:ok, _team} ->
        {:noreply, socket |> assign(editing_id: nil) |> put_flash(:info, "Team renamed.") |> load_teams()}

      {:error, err} ->
        {:noreply, put_api_error(socket, err, "Could not rename team.")}
    end
  end

  def handle_event("delete", %{"id" => id}, socket) do
    case TeamsApi.delete(token(socket), id) do
      {:ok, _} ->
        {:noreply, socket |> put_flash(:info, "Team deleted.") |> load_teams()}

      {:error, err} ->
        {:noreply, put_api_error(socket, err, "Could not delete team.")}
    end
  end

  defp token(socket), do: socket.assigns.current_user.access_token

  defp load_teams(socket) do
    case TeamsApi.list(token(socket)) do
      {:ok, teams} when is_list(teams) ->
        assign(socket, teams: teams, loading: false, load_error: nil)

      {:error, %{status: 401}} ->
        redirect_to_refresh(socket)

      {:error, err} ->
        assign(socket, loading: false, load_error: err[:detail] || "Could not load teams.")

      _ ->
        assign(socket, teams: [], loading: false)
    end
  end

  defp put_api_error(socket, %{status: 401}, _fallback), do: redirect_to_refresh(socket)
  defp put_api_error(socket, err, fallback), do: put_flash(socket, :error, err[:detail] || fallback)

  defp redirect_to_refresh(socket),
    do: redirect(socket, to: ~p"/session/refresh?#{[return_to: @page_path]}")

  @impl true
  def render(assigns) do
    ~H"""
    <div class="py-8">
      <h1 class="text-2xl font-bold text-gray-900">Teams</h1>

      <p
        :if={@load_error}
        class="mt-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-800 ring-1 ring-red-200"
        role="alert"
      >
        {@load_error}
      </p>

      <p :if={@loading} class="mt-8 text-gray-500">Loading teams…</p>

      <form phx-submit="create" class="mt-4 flex gap-2">
        <input
          type="text"
          name="name"
          placeholder="New team name"
          required
          class="w-64 rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
        />
        <button type="submit" class="rounded-lg bg-brand px-4 py-2 font-medium text-white hover:bg-brand-hover">
          Add team
        </button>
      </form>

      <p :if={not @loading and @teams == []} class="mt-8 text-gray-500">No teams yet. Create your first team above.</p>

      <ul :if={@teams != []} class="mt-6 divide-y divide-gray-100 rounded-lg border border-gray-100">
        <li :for={team <- @teams} class="flex items-center justify-between px-4 py-3">
          <div :if={@editing_id != team["id"]} class="flex items-center gap-3">
            <span class="font-medium text-gray-900">{team["name"]}</span>
            <span class="text-xs text-gray-400">
              {team["epicCount"]} epics · {team["ticketCount"]} tickets
            </span>
          </div>

          <form :if={@editing_id == team["id"]} id={"team-edit-#{team["id"]}"} phx-submit="save_edit" class="flex items-center gap-2">
            <input type="hidden" name="team_id" value={team["id"]} />
            <input
              type="text"
              name="name"
              value={team["name"]}
              required
              class="rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
            />
            <button type="submit" class="rounded-lg bg-brand px-3 py-1 text-sm text-white">Save</button>
            <button type="button" phx-click="cancel_edit" class="text-sm text-gray-500">Cancel</button>
          </form>

          <div :if={@editing_id != team["id"]} class="flex items-center gap-3">
            <button phx-click="start_edit" phx-value-id={team["id"]} class="text-sm text-gray-600 hover:underline">
              Rename
            </button>
            <button
              phx-click="delete"
              phx-value-id={team["id"]}
              data-confirm="Delete this team?"
              disabled={not team["canDelete"]}
              title={team["canDelete"] == false && "Team has tickets or epics" || nil}
              class="text-sm text-error hover:underline disabled:cursor-not-allowed disabled:text-gray-300 disabled:no-underline"
            >
              Delete
            </button>
          </div>
        </li>
      </ul>
    </div>
    """
  end
end

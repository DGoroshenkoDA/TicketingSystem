defmodule TicketingUiWeb.TeamLive.Index do
  use TicketingUiWeb, :live_view

  alias TicketingUi.Api.TeamsApi

  @page_path "/teams"

  @impl true
  def mount(_params, _session, socket) do
    socket =
      assign(socket,
        editing_id: nil,
        show_create: false,
        teams: [],
        loading: true,
        load_error: nil
      )

    if connected?(socket) do
      {:ok, load_teams(socket)}
    else
      {:ok, socket}
    end
  end

  @impl true
  def handle_event("open_create", _params, socket), do: {:noreply, assign(socket, show_create: true)}
  def handle_event("close_create", _params, socket), do: {:noreply, assign(socket, show_create: false)}

  def handle_event("create", %{"name" => name}, socket) do
    case TeamsApi.create(token(socket), name) do
      {:ok, _team} ->
        {:noreply, socket |> assign(show_create: false) |> put_flash(:info, "Team created.") |> load_teams()}

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
      <div class="flex items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Teams</h1>
          <p class="mt-1 text-sm text-gray-500">All verified users can view and manage all teams.</p>
        </div>
        <button
          phx-click="open_create"
          class="inline-flex items-center gap-2 rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-brand-hover"
        >
          <.icon name="plus" class="h-4 w-4" /> Create team
        </button>
      </div>

      <p
        :if={@load_error}
        class="mt-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-800 ring-1 ring-red-200"
        role="alert"
      >
        {@load_error}
      </p>

      <p :if={@loading} class="mt-8 text-gray-500">Loading teams…</p>

      <p :if={not @loading and @teams == []} class="mt-8 text-gray-500">
        No teams yet. Create your first team.
      </p>

      <div :if={@teams != []} class="mt-6 overflow-hidden rounded-xl border border-gray-200 shadow-sm">
        <table class="w-full text-left text-sm">
          <thead>
            <tr class="border-b border-gray-200 bg-gray-50 text-xs font-medium uppercase tracking-wide text-gray-500">
              <th class="px-4 py-2.5">Name</th>
              <th class="px-4 py-2.5">Tickets</th>
              <th class="px-4 py-2.5">Epics</th>
              <th class="px-4 py-2.5">Modified</th>
              <th class="px-4 py-2.5 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <%= for team <- @teams do %>
              <tr :if={@editing_id == team["id"]} class="bg-gray-50">
                <td colspan="5" class="px-4 py-3">
                  <form
                    id={"team-edit-#{team["id"]}"}
                    phx-submit="save_edit"
                    class="flex flex-wrap items-center gap-2"
                  >
                    <input type="hidden" name="team_id" value={team["id"]} />
                    <input
                      type="text"
                      name="name"
                      value={team["name"]}
                      required
                      class="w-64 rounded-lg border-gray-300 py-1.5 text-sm focus:border-brand focus:ring-brand"
                    />
                    <button
                      type="submit"
                      class="rounded-lg bg-brand px-3 py-1.5 text-sm font-medium text-white hover:bg-brand-hover"
                    >
                      Save
                    </button>
                    <button
                      type="button"
                      phx-click="cancel_edit"
                      class="rounded-lg px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100"
                    >
                      Cancel
                    </button>
                  </form>
                </td>
              </tr>

              <tr :if={@editing_id != team["id"]} class="hover:bg-gray-50">
                <td class="px-4 py-3 font-medium text-gray-900">{team["name"]}</td>
                <td class="px-4 py-3 text-gray-500">{team["ticketCount"]}</td>
                <td class="px-4 py-3 text-gray-500">{team["epicCount"]}</td>
                <td class="px-4 py-3 text-gray-500">
                  <.local_time at={team["modifiedAt"]} id={"lt-team-#{team["id"]}"} />
                </td>
                <td class="px-4 py-3">
                  <div class="flex items-center justify-end gap-1">
                    <button
                      phx-click="start_edit"
                      phx-value-id={team["id"]}
                      class="rounded-lg px-2.5 py-1.5 text-sm text-gray-600 hover:bg-gray-100"
                    >
                      Rename
                    </button>
                    <button
                      phx-click="delete"
                      phx-value-id={team["id"]}
                      data-confirm="Delete this team?"
                      disabled={not team["canDelete"]}
                      title={team["canDelete"] == false && "Team has tickets or epics" || nil}
                      class="rounded-lg px-2.5 py-1.5 text-sm text-error hover:bg-red-50 disabled:cursor-not-allowed disabled:text-gray-300 disabled:hover:bg-transparent"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            <% end %>
          </tbody>
        </table>
      </div>

      <p :if={@teams != []} class="mt-3 text-xs text-gray-400">
        Delete is disabled while a team contains tickets or epics.
      </p>

      <%!-- Create-team popup --%>
      <div
        :if={@show_create}
        class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/40 p-4"
      >
        <div class="relative mt-24 w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
          <button
            type="button"
            phx-click="close_create"
            aria-label="Close"
            class="absolute right-4 top-4 text-gray-400 hover:text-gray-600"
          >
            <.icon name="x" class="h-5 w-5" />
          </button>

          <h2 class="text-lg font-semibold text-gray-900">Create team</h2>

          <form phx-submit="create" class="mt-4 space-y-3">
            <div>
              <label class="mb-1 block text-sm font-medium text-gray-700">Team name</label>
              <input
                type="text"
                name="name"
                placeholder="e.g. Platform Engineering"
                required
                autofocus
                class="w-full rounded-lg border-gray-300 focus:border-brand focus:ring-brand"
              />
            </div>
            <div class="flex justify-end gap-2 pt-1">
              <button
                type="button"
                phx-click="close_create"
                class="rounded-lg px-4 py-2 text-sm text-gray-600 hover:bg-gray-100"
              >
                Cancel
              </button>
              <button
                type="submit"
                class="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white hover:bg-brand-hover"
              >
                Create
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
    """
  end
end

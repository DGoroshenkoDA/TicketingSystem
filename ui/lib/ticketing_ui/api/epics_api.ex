defmodule TicketingUi.Api.EpicsApi do
  @moduledoc "Epic endpoints of the .NET API."

  alias TicketingUi.Api.HttpClient

  def list(token, team_id),
    do: HttpClient.get_json("/api/v1/epics", %{teamId: team_id}, token: token)

  def create(token, %{team_id: team_id, title: title, description: description}),
    do:
      HttpClient.post_json(
        "/api/v1/epics",
        %{teamId: team_id, title: title, description: description},
        token: token
      )

  def update(token, id, %{title: title, description: description}),
    do:
      HttpClient.put_json(
        "/api/v1/epics/#{id}",
        %{title: title, description: description},
        token: token
      )

  def delete(token, id),
    do: HttpClient.delete_json("/api/v1/epics/#{id}", token: token)
end

defmodule TicketingUi.Api.TeamsApi do
  @moduledoc "Team endpoints of the .NET API."

  alias TicketingUi.Api.HttpClient

  def list(token), do: HttpClient.get_json("/api/v1/teams", %{}, token: token)

  def create(token, name),
    do: HttpClient.post_json("/api/v1/teams", %{name: name}, token: token)

  def update(token, id, name),
    do: HttpClient.put_json("/api/v1/teams/#{id}", %{name: name}, token: token)

  def delete(token, id),
    do: HttpClient.delete_json("/api/v1/teams/#{id}", token: token)
end

defmodule TicketingUi.Api.CommentsApi do
  @moduledoc "Comment endpoints of the .NET API."

  alias TicketingUi.Api.HttpClient

  def list(token, ticket_id),
    do: HttpClient.get_json("/api/v1/tickets/#{ticket_id}/comments", %{}, token: token)

  def create(token, ticket_id, body),
    do: HttpClient.post_json("/api/v1/tickets/#{ticket_id}/comments", %{body: body}, token: token)
end

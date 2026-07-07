defmodule TicketingUi.Api.HistoryApi do
  @moduledoc "Ticket change-history (audit log) endpoint of the .NET API."

  alias TicketingUi.Api.HttpClient

  @doc """
  Lists a ticket's change history, newest first. Returns the standard
  `{:ok, list}` / `{:error, err}` envelope produced by `ResultParser`.
  """
  def list(token, ticket_id),
    do: HttpClient.get_json("/api/v1/tickets/#{ticket_id}/history", %{}, token: token)
end

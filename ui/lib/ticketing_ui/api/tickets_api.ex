defmodule TicketingUi.Api.TicketsApi do
  @moduledoc "Ticket endpoints of the .NET API."

  alias TicketingUi.Api.HttpClient

  def list(token, filters) do
    params =
      %{
        "teamId" => filters[:team_id],
        "type" => blank_to_nil(filters[:type]),
        "epicId" => blank_to_nil(filters[:epic_id]),
        "search" => blank_to_nil(filters[:search])
      }
      |> Enum.reject(fn {_k, v} -> is_nil(v) end)
      |> Map.new()

    HttpClient.get_json("/api/v1/tickets", params, token: token)
  end

  def get(token, id), do: HttpClient.get_json("/api/v1/tickets/#{id}", %{}, token: token)

  def create(token, attrs) do
    HttpClient.post_json(
      "/api/v1/tickets",
      %{
        teamId: attrs[:team_id],
        type: attrs[:type],
        epicId: blank_to_nil(attrs[:epic_id]),
        title: attrs[:title],
        body: attrs[:body]
      },
      token: token
    )
  end

  def update(token, id, attrs) do
    HttpClient.put_json(
      "/api/v1/tickets/#{id}",
      %{
        teamId: attrs[:team_id],
        type: attrs[:type],
        epicId: blank_to_nil(attrs[:epic_id]),
        title: attrs[:title],
        body: attrs[:body],
        state: attrs[:state]
      },
      token: token
    )
  end

  def update_state(token, id, state),
    do: HttpClient.patch_json("/api/v1/tickets/#{id}/state", %{state: state}, token: token)

  def delete(token, id), do: HttpClient.delete_json("/api/v1/tickets/#{id}", token: token)

  defp blank_to_nil(nil), do: nil
  defp blank_to_nil(""), do: nil
  defp blank_to_nil(value), do: value
end

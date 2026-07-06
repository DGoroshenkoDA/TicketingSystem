defmodule TicketingUiWeb.HealthController do
  use TicketingUiWeb, :controller

  def index(conn, _params) do
    json(conn, %{status: "healthy"})
  end
end

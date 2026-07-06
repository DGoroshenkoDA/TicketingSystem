defmodule TicketingUiWeb.PageController do
  use TicketingUiWeb, :controller

  def home(conn, _params) do
    render(conn, :home)
  end
end

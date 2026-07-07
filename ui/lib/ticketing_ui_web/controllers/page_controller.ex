defmodule TicketingUiWeb.PageController do
  use TicketingUiWeb, :controller

  # The header nav (Board/Teams/Epics) is the single menu, so "/" no longer has
  # its own landing page — authenticated users go straight to the board.
  # Unauthenticated users are bounced to /login by require_authenticated_user
  # before this action ever runs.
  def home(conn, _params), do: redirect(conn, to: ~p"/board")
end

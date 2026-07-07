defmodule TicketingUiWeb.Theme do
  @moduledoc "Theme selection persisted in a cookie and applied via data-theme."

  import Plug.Conn

  @themes ~w(indigo amber)
  @default "indigo"
  @cookie "theme"

  def themes, do: @themes
  def default, do: @default
  def cookie_name, do: @cookie
  def valid?(theme), do: theme in @themes

  # Plug: assigns :theme from the cookie (falling back to the default).
  def fetch_theme(conn, _opts) do
    conn = fetch_cookies(conn)
    theme = conn.cookies[@cookie]
    assign(conn, :theme, if(theme in @themes, do: theme, else: @default))
  end
end

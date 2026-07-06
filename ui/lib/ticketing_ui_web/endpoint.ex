defmodule TicketingUiWeb.Endpoint do
  use Phoenix.Endpoint, otp_app: :ticketing_ui

  # The session will be stored in a cookie and signed.
  @session_options [
    store: :cookie,
    key: "_ticketing_ui_key",
    signing_salt: "CHANGE_ME_SESSION",
    same_site: "Lax"
  ]

  socket "/live", Phoenix.LiveView.Socket,
    websocket: [connect_info: [session: @session_options]],
    longpoll: [connect_info: [session: @session_options]]

  # Serve static assets from priv/static.
  plug Plug.Static,
    at: "/",
    from: :ticketing_ui,
    gzip: false,
    only: TicketingUiWeb.static_paths()

  if code_reloading? do
    socket "/phoenix/live_reload/socket", Phoenix.LiveReloader.Socket
    plug Phoenix.LiveReloader
    plug Phoenix.CodeReloader
  end

  plug Plug.RequestId
  plug Plug.Telemetry, event_prefix: [:phoenix, :endpoint]

  plug Plug.Parsers,
    parsers: [:urlencoded, :multipart, :json],
    pass: ["*/*"],
    json_decoder: Phoenix.json_library()

  plug Plug.MethodOverride
  plug Plug.Head
  plug Plug.Session, @session_options
  plug TicketingUiWeb.Router
end

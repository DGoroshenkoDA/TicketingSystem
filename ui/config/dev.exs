import Config

config :ticketing_ui, TicketingUiWeb.Endpoint,
  http: [ip: {0, 0, 0, 0}, port: 4000],
  check_origin: false,
  code_reloader: true,
  debug_errors: true,
  secret_key_base: "dev_only_secret_key_base_at_least_64_bytes_long_xxxxxxxxxxxxxxxxxxxxxxxx",
  watchers: [
    esbuild: {Esbuild, :install_and_run, [:ticketing_ui, ~w(--sourcemap=inline --watch)]},
    tailwind: {Tailwind, :install_and_run, [:ticketing_ui, ~w(--watch)]}
  ]

config :ticketing_ui, TicketingUiWeb.Endpoint,
  live_reload: [
    patterns: [
      ~r"priv/static/(?!uploads/).*(js|css|png|jpeg|jpg|gif|svg)$",
      ~r"lib/ticketing_ui_web/(controllers|live|components)/.*(ex|heex)$"
    ]
  ]

config :ticketing_ui, dev_routes: true

config :logger, :console, format: "[$level] $message\n"

config :phoenix, :stacktrace_depth, 20
config :phoenix, :plug_init_mode, :runtime

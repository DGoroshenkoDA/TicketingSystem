import Config

# General application configuration.

config :ticketing_ui,
  generators: [timestamp_type: :utc_datetime]

# Endpoint configuration.
config :ticketing_ui, TicketingUiWeb.Endpoint,
  url: [host: "localhost"],
  adapter: Bandit.PhoenixAdapter,
  render_errors: [
    formats: [html: TicketingUiWeb.ErrorHTML, json: TicketingUiWeb.ErrorJSON],
    layout: false
  ],
  pubsub_server: TicketingUi.PubSub,
  live_view: [signing_salt: "CHANGE_ME_SALT"]

# Base URL of the .NET REST API. Overridden at runtime via APP_API_BASE_URL.
config :ticketing_ui, :api_base_url, "http://localhost:5080"

# esbuild configuration.
config :esbuild,
  version: "0.21.5",
  ticketing_ui: [
    args:
      ~w(js/app.js --bundle --target=es2017 --outdir=../priv/static/assets --external:/fonts/* --external:/images/*),
    cd: Path.expand("../assets", __DIR__),
    env: %{"NODE_PATH" => Path.expand("../deps", __DIR__)}
  ]

# tailwind configuration.
config :tailwind,
  version: "3.4.3",
  ticketing_ui: [
    args: ~w(
      --config=tailwind.config.js
      --input=css/app.css
      --output=../priv/static/assets/app.css
    ),
    cd: Path.expand("../assets", __DIR__)
  ]

config :logger, :console,
  format: "$time $metadata[$level] $message\n",
  metadata: [:request_id]

config :phoenix, :json_library, Jason

import_config "#{config_env()}.exs"

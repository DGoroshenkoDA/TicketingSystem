import Config

config :ticketing_ui, TicketingUiWeb.Endpoint,
  http: [ip: {127, 0, 0, 1}, port: 4002],
  secret_key_base: "test_secret_key_base_at_least_64_bytes_long_0000000000000000000000000000",
  server: false

# Point the API client at a local stub by default; tests override per-case (Bypass).
config :ticketing_ui, :api_base_url, "http://localhost:4010"

config :logger, level: :warning

config :phoenix, :plug_init_mode, :runtime

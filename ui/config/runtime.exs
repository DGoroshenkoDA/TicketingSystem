import Config

# Start the Phoenix server when running the release (set in the Docker image).
if System.get_env("PHX_SERVER") do
  config :ticketing_ui, TicketingUiWeb.Endpoint, server: true
end

# Base URL of the .NET REST API (compose-internal address, e.g. http://api:8080).
if api_base_url = System.get_env("APP_API_BASE_URL") do
  config :ticketing_ui, :api_base_url, api_base_url
end

if config_env() == :prod do
  secret_key_base =
    System.get_env("SECRET_KEY_BASE") ||
      raise """
      environment variable SECRET_KEY_BASE is missing.
      You can generate one by calling: mix phx.gen.secret
      """

  host = System.get_env("PHX_HOST") || "localhost"
  port = String.to_integer(System.get_env("PORT") || "4000")

  config :ticketing_ui, TicketingUiWeb.Endpoint,
    url: [host: host, port: 443, scheme: "https"],
    http: [
      ip: {0, 0, 0, 0, 0, 0, 0, 0},
      port: port
    ],
    secret_key_base: secret_key_base
end

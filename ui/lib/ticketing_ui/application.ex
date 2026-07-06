defmodule TicketingUi.Application do
  @moduledoc false

  use Application

  @impl true
  def start(_type, _args) do
    children = [
      TicketingUiWeb.Telemetry,
      {DNSCluster, query: Application.get_env(:ticketing_ui, :dns_cluster_query) || :ignore},
      {Phoenix.PubSub, name: TicketingUi.PubSub},
      TicketingUiWeb.Endpoint
    ]

    opts = [strategy: :one_for_one, name: TicketingUi.Supervisor]
    Supervisor.start_link(children, opts)
  end

  @impl true
  def config_change(changed, _new, removed) do
    TicketingUiWeb.Endpoint.config_change(changed, removed)
    :ok
  end
end

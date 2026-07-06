defmodule TicketingUi.MixProject do
  use Mix.Project

  def project do
    [
      app: :ticketing_ui,
      version: "0.1.0",
      elixir: "~> 1.15",
      elixirc_paths: elixirc_paths(Mix.env()),
      start_permanent: Mix.env() == :prod,
      aliases: aliases(),
      deps: deps(),
      releases: releases()
    ]
  end

  def application do
    [
      mod: {TicketingUi.Application, []},
      extra_applications: [:logger, :runtime_tools]
    ]
  end

  defp releases do
    [
      ticketing_ui: [
        include_executables_for: [:unix]
      ]
    ]
  end

  defp elixirc_paths(:test), do: ["lib", "test/support"]
  defp elixirc_paths(_), do: ["lib"]

  defp deps do
    [
      {:phoenix, "~> 1.7.14"},
      {:phoenix_html, "~> 4.1"},
      {:phoenix_live_reload, "~> 1.5", only: :dev},
      {:phoenix_live_view, "~> 1.0"},
      {:telemetry_metrics, "~> 1.0"},
      {:telemetry_poller, "~> 1.0"},
      {:jason, "~> 1.2"},
      {:bandit, "~> 1.5"},
      {:req, "~> 0.5"},
      {:dns_cluster, "~> 0.1.1"},
      {:esbuild, "~> 0.8", runtime: Mix.env() == :dev},
      {:tailwind, "~> 0.2", runtime: Mix.env() == :dev},
      {:bypass, "~> 2.1", only: :test},
      {:lazy_html, ">= 0.1.0", only: :test}
    ]
  end

  defp aliases do
    [
      setup: ["deps.get", "assets.setup", "assets.build"],
      "assets.setup": ["tailwind.install --if-missing", "esbuild.install --if-missing"],
      "assets.build": ["tailwind ticketing_ui", "esbuild ticketing_ui"],
      "assets.deploy": [
        "tailwind ticketing_ui --minify",
        "esbuild ticketing_ui --minify",
        "phx.digest"
      ]
    ]
  end
end

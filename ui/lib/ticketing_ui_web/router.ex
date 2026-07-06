defmodule TicketingUiWeb.Router do
  use TicketingUiWeb, :router

  pipeline :browser do
    plug :accepts, ["html"]
    plug :fetch_session
    plug :fetch_live_flash
    plug :put_root_layout, html: {TicketingUiWeb.Layouts, :root}
    plug :protect_from_forgery
    plug :put_secure_browser_headers
  end

  pipeline :api do
    plug :accepts, ["json"]
  end

  scope "/", TicketingUiWeb do
    pipe_through :browser

    get "/", PageController, :home
  end

  # Liveness/readiness endpoint (public).
  scope "/", TicketingUiWeb do
    pipe_through :api

    get "/health", HealthController, :index
  end
end

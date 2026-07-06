defmodule TicketingUiWeb.Router do
  use TicketingUiWeb, :router

  import TicketingUiWeb.Auth

  pipeline :browser do
    plug :accepts, ["html"]
    plug :fetch_session
    plug :fetch_live_flash
    plug :put_root_layout, html: {TicketingUiWeb.Layouts, :root}
    plug :protect_from_forgery
    plug :put_secure_browser_headers
    plug :fetch_current_user
  end

  pipeline :api do
    plug :accepts, ["json"]
  end

  # Guest-only routes (signed-in users are redirected home).
  scope "/", TicketingUiWeb do
    pipe_through [:browser, :redirect_if_authenticated]

    get "/login", AuthController, :login_new
    post "/login", AuthController, :login_create
    get "/signup", AuthController, :signup_new
    post "/signup", AuthController, :signup_create
  end

  # Logout (available while signed in).
  scope "/", TicketingUiWeb do
    pipe_through :browser

    delete "/logout", AuthController, :delete
    post "/logout", AuthController, :delete
  end

  # Authenticated business routes.
  scope "/", TicketingUiWeb do
    pipe_through [:browser, :require_authenticated_user]

    get "/", PageController, :home

    live_session :authenticated,
      on_mount: [{TicketingUiWeb.Auth, :ensure_authenticated}] do
      live "/teams", TeamLive.Index, :index
      live "/epics", EpicLive.Index, :index
    end
  end

  # Public liveness/readiness endpoint.
  scope "/", TicketingUiWeb do
    pipe_through :api

    get "/health", HealthController, :index
  end
end

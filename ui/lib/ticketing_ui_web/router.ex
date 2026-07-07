defmodule TicketingUiWeb.Router do
  use TicketingUiWeb, :router

  import TicketingUiWeb.Auth
  import TicketingUiWeb.Theme

  pipeline :browser do
    plug :accepts, ["html"]
    plug :fetch_session
    plug :fetch_live_flash
    plug :put_root_layout, html: {TicketingUiWeb.Layouts, :root}
    plug :protect_from_forgery
    plug :put_secure_browser_headers
    plug :fetch_current_user
    plug :fetch_theme
  end

  # Refreshes rotated tokens before the auth guard runs on browser requests.
  pipeline :refresh_tokens do
    plug :refresh_token_if_needed
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

  # Email verification landing + resend (works whether signed in or not).
  scope "/", TicketingUiWeb do
    pipe_through :browser

    get "/verify", AuthController, :verify
    post "/resend-verification", AuthController, :resend
  end

  # On-demand token refresh for connected LiveViews after a 401. Reachable by an
  # authenticated user; not behind redirect_if_authenticated, and deliberately
  # not behind :refresh_tokens (the action owns the refresh).
  scope "/", TicketingUiWeb do
    pipe_through [:browser, :require_authenticated_user]

    get "/session/refresh", AuthController, :refresh
  end

  # Logout (available while signed in).
  scope "/", TicketingUiWeb do
    pipe_through :browser

    delete "/logout", AuthController, :delete
    post "/logout", AuthController, :delete
  end

  # Authenticated business routes.
  scope "/", TicketingUiWeb do
    pipe_through [:browser, :refresh_tokens, :require_authenticated_user]

    get "/", PageController, :home

    get "/profile", ProfileController, :show
    post "/profile", ProfileController, :update
    post "/profile/password", ProfileController, :change_password
    post "/profile/theme", ProfileController, :set_theme

    live_session :authenticated,
      on_mount: [{TicketingUiWeb.Auth, :ensure_authenticated}] do
      live "/board", BoardLive.Index, :index
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

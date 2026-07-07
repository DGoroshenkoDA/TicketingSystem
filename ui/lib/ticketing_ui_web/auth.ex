defmodule TicketingUiWeb.Auth do
  @moduledoc """
  Session management for the UI. The access/refresh tokens and the user profile
  live in the server-side session cookie (never in the URL or localStorage).
  """

  import Plug.Conn
  import Phoenix.Controller

  alias TicketingUi.Api.AuthApi
  alias Phoenix.LiveView

  @doc "Assigns :current_user from the session (nil when signed out)."
  def fetch_current_user(conn, _opts) do
    assign(conn, :current_user, current_user_from_session(conn))
  end

  @doc "Blocks unauthenticated access to business routes."
  def require_authenticated_user(conn, _opts) do
    if conn.assigns[:current_user] do
      conn
    else
      conn
      |> put_flash(:error, "Please sign in to continue.")
      |> redirect(to: "/login")
      |> halt()
    end
  end

  @doc "Sends already-signed-in users away from login/signup."
  def redirect_if_authenticated(conn, _opts) do
    if conn.assigns[:current_user] do
      conn |> redirect(to: "/") |> halt()
    else
      conn
    end
  end

  @doc """
  Refreshes the access/refresh token pair when the access token is missing,
  expired, or about to expire (~60s). The .NET API rotates refresh tokens, so
  the new pair must be persisted back into the session cookie — which is only
  possible from a plug/controller (a connected LiveView cannot set cookies).

  On success the session (and :current_user assign) are updated with the new
  tokens. On failure the session is cleared so the downstream auth guard
  redirects. This plug never crashes the request pipeline.
  """
  def refresh_token_if_needed(conn, _opts) do
    refresh_token = get_session(conn, :refresh_token)

    if is_binary(refresh_token) and refresh_token != "" and access_token_stale?(conn) do
      case safe_refresh(refresh_token) do
        {:ok, auth} ->
          conn = store_tokens(conn, auth)
          assign(conn, :current_user, current_user_from_session(conn))

        _ ->
          conn
          |> configure_session(renew: true)
          |> clear_session()
          |> assign(:current_user, nil)
      end
    else
      conn
    end
  rescue
    _ -> conn
  end

  @doc "Stores tokens + user in the session after a successful login."
  def log_in(conn, auth) do
    conn
    |> configure_session(renew: true)
    |> store_tokens(auth)
  end

  @doc "Revokes the refresh token via the API and clears the session."
  def log_out(conn) do
    refresh_token = get_session(conn, :refresh_token)
    access_token = get_session(conn, :access_token)

    if is_binary(refresh_token) and refresh_token != "" do
      _ = AuthApi.logout(refresh_token, access_token)
    end

    conn
    |> configure_session(renew: true)
    |> clear_session()
  end

  @doc "For LiveView live_session: ensures an authenticated user."
  def on_mount(:ensure_authenticated, _params, session, socket) do
    case build_user(session) do
      nil ->
        {:halt,
         socket
         |> LiveView.put_flash(:error, "Please sign in to continue.")
         |> LiveView.redirect(to: "/login")}

      user ->
        {:cont, Phoenix.Component.assign(socket, :current_user, user)}
    end
  end

  # Persists a normalized auth payload (see AuthApi.normalize_auth/1) into the
  # session. User fields are only overwritten when present, so a refresh
  # response that omits the user does not wipe the signed-in identity.
  defp store_tokens(conn, auth) do
    conn
    |> put_session(:access_token, auth[:access_token])
    |> put_session(:refresh_token, auth[:refresh_token])
    |> put_session(:access_expires_at, auth[:access_expires_at])
    |> put_session(:refresh_expires_at, auth[:refresh_expires_at])
    |> maybe_put_user(auth[:user])
  end

  defp maybe_put_user(conn, %{id: id} = user) when is_binary(id) and id != "" do
    conn
    |> put_session(:user_id, id)
    |> put_session(:user_email, user[:email])
    |> put_session(:user_name, user[:display_name])
  end

  defp maybe_put_user(conn, _), do: conn

  # True when the access token is missing, unparseable, expired, or within ~60s
  # of expiry. Missing/unparseable is treated as stale so we refresh defensively.
  defp access_token_stale?(conn) do
    case get_session(conn, :access_expires_at) do
      iso when is_binary(iso) and iso != "" ->
        case DateTime.from_iso8601(iso) do
          {:ok, expires_at, _offset} ->
            threshold = DateTime.add(DateTime.utc_now(), 60, :second)
            DateTime.compare(expires_at, threshold) != :gt

          _ ->
            true
        end

      _ ->
        true
    end
  end

  defp safe_refresh(refresh_token) do
    AuthApi.refresh(refresh_token)
  rescue
    _ -> {:error, :exception}
  end

  defp current_user_from_session(conn) do
    build_user(%{
      "user_id" => get_session(conn, :user_id),
      "user_email" => get_session(conn, :user_email),
      "user_name" => get_session(conn, :user_name),
      "access_token" => get_session(conn, :access_token)
    })
  end

  defp build_user(%{"user_id" => id} = session) when is_binary(id) and id != "" do
    %{
      id: id,
      email: session["user_email"],
      name: session["user_name"],
      access_token: session["access_token"]
    }
  end

  defp build_user(_), do: nil
end

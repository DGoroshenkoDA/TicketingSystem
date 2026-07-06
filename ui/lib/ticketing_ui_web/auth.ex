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

  @doc "Stores tokens + user in the session after a successful login."
  def log_in(conn, auth) do
    user = auth.user

    conn
    |> configure_session(renew: true)
    |> put_session(:access_token, auth.access_token)
    |> put_session(:refresh_token, auth.refresh_token)
    |> put_session(:user_id, user.id)
    |> put_session(:user_email, user.email)
    |> put_session(:user_name, user.display_name)
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

  @doc "Session map handed to live_session."
  def session(conn) do
    %{
      "access_token" => get_session(conn, :access_token),
      "refresh_token" => get_session(conn, :refresh_token),
      "user_id" => get_session(conn, :user_id),
      "user_email" => get_session(conn, :user_email),
      "user_name" => get_session(conn, :user_name)
    }
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

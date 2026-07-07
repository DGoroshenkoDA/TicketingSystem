defmodule TicketingUiWeb.AuthController do
  use TicketingUiWeb, :controller

  alias TicketingUi.Api.AuthApi
  alias TicketingUiWeb.Auth

  def login_new(conn, _params) do
    render(conn, :login, error: nil, email: "")
  end

  def login_create(conn, params) do
    email = params["email"] || ""
    password = params["password"] || ""

    case AuthApi.login(email, password) do
      {:ok, auth} ->
        conn
        |> Auth.log_in(auth)
        |> put_flash(:info, "Welcome back!")
        |> redirect(to: "/")

      {:error, err} ->
        conn
        |> render(:login, error: err[:detail] || "Login failed.", email: email)
    end
  end

  def signup_new(conn, _params) do
    render(conn, :signup, error: nil, values: %{})
  end

  def signup_create(conn, params) do
    attrs = %{
      email: params["email"] || "",
      display_name: params["display_name"] || "",
      password: params["password"] || "",
      password_confirm: params["password_confirm"] || ""
    }

    case AuthApi.signup(attrs) do
      {:ok, _user} ->
        conn
        |> put_flash(:info, "Account created. Check your email for a verification link, then sign in.")
        |> redirect(to: "/login")

      {:error, err} ->
        conn
        |> render(:signup, error: err[:detail] || "Sign up failed.", values: params)
    end
  end

  def delete(conn, _params) do
    conn
    |> Auth.log_out()
    |> put_flash(:info, "Signed out.")
    |> redirect(to: "/login")
  end

  def resend(conn, params) do
    _ = AuthApi.resend(params["email"] || "")

    conn
    |> put_flash(:info, "If that account exists and is unverified, a new verification email was sent.")
    |> redirect(to: "/login")
  end

  def verify(conn, params) do
    case AuthApi.verify(params["token"] || "") do
      {:ok, _} -> render(conn, :verified)
      {:error, _} -> render(conn, :verify_error)
    end
  end

  @doc """
  On-demand refresh used by connected LiveViews after a 401. Refreshes the
  rotated token pair, re-stores it in the session cookie, and redirects to a
  sanitized local `return_to`. On failure it logs out and sends the user to
  the login page.
  """
  def refresh(conn, params) do
    return_to = sanitize_return_to(params["return_to"])

    case get_session(conn, :refresh_token) do
      token when is_binary(token) and token != "" ->
        case AuthApi.refresh(token) do
          {:ok, auth} ->
            conn
            |> Auth.log_in(auth)
            |> redirect(to: return_to)

          {:error, _} ->
            expired(conn)
        end

      _ ->
        expired(conn)
    end
  end

  defp expired(conn) do
    conn
    |> Auth.log_out()
    |> put_flash(:error, "Your session has expired. Please sign in again.")
    |> redirect(to: "/login")
  end

  # Only allow local paths ("/..."), never protocol-relative ("//" or "/\\").
  defp sanitize_return_to(path) when is_binary(path) do
    if String.starts_with?(path, "/") and
         not String.starts_with?(path, "//") and
         not String.starts_with?(path, "/\\") do
      path
    else
      "/board"
    end
  end

  defp sanitize_return_to(_), do: "/board"
end

defmodule TicketingUiWeb.AuthController do
  use TicketingUiWeb, :controller

  alias TicketingUi.Api.AuthApi
  alias TicketingUiWeb.Auth

  def login_new(conn, _params) do
    render(conn, :login, error: nil, email: "")
  end

  def login_create(conn, %{"email" => email, "password" => password}) do
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
end

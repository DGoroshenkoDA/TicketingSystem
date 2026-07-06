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
        |> put_flash(:info, "Account created. Please sign in.")
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
end

defmodule TicketingUiWeb.AuthControllerTest do
  use TicketingUiWeb.ConnCase, async: false

  setup do
    bypass = Bypass.open()
    previous = Application.get_env(:ticketing_ui, :api_base_url)
    Application.put_env(:ticketing_ui, :api_base_url, "http://localhost:#{bypass.port}")
    on_exit(fn -> Application.put_env(:ticketing_ui, :api_base_url, previous) end)
    {:ok, bypass: bypass}
  end

  defp json(conn, status, payload) do
    conn
    |> Plug.Conn.put_resp_header("content-type", "application/json")
    |> Plug.Conn.resp(status, Jason.encode!(payload))
  end

  test "successful login stores tokens in session and redirects home", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/auth/login", fn c ->
      json(c, 200, %{
        success: true,
        data: %{
          accessToken: "access-token",
          refreshToken: "refresh-token",
          accessExpiresAt: "2026-01-01T00:00:00Z",
          refreshExpiresAt: "2026-01-15T00:00:00Z",
          user: %{id: "u-1", email: "a@b.com", displayName: "Alice"}
        }
      })
    end)

    conn = post(conn, "/login", %{"email" => "a@b.com", "password" => "password123"})

    assert redirected_to(conn) == "/"
    assert get_session(conn, :access_token) == "access-token"
    assert get_session(conn, :refresh_token) == "refresh-token"
    assert get_session(conn, :user_email) == "a@b.com"
  end

  test "invalid credentials re-render the login form with an error", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/auth/login", fn c ->
      json(c, 401, %{success: false, code: "Auth.InvalidCredentials", detail: "Invalid email or password."})
    end)

    conn = post(conn, "/login", %{"email" => "a@b.com", "password" => "wrong"})

    assert html_response(conn, 200) =~ "Invalid email or password."
    refute get_session(conn, :access_token)
  end

  test "successful signup redirects to login", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/auth/signup", fn c ->
      json(c, 201, %{success: true, data: %{id: "u-2", email: "new@b.com", displayName: "New User"}})
    end)

    conn =
      post(conn, "/signup", %{
        "email" => "new@b.com",
        "display_name" => "New User",
        "password" => "password123",
        "password_confirm" => "password123"
      })

    assert redirected_to(conn) == "/login"
  end

  test "duplicate email on signup re-renders with an error", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/auth/signup", fn c ->
      json(c, 409, %{success: false, code: "Auth.EmailTaken", detail: "This email address is already registered."})
    end)

    conn =
      post(conn, "/signup", %{
        "email" => "dup@b.com",
        "display_name" => "Dup",
        "password" => "password123",
        "password_confirm" => "password123"
      })

    assert html_response(conn, 200) =~ "already registered"
  end

  test "verify with a valid token shows the verified screen", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "GET", "/api/v1/auth/verify", fn c ->
      json(c, 200, %{success: true, data: %{verified: true}})
    end)

    conn = get(conn, "/verify", %{"token" => "good-token"})

    assert html_response(conn, 200) =~ "Email verified"
  end

  test "verify with an invalid token shows the error screen", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "GET", "/api/v1/auth/verify", fn c ->
      json(c, 400, %{success: false, code: "Auth.InvalidVerificationToken", detail: "invalid"})
    end)

    conn = get(conn, "/verify", %{"token" => "bad"})

    assert html_response(conn, 200) =~ "Expired or invalid link"
  end

  test "resend verification redirects to login", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/auth/resend-verification", fn c ->
      json(c, 200, %{success: true, data: %{sent: true}})
    end)

    conn = post(conn, "/resend-verification", %{"email" => "a@b.com"})

    assert redirected_to(conn) == "/login"
  end
end

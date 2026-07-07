defmodule TicketingUiWeb.ProfileControllerTest do
  use TicketingUiWeb.ConnCase, async: false

  setup do
    bypass = Bypass.open()
    previous = Application.get_env(:ticketing_ui, :api_base_url)
    Application.put_env(:ticketing_ui, :api_base_url, "http://localhost:#{bypass.port}")
    on_exit(fn -> Application.put_env(:ticketing_ui, :api_base_url, previous) end)
    {:ok, bypass: bypass}
  end

  defp authed(conn) do
    Plug.Test.init_test_session(conn, %{
      "user_id" => "u-1",
      "user_email" => "a@b.com",
      "user_name" => "Alice",
      "access_token" => "token",
      "refresh_token" => "refresh",
      "access_expires_at" => "2099-01-01T00:00:00Z"
    })
  end

  defp json(conn, status, payload) do
    conn
    |> Plug.Conn.put_resp_header("content-type", "application/json")
    |> Plug.Conn.resp(status, Jason.encode!(payload))
  end

  test "shows the profile", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "GET", "/api/v1/profile", fn c ->
      json(c, 200, %{success: true, data: %{email: "a@b.com", displayName: "Alice"}})
    end)

    conn = get(authed(conn), "/profile")

    body = html_response(conn, 200)
    assert body =~ "Profile"
    assert body =~ "Alice"
  end

  test "updating the display name persists it and redirects", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "PUT", "/api/v1/profile", fn c ->
      json(c, 200, %{success: true, data: %{email: "a@b.com", displayName: "Alice B"}})
    end)

    conn = post(authed(conn), "/profile", %{"display_name" => "Alice B"})

    assert redirected_to(conn) == "/profile"
    assert get_session(conn, :user_name) == "Alice B"
  end

  test "changing the password succeeds", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/profile/password", fn c ->
      json(c, 200, %{success: true, data: %{changed: true}})
    end)

    conn =
      post(authed(conn), "/profile/password", %{
        "current_password" => "old-password",
        "new_password" => "new-password"
      })

    assert redirected_to(conn) == "/profile"
    assert Phoenix.Flash.get(conn.assigns.flash, :info) =~ "Password changed."
  end

  test "a rejected password change re-flashes the API error", %{conn: conn, bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/profile/password", fn c ->
      json(c, 400, %{success: false, code: "Auth.InvalidPassword", detail: "Current password is incorrect."})
    end)

    conn =
      post(authed(conn), "/profile/password", %{
        "current_password" => "wrong",
        "new_password" => "new-password"
      })

    assert redirected_to(conn) == "/profile"
    assert Phoenix.Flash.get(conn.assigns.flash, :error) =~ "incorrect"
  end

  test "setting a valid theme stores a cookie and redirects", %{conn: conn} do
    conn = post(authed(conn), "/profile/theme", %{"theme" => "emerald"})

    assert redirected_to(conn) == "/profile"
    assert conn.resp_cookies["theme"].value == "emerald"
  end

  test "setting a theme with a missing param does not crash", %{conn: conn} do
    conn = post(authed(conn), "/profile/theme", %{})

    assert redirected_to(conn) == "/profile"
    refute Map.has_key?(conn.resp_cookies, "theme")
  end
end

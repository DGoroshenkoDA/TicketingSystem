defmodule TicketingUiWeb.TeamLiveTest do
  use TicketingUiWeb.ConnCase, async: false

  import Phoenix.LiveViewTest

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
      # Far-future expiry so the refresh_token_if_needed plug is a no-op.
      "access_expires_at" => "2099-01-01T00:00:00Z"
    })
  end

  defp json(conn, status, payload) do
    conn
    |> Plug.Conn.put_resp_header("content-type", "application/json")
    |> Plug.Conn.resp(status, Jason.encode!(payload))
  end

  defp team(name, opts \\ []) do
    %{
      id: Keyword.get(opts, :id, "t-1"),
      name: name,
      createdAt: "2026-01-01T00:00:00Z",
      modifiedAt: "2026-01-01T00:00:00Z",
      epicCount: Keyword.get(opts, :epics, 0),
      ticketCount: Keyword.get(opts, :tickets, 0),
      canDelete: Keyword.get(opts, :can_delete, true)
    }
  end

  test "lists teams on mount", %{conn: conn, bypass: bypass} do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 200, %{success: true, data: [team("Alpha"), team("Beta", id: "t-2", epics: 2, can_delete: false)]})
    end)

    {:ok, _view, html} = live(authed(conn), "/teams")

    assert html =~ "Alpha"
    assert html =~ "Beta"
  end

  test "creating a team posts to the API and reloads", %{conn: conn, bypass: bypass} do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 200, %{success: true, data: [team("Alpha")]})
    end)

    Bypass.expect_once(bypass, "POST", "/api/v1/teams", fn c ->
      json(c, 201, %{success: true, data: team("Gamma", id: "t-3")})
    end)

    {:ok, view, _html} = live(authed(conn), "/teams")

    # Creating a team now happens in a popup opened from the "Create team" button.
    view |> element("button", "Create team") |> render_click()
    html = view |> form("form[phx-submit=create]", %{name: "Gamma"}) |> render_submit()

    assert html =~ "Team created."
  end
end

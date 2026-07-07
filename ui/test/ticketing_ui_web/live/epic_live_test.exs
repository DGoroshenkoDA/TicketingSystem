defmodule TicketingUiWeb.EpicLiveTest do
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
      "access_expires_at" => "2099-01-01T00:00:00Z"
    })
  end

  defp json(conn, status, payload) do
    conn
    |> Plug.Conn.put_resp_header("content-type", "application/json")
    |> Plug.Conn.resp(status, Jason.encode!(payload))
  end

  defp epic(title, opts \\ []) do
    %{
      id: Keyword.get(opts, :id, "e-1"),
      teamId: "t-1",
      title: title,
      description: Keyword.get(opts, :description, "Desc"),
      ticketCount: Keyword.get(opts, :tickets, 0),
      canDelete: Keyword.get(opts, :can_delete, true)
    }
  end

  defp stub_teams(bypass) do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 200, %{
        success: true,
        data: [%{id: "t-1", name: "Alpha", epicCount: 0, ticketCount: 0, canDelete: true}]
      })
    end)
  end

  test "renders epics for the selected team", %{conn: conn, bypass: bypass} do
    stub_teams(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/epics", fn c ->
      json(c, 200, %{success: true, data: [epic("Login flow")]})
    end)

    {:ok, _view, html} = live(authed(conn), "/epics")

    assert html =~ "Epics"
    assert html =~ "Login flow"
  end

  test "creating an epic posts and reloads", %{conn: conn, bypass: bypass} do
    stub_teams(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/epics", fn c ->
      json(c, 200, %{success: true, data: []})
    end)

    Bypass.expect_once(bypass, "POST", "/api/v1/epics", fn c ->
      json(c, 201, %{success: true, data: epic("Brand new")})
    end)

    {:ok, view, _html} = live(authed(conn), "/epics")

    html =
      view
      |> form("form[phx-submit=create]", %{title: "Brand new", description: "Because"})
      |> render_submit()

    assert html =~ "Epic created."
  end

  test "deleting an epic posts and reloads", %{conn: conn, bypass: bypass} do
    stub_teams(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/epics", fn c ->
      json(c, 200, %{success: true, data: [epic("Removable")]})
    end)

    Bypass.expect_once(bypass, "DELETE", "/api/v1/epics/e-1", fn c ->
      json(c, 200, %{success: true, data: %{deleted: true}})
    end)

    {:ok, view, _html} = live(authed(conn), "/epics")

    html =
      view
      |> element("button[phx-click='delete'][phx-value-id='e-1']")
      |> render_click()

    assert html =~ "Epic deleted."
  end
end

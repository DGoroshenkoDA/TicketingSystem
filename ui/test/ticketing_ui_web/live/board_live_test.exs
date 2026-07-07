defmodule TicketingUiWeb.BoardLiveTest do
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

  defp stub_common(bypass) do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 200, %{success: true, data: [%{id: "t-1", name: "Alpha", epicCount: 0, ticketCount: 0, canDelete: true}]})
    end)

    Bypass.expect(bypass, "GET", "/api/v1/epics", fn c ->
      json(c, 200, %{success: true, data: []})
    end)
  end

  defp ticket(overrides \\ %{}) do
    Map.merge(
      %{
        id: "tk-1",
        teamId: "t-1",
        type: "bug",
        state: "new",
        epicId: nil,
        epicTitle: nil,
        title: "First ticket",
        body: "Body",
        createdBy: "u-1",
        createdByName: "Alice",
        createdAt: "2026-01-01T00:00:00Z",
        modifiedAt: "2026-01-01T00:00:00Z"
      },
      overrides
    )
  end

  test "renders columns and a card", %{conn: conn, bypass: bypass} do
    stub_common(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      json(c, 200, %{success: true, data: [ticket()]})
    end)

    {:ok, _view, html} = live(authed(conn), "/board")

    assert html =~ "Board"
    assert html =~ "In progress"
    assert html =~ "First ticket"
  end

  test "creating a ticket posts and reloads", %{conn: conn, bypass: bypass} do
    stub_common(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      json(c, 200, %{success: true, data: []})
    end)

    Bypass.expect_once(bypass, "POST", "/api/v1/tickets", fn c ->
      json(c, 201, %{success: true, data: ticket(%{title: "New one"})})
    end)

    {:ok, view, _html} = live(authed(conn), "/board")

    view |> element("button", "New ticket") |> render_click()

    html =
      view
      |> form("form[phx-submit=save]", %{
        team_id: "t-1",
        type: "bug",
        epic_id: "",
        title: "New one",
        body: "Body"
      })
      |> render_submit()

    assert html =~ "Ticket saved."
  end

  test "moving a ticket patches its state", %{conn: conn, bypass: bypass} do
    stub_common(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      json(c, 200, %{success: true, data: [ticket()]})
    end)

    Bypass.expect_once(bypass, "PATCH", "/api/v1/tickets/tk-1/state", fn c ->
      json(c, 200, %{success: true, data: ticket(%{state: "done"})})
    end)

    {:ok, view, _html} = live(authed(conn), "/board")

    html = render_hook(view, "move_ticket", %{"id" => "tk-1", "state" => "done"})

    assert html =~ "First ticket"
  end

  test "a failed move shows an error", %{conn: conn, bypass: bypass} do
    stub_common(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      json(c, 200, %{success: true, data: [ticket()]})
    end)

    Bypass.expect_once(bypass, "PATCH", "/api/v1/tickets/tk-1/state", fn c ->
      json(c, 400, %{success: false, code: "Ticket.InvalidState", detail: "Move failed."})
    end)

    {:ok, view, _html} = live(authed(conn), "/board")

    html = render_hook(view, "move_ticket", %{"id" => "tk-1", "state" => "done"})

    assert html =~ "Move failed."
  end

  defp comment(body \\ "Existing comment") do
    %{id: "c-1", ticketId: "tk-1", authorId: "u-1", authorName: "Alice", body: body, createdAt: "2026-01-01T00:00:00Z"}
  end

  test "opening a ticket shows comments and adding one posts", %{conn: conn, bypass: bypass} do
    stub_common(bypass)

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      json(c, 200, %{success: true, data: [ticket()]})
    end)

    Bypass.expect(bypass, "GET", "/api/v1/tickets/tk-1", fn c ->
      json(c, 200, %{success: true, data: ticket()})
    end)

    Bypass.expect(bypass, "GET", "/api/v1/tickets/tk-1/comments", fn c ->
      json(c, 200, %{success: true, data: [comment()]})
    end)

    Bypass.expect_once(bypass, "POST", "/api/v1/tickets/tk-1/comments", fn c ->
      json(c, 201, %{success: true, data: comment("New")})
    end)

    {:ok, view, _html} = live(authed(conn), "/board")

    html = view |> element("button[data-ticket-id='tk-1']") |> render_click()
    assert html =~ "Existing comment"

    html2 = view |> form("#comment-form", %{body: "New"}) |> render_submit()
    assert html2 =~ "Existing comment"
  end

  test "type/epic/search filters re-query tickets together (AND semantics)", %{conn: conn, bypass: bypass} do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 200, %{success: true, data: [%{id: "t-1", name: "Alpha", epicCount: 0, ticketCount: 0, canDelete: true}]})
    end)

    Bypass.expect(bypass, "GET", "/api/v1/epics", fn c ->
      json(c, 200, %{success: true, data: [%{id: "e-1", title: "Epic 1", ticketCount: 0, canDelete: true}]})
    end)

    parent = self()

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      c = Plug.Conn.fetch_query_params(c)
      send(parent, {:tickets_query, c.query_params})
      json(c, 200, %{success: true, data: []})
    end)

    {:ok, view, _html} = live(authed(conn), "/board")

    # Drain the initial load's request (team only).
    assert_receive {:tickets_query, initial}
    assert initial["teamId"] == "t-1"

    render_hook(view, "filter", %{"type" => "bug", "epic_id" => "e-1", "search" => "boom"})

    # All active filters are sent on a single request (AND semantics).
    assert_receive {:tickets_query, params}
    assert params["teamId"] == "t-1"
    assert params["type"] == "bug"
    assert params["epicId"] == "e-1"
    assert params["search"] == "boom"
  end

  test "switching team resets the epic filter and reloads for the new team", %{conn: conn, bypass: bypass} do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 200, %{
        success: true,
        data: [
          %{id: "t-1", name: "Alpha", epicCount: 0, ticketCount: 0, canDelete: true},
          %{id: "t-2", name: "Beta", epicCount: 0, ticketCount: 0, canDelete: true}
        ]
      })
    end)

    Bypass.expect(bypass, "GET", "/api/v1/epics", fn c ->
      json(c, 200, %{success: true, data: []})
    end)

    parent = self()

    Bypass.expect(bypass, "GET", "/api/v1/tickets", fn c ->
      c = Plug.Conn.fetch_query_params(c)
      send(parent, {:tickets_query, c.query_params})
      json(c, 200, %{success: true, data: []})
    end)

    {:ok, view, _html} = live(authed(conn), "/board")
    assert_receive {:tickets_query, _initial}

    render_hook(view, "filter", %{"type" => "", "epic_id" => "e-1", "search" => ""})
    assert_receive {:tickets_query, filtered}
    assert filtered["teamId"] == "t-1"
    assert filtered["epicId"] == "e-1"

    render_hook(view, "select_team", %{"team_id" => "t-2"})
    assert_receive {:tickets_query, reloaded}
    assert reloaded["teamId"] == "t-2"
    refute Map.has_key?(reloaded, "epicId")
  end

  test "a 401 on the initial board load redirects to the refresh endpoint", %{conn: conn, bypass: bypass} do
    Bypass.expect(bypass, "GET", "/api/v1/teams", fn c ->
      json(c, 401, %{success: false, code: "Auth.Unauthorized", detail: "Token expired."})
    end)

    assert {:error, {:redirect, %{to: to}}} = live(authed(conn), "/board")
    assert to =~ "/session/refresh"
    assert to =~ "return_to"
  end
end

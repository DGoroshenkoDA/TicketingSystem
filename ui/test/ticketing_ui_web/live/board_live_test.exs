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
      "refresh_token" => "refresh"
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
        body: "Body",
        state: "new"
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
end

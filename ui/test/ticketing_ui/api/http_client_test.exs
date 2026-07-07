defmodule TicketingUi.Api.HttpClientTest do
  use ExUnit.Case, async: false

  alias TicketingUi.Api.HttpClient

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

  test "parses a success envelope", %{bypass: bypass} do
    Bypass.expect_once(bypass, "GET", "/api/v1/thing", fn c ->
      json(c, 200, %{success: true, data: %{"ok" => 1}})
    end)

    assert HttpClient.get_json("/api/v1/thing") == {:ok, %{"ok" => 1}}
  end

  test "attaches a bearer token when given", %{bypass: bypass} do
    parent = self()

    Bypass.expect_once(bypass, "GET", "/api/v1/thing", fn c ->
      send(parent, {:auth, Plug.Conn.get_req_header(c, "authorization")})
      json(c, 200, %{data: %{}})
    end)

    assert {:ok, _} = HttpClient.get_json("/api/v1/thing", %{}, token: "abc")
    assert_receive {:auth, ["Bearer abc"]}
  end

  test "maps a non-2xx response to an error tuple", %{bypass: bypass} do
    Bypass.expect_once(bypass, "POST", "/api/v1/thing", fn c ->
      json(c, 422, %{success: false, code: "X", detail: "nope"})
    end)

    assert HttpClient.post_json("/api/v1/thing", %{}) ==
             {:error, %{status: 422, detail: "nope", code: "X"}}
  end

  test "returns a network error when the API is unreachable", %{bypass: bypass} do
    Bypass.down(bypass)

    assert {:error, %{status: :network, code: nil}} = HttpClient.get_json("/api/v1/thing")
  end
end

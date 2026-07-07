defmodule TicketingUi.Api.ResultParserTest do
  use ExUnit.Case, async: true

  alias TicketingUi.Api.ResultParser

  describe "parse_success/1" do
    test "unwraps the {success, data} envelope" do
      assert ResultParser.parse_success(%{"success" => true, "data" => %{"id" => 1}}) ==
               {:ok, %{"id" => 1}}
    end

    test "unwraps a bare {data} envelope" do
      assert ResultParser.parse_success(%{"data" => [1, 2]}) == {:ok, [1, 2]}
    end

    test "passes through a bare body (map or list)" do
      assert ResultParser.parse_success(%{"id" => 1}) == {:ok, %{"id" => 1}}
      assert ResultParser.parse_success([1, 2]) == {:ok, [1, 2]}
    end
  end

  describe "parse_error/2" do
    test "prefers detail, then message, then title, and keeps the code" do
      assert ResultParser.parse_error(400, %{
               "detail" => "d",
               "message" => "m",
               "title" => "t",
               "code" => "C"
             }) == {:error, %{status: 400, detail: "d", code: "C"}}

      assert ResultParser.parse_error(400, %{"message" => "m", "title" => "t"}) ==
               {:error, %{status: 400, detail: "m", code: nil}}

      assert ResultParser.parse_error(400, %{"title" => "t"}) ==
               {:error, %{status: 400, detail: "t", code: nil}}
    end

    test "falls back to a generic message when the map has no known keys" do
      assert {:error, %{status: 404, detail: detail, code: nil}} =
               ResultParser.parse_error(404, %{})

      assert detail =~ "404"
    end

    test "falls back to a generic message for a non-map body" do
      assert ResultParser.parse_error(500, "boom") ==
               {:error, %{status: 500, detail: "Request failed (HTTP 500).", code: nil}}
    end
  end
end

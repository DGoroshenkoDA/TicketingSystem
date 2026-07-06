defmodule TicketingUi.Api.ResultParser do
  @moduledoc """
  Parses the .NET API response envelope:
    success -> %{"success" => true, "data" => data}
    error   -> %{"success" => false, "code" => code, "detail" => detail}
  """

  def parse_success(%{"success" => true, "data" => data}), do: {:ok, data}
  def parse_success(%{"data" => data}), do: {:ok, data}
  def parse_success(body), do: {:ok, body}

  def parse_error(status, body) when is_map(body) do
    detail =
      body["detail"] || body["message"] ||
        body["title"] || "Request failed (HTTP #{status})."

    {:error, %{status: status, detail: detail, code: body["code"]}}
  end

  def parse_error(status, _body) do
    {:error, %{status: status, detail: "Request failed (HTTP #{status}).", code: nil}}
  end
end

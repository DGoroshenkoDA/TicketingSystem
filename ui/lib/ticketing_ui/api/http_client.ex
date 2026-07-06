defmodule TicketingUi.Api.HttpClient do
  @moduledoc """
  Thin HTTP client over Req for the .NET REST API.
  Reads the base URL from config (:ticketing_ui, :api_base_url) and attaches a
  bearer token when provided via the :token option.
  """

  alias TicketingUi.Api.ResultParser

  def get_json(path, params \\ %{}, opts \\ []),
    do: request(:get, path, [params: params] ++ opts)

  def post_json(path, payload \\ %{}, opts \\ []),
    do: request(:post, path, [json: payload] ++ opts)

  def put_json(path, payload \\ %{}, opts \\ []),
    do: request(:put, path, [json: payload] ++ opts)

  def patch_json(path, payload \\ %{}, opts \\ []),
    do: request(:patch, path, [json: payload] ++ opts)

  def delete_json(path, opts \\ []),
    do: request(:delete, path, opts)

  defp request(method, path, opts) do
    token = Keyword.get(opts, :token)

    req_opts =
      [
        method: method,
        base_url: base_url(),
        url: path,
        headers: headers(token),
        receive_timeout: 15_000
      ]
      |> maybe_put(:json, Keyword.get(opts, :json))
      |> maybe_put(:params, Keyword.get(opts, :params))

    case Req.request(req_opts) do
      {:ok, %Req.Response{status: status, body: body}} when status in 200..299 ->
        ResultParser.parse_success(body)

      {:ok, %Req.Response{status: status, body: body}} ->
        ResultParser.parse_error(status, body)

      {:error, reason} ->
        {:error, %{status: :network, detail: Exception.message(reason), code: nil}}
    end
  end

  defp base_url do
    :ticketing_ui
    |> Application.get_env(:api_base_url, "http://localhost:5080")
    |> String.trim_trailing("/")
  end

  defp headers(nil), do: [{"accept", "application/json"}]
  defp headers(""), do: [{"accept", "application/json"}]
  defp headers(token), do: [{"accept", "application/json"}, {"authorization", "Bearer #{token}"}]

  defp maybe_put(opts, _key, nil), do: opts
  defp maybe_put(opts, key, value), do: Keyword.put(opts, key, value)
end

defmodule TicketingUi.Api.AuthApi do
  @moduledoc "Auth endpoints of the .NET API."

  alias TicketingUi.Api.HttpClient

  @spec signup(map()) :: {:ok, map()} | {:error, map()}
  def signup(%{email: email, display_name: display_name, password: password, password_confirm: confirm}) do
    HttpClient.post_json("/api/v1/auth/signup", %{
      email: email,
      displayName: display_name,
      password: password,
      passwordConfirm: confirm
    })
  end

  @spec login(String.t(), String.t()) :: {:ok, map()} | {:error, map()}
  def login(email, password) do
    case HttpClient.post_json("/api/v1/auth/login", %{email: email, password: password}) do
      {:ok, data} -> {:ok, normalize_auth(data)}
      error -> error
    end
  end

  @spec refresh(String.t()) :: {:ok, map()} | {:error, map()}
  def refresh(refresh_token) do
    case HttpClient.post_json("/api/v1/auth/refresh", %{refreshToken: refresh_token}) do
      {:ok, data} -> {:ok, normalize_auth(data)}
      error -> error
    end
  end

  @spec logout(String.t(), String.t() | nil) :: {:ok, map()} | {:error, map()}
  def logout(refresh_token, access_token \\ nil) do
    HttpClient.post_json("/api/v1/auth/logout", %{refreshToken: refresh_token}, token: access_token)
  end

  @spec verify(String.t()) :: {:ok, map()} | {:error, map()}
  def verify(token), do: HttpClient.get_json("/api/v1/auth/verify", %{token: token})

  @spec resend(String.t()) :: {:ok, map()} | {:error, map()}
  def resend(email), do: HttpClient.post_json("/api/v1/auth/resend-verification", %{email: email})

  defp normalize_auth(data) do
    user = data["user"] || %{}

    %{
      access_token: data["accessToken"],
      access_expires_at: data["accessExpiresAt"],
      refresh_token: data["refreshToken"],
      refresh_expires_at: data["refreshExpiresAt"],
      user: %{
        id: user["id"],
        email: user["email"],
        display_name: user["displayName"]
      }
    }
  end
end

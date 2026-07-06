defmodule TicketingUiWeb.ProfileController do
  use TicketingUiWeb, :controller

  alias TicketingUi.Api.ProfileApi
  alias TicketingUiWeb.Theme

  def show(conn, _params) do
    profile =
      case ProfileApi.get(token(conn)) do
        {:ok, data} -> data
        _ -> %{}
      end

    render(conn, :show, profile: profile, themes: Theme.themes(), current_theme: conn.assigns[:theme] || Theme.default())
  end

  def update(conn, %{"display_name" => display_name}) do
    case ProfileApi.update_display_name(token(conn), display_name) do
      {:ok, data} ->
        conn
        |> put_session(:user_name, data["displayName"])
        |> put_flash(:info, "Profile updated.")
        |> redirect(to: "/profile")

      {:error, err} ->
        conn |> put_flash(:error, err[:detail] || "Could not update profile.") |> redirect(to: "/profile")
    end
  end

  def change_password(conn, %{"current_password" => current, "new_password" => new}) do
    case ProfileApi.change_password(token(conn), current, new) do
      {:ok, _} ->
        conn |> put_flash(:info, "Password changed.") |> redirect(to: "/profile")

      {:error, err} ->
        conn |> put_flash(:error, err[:detail] || "Could not change password.") |> redirect(to: "/profile")
    end
  end

  def set_theme(conn, %{"theme" => theme}) do
    conn =
      if Theme.valid?(theme) do
        put_resp_cookie(conn, Theme.cookie_name(), theme,
          max_age: 60 * 60 * 24 * 365,
          http_only: false,
          same_site: "Lax"
        )
      else
        conn
      end

    redirect(conn, to: "/profile")
  end

  defp token(conn), do: get_session(conn, :access_token)
end

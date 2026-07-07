defmodule TicketingUiWeb.ProfileController do
  use TicketingUiWeb, :controller

  alias TicketingUi.Api.ProfileApi
  alias TicketingUiWeb.Theme

  def show(conn, _params) do
    themes = Theme.themes()
    current_theme = conn.assigns[:theme] || Theme.default()

    case ProfileApi.get(token(conn)) do
      {:ok, data} ->
        render(conn, :show, profile: data, themes: themes, current_theme: current_theme, load_error: nil)

      {:error, %{status: 401}} ->
        redirect(conn, to: ~p"/session/refresh?#{[return_to: "/profile"]}")

      {:error, err} ->
        render(conn, :show,
          profile: %{},
          themes: themes,
          current_theme: current_theme,
          load_error: err[:detail] || "Could not load your profile."
        )
    end
  end

  def update(conn, params) do
    display_name = params["display_name"] || ""

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

  def change_password(conn, params) do
    current = params["current_password"] || ""
    new = params["new_password"] || ""

    case ProfileApi.change_password(token(conn), current, new) do
      {:ok, _} ->
        conn |> put_flash(:info, "Password changed.") |> redirect(to: "/profile")

      {:error, err} ->
        conn |> put_flash(:error, err[:detail] || "Could not change password.") |> redirect(to: "/profile")
    end
  end

  def set_theme(conn, params) do
    theme = params["theme"] || ""

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

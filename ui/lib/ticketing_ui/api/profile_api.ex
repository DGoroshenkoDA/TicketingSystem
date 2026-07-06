defmodule TicketingUi.Api.ProfileApi do
  @moduledoc "Profile endpoints of the .NET API."

  alias TicketingUi.Api.HttpClient

  def get(token), do: HttpClient.get_json("/api/v1/profile", %{}, token: token)

  def update_display_name(token, display_name),
    do: HttpClient.put_json("/api/v1/profile", %{displayName: display_name}, token: token)

  def change_password(token, current_password, new_password),
    do:
      HttpClient.post_json(
        "/api/v1/profile/password",
        %{currentPassword: current_password, newPassword: new_password},
        token: token
      )
end

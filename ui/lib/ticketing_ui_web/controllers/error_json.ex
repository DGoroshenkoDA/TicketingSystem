defmodule TicketingUiWeb.ErrorJSON do
  # Renders {"errors": {"detail": "..."}} from the status message.
  def render(template, _assigns) do
    %{errors: %{detail: Phoenix.Controller.status_message_from_template(template)}}
  end
end

defmodule TicketingUiWeb.ErrorHTML do
  use TicketingUiWeb, :html

  # Renders "404.html", "500.html", etc. from the status message.
  def render(template, _assigns) do
    Phoenix.Controller.status_message_from_template(template)
  end
end

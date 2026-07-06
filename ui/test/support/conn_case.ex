defmodule TicketingUiWeb.ConnCase do
  @moduledoc """
  Test case for controller/endpoint tests.
  """
  use ExUnit.CaseTemplate

  using do
    quote do
      import Plug.Conn
      import Phoenix.ConnTest
      import TicketingUiWeb.ConnCase

      @endpoint TicketingUiWeb.Endpoint
    end
  end

  setup _tags do
    # Skip CSRF protection so form POSTs can be exercised in tests.
    conn =
      Phoenix.ConnTest.build_conn()
      |> Plug.Conn.put_private(:plug_skip_csrf_protection, true)

    {:ok, conn: conn}
  end
end

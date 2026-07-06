defmodule TicketingUiWeb.CoreComponents do
  @moduledoc """
  Core UI components. Kept minimal for Phase 0; extended in later phases
  (buttons, inputs, modals, tables).
  """
  use Phoenix.Component

  @doc "Renders flash notices."
  attr :flash, :map, default: %{}

  def flash_group(assigns) do
    ~H"""
    <div class="fixed top-4 right-4 z-50 space-y-2">
      <.flash :if={Phoenix.Flash.get(@flash, :info)} kind={:info} message={Phoenix.Flash.get(@flash, :info)} />
      <.flash :if={Phoenix.Flash.get(@flash, :error)} kind={:error} message={Phoenix.Flash.get(@flash, :error)} />
    </div>
    """
  end

  attr :kind, :atom, required: true
  attr :message, :string, required: true

  def flash(assigns) do
    ~H"""
    <div
      phx-click={Phoenix.LiveView.JS.push("lv:clear-flash", value: %{key: @kind})}
      class={[
        "cursor-pointer rounded-lg px-4 py-3 text-sm font-medium shadow-lg ring-1",
        @kind == :info && "bg-green-50 text-green-800 ring-green-200",
        @kind == :error && "bg-red-50 text-red-800 ring-red-200"
      ]}
      role="alert"
    >
      {@message}
    </div>
    """
  end
end

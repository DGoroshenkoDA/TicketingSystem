defmodule TicketingUiWeb.CoreComponents do
  @moduledoc """
  Core UI components shared across the app (flash notices).
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

  @doc """
  Renders a UTC ISO-8601 timestamp that the browser localizes to the visitor's
  timezone via the `LocalTime` JS hook. The raw ISO string is rendered as the
  server-side fallback (used when JS is off or parsing fails). Works in both
  LiveViews (re-runs after patches) and dead views (window-load pass in app.js).

  A stable, unique `id` is required so LiveView can track the element.
  """
  attr :at, :string, default: nil
  attr :id, :string, required: true
  attr :class, :string, default: nil

  def local_time(assigns) do
    ~H"""
    <time
      :if={@at not in [nil, ""]}
      id={@id}
      datetime={@at}
      data-utc={@at}
      phx-hook="LocalTime"
      class={@class}
    >{@at}</time>
    <span :if={@at in [nil, ""]} class={@class}>—</span>
    """
  end

  @doc "Inline SVG icon (heroicons-style, 24×24, stroke = currentColor)."
  attr :name, :string, required: true
  attr :class, :string, default: "h-5 w-5"

  def icon(assigns) do
    ~H"""
    <svg
      class={@class}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.5"
      aria-hidden="true"
    >
      <%= case @name do %>
        <% "check" -> %>
          <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
        <% "plus" -> %>
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        <% "x" -> %>
          <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
        <% "trash" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0"
          />
        <% "user" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z"
          />
        <% "mail" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75"
          />
        <% "lock" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z"
          />
        <% "eye" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z"
          />
          <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
        <% "eye-off" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.774 3.162 10.066 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88"
          />
        <% "login" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l3 3m0 0l-3 3m3-3H2.25"
          />
        <% "user-plus" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M18 7.5v3m0 0v3m0-3h3m-3 0h-3m-2.25-4.125a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zM3 19.235v-.11a6.375 6.375 0 0112.75 0v.109A12.318 12.318 0 019.374 21c-2.331 0-4.512-.645-6.374-1.766z"
          />
        <% "alert" -> %>
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"
          />
        <% _ -> %>
      <% end %>
    </svg>
    """
  end
end

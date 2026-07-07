defmodule TicketingUiWeb.AuthHTML do
  use TicketingUiWeb, :html

  embed_templates "auth_html/*"

  @doc """
  Card shell for the authentication screens: centres a single elevated card on
  the themed page background. Screens supply their own heading/tabs/content.
  """
  slot :inner_block, required: true

  def auth_shell(assigns) do
    ~H"""
    <div class="flex min-h-[calc(100vh-8rem)] items-center justify-center py-8">
      <div class="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-xl">
        {render_slot(@inner_block)}
      </div>
    </div>
    """
  end

  @doc "Segmented Sign Up / Sign In switch. The active tab reflects the current page."
  attr :active, :atom, required: true

  def auth_tabs(assigns) do
    ~H"""
    <div class="grid grid-cols-2 gap-1 rounded-xl bg-slate-100 p-1">
      <.tab_link href="/signup" active={@active == :signup} icon="user-plus" label="Sign Up" />
      <.tab_link href="/login" active={@active == :login} icon="login" label="Sign In" />
    </div>
    """
  end

  attr :href, :string, required: true
  attr :active, :boolean, required: true
  attr :icon, :string, required: true
  attr :label, :string, required: true

  defp tab_link(assigns) do
    ~H"""
    <a
      href={@href}
      class={[
        "flex items-center justify-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition",
        @active && "bg-white text-slate-900 shadow-sm ring-1 ring-slate-200",
        !@active && "text-slate-500 hover:text-slate-700"
      ]}
    >
      <.icon name={@icon} class="h-4 w-4" />
      {@label}
    </a>
    """
  end

  @doc "Labelled text input with a leading icon."
  attr :name, :string, required: true
  attr :label, :string, required: true
  attr :icon, :string, required: true
  attr :type, :string, default: "text"
  attr :value, :string, default: nil
  attr :placeholder, :string, default: nil
  attr :autocomplete, :string, default: nil
  attr :id, :string, default: nil
  attr :required, :boolean, default: true

  def input_field(assigns) do
    ~H"""
    <div>
      <label class="mb-1.5 block text-sm font-medium text-slate-700">{@label}</label>
      <div class="relative">
        <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
          <.icon name={@icon} class="h-5 w-5" />
        </span>
        <input
          type={@type}
          id={@id}
          name={@name}
          value={@value}
          placeholder={@placeholder}
          autocomplete={@autocomplete}
          required={@required}
          class="block w-full rounded-lg border-slate-200 bg-white py-2.5 pl-10 pr-3 text-sm text-slate-900 placeholder:text-slate-400 shadow-sm focus:border-brand focus:ring-1 focus:ring-brand"
        />
      </div>
    </div>
    """
  end

  @doc "Password input with a leading lock icon and a show/hide toggle."
  attr :name, :string, required: true
  attr :label, :string, required: true
  attr :id, :string, required: true
  attr :placeholder, :string, default: "••••••••"
  attr :autocomplete, :string, default: "new-password"
  attr :minlength, :string, default: nil

  def password_field(assigns) do
    ~H"""
    <div>
      <label class="mb-1.5 block text-sm font-medium text-slate-700">{@label}</label>
      <div class="relative">
        <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
          <.icon name="lock" class="h-5 w-5" />
        </span>
        <input
          type="password"
          id={@id}
          name={@name}
          placeholder={@placeholder}
          autocomplete={@autocomplete}
          required
          minlength={@minlength}
          class="block w-full rounded-lg border-slate-200 bg-white py-2.5 pl-10 pr-10 text-sm text-slate-900 placeholder:text-slate-400 shadow-sm focus:border-brand focus:ring-1 focus:ring-brand"
        />
        <button
          type="button"
          data-pw-toggle={@id}
          tabindex="-1"
          aria-label="Show password"
          class="absolute inset-y-0 right-0 flex items-center pr-3 text-slate-400 hover:text-slate-600"
        >
          <.icon name="eye" class="pw-eye h-5 w-5" />
          <.icon name="eye-off" class="pw-eye-off hidden h-5 w-5" />
        </button>
      </div>
    </div>
    """
  end

  @doc "Primary full-width submit button with a leading icon."
  attr :icon, :string, required: true
  attr :label, :string, required: true
  attr :rest, :global

  def submit_button(assigns) do
    ~H"""
    <button
      type="submit"
      {@rest}
      class="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-brand px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-brand-hover focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand disabled:cursor-not-allowed disabled:opacity-50"
    >
      <.icon name={@icon} class="h-5 w-5" />
      {@label}
    </button>
    """
  end
end

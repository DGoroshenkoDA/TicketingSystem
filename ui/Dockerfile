# syntax=docker/dockerfile:1

ARG ELIXIR_VERSION=1.17.3
ARG OTP_VERSION=27.1.2
ARG DEBIAN_VERSION=bookworm-20241016-slim

ARG BUILDER_IMAGE="hexpm/elixir:${ELIXIR_VERSION}-erlang-${OTP_VERSION}-debian-${DEBIAN_VERSION}"
ARG RUNNER_IMAGE="debian:${DEBIAN_VERSION}"

# ---- build stage ----
FROM ${BUILDER_IMAGE} AS build

RUN apt-get update -y \
    && apt-get install -y build-essential git \
    && apt-get clean && rm -f /var/lib/apt/lists/*_*

WORKDIR /app

RUN mix local.hex --force && mix local.rebar --force

ENV MIX_ENV="prod"

# Install dependencies (cached layer).
COPY mix.exs ./
RUN mix deps.get --only $MIX_ENV
RUN mkdir config
COPY config/config.exs config/${MIX_ENV}.exs config/
RUN mix deps.compile

COPY priv priv
COPY lib lib
COPY assets assets

# Install asset tooling (tailwind + esbuild binaries), then build assets and compile.
RUN mix assets.setup
RUN mix assets.deploy
RUN mix compile

COPY config/runtime.exs config/

RUN mix release

# ---- runtime stage ----
FROM ${RUNNER_IMAGE} AS runtime

RUN apt-get update -y \
    && apt-get install -y --no-install-recommends libstdc++6 openssl libncurses5 locales ca-certificates curl \
    && apt-get clean && rm -f /var/lib/apt/lists/*_*

RUN sed -i '/en_US.UTF-8/s/^# //g' /etc/locale.gen && locale-gen
ENV LANG=en_US.UTF-8 LANGUAGE=en_US:en LC_ALL=en_US.UTF-8

WORKDIR /app

ENV MIX_ENV="prod"
ENV PHX_SERVER="true"

COPY --from=build /app/_build/${MIX_ENV}/rel/ticketing_ui ./

EXPOSE 4000

CMD ["/app/bin/ticketing_ui", "start"]

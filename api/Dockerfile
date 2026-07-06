# syntax=docker/dockerfile:1

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (better layer caching): copy solution + project files only.
COPY Ticketing.sln .
COPY Ticketing.Common/Ticketing.Common.csproj Ticketing.Common/
COPY Ticketing.Data/Ticketing.Data.csproj Ticketing.Data/
COPY Ticketing.Services/Ticketing.Services.csproj Ticketing.Services/
COPY Ticketing.Api/Ticketing.Api.csproj Ticketing.Api/
COPY Ticketing.Tests/Ticketing.Tests.csproj Ticketing.Tests/
RUN dotnet restore Ticketing.Api/Ticketing.Api.csproj

# Copy the rest and publish.
COPY . .
RUN dotnet publish Ticketing.Api/Ticketing.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is used by the compose healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Ticketing.Api.dll"]

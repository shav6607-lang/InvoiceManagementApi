# Stage 1: Base runtime environment with non-root security
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app


# Stage 2: SDK Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "InvoiceManagementApi.dll"]

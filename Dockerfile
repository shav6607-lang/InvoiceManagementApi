# Stage 1: Base runtime environment with non-root security
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app


# Stage 2: SDK Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project file(s) first to leverage Docker cache for dependencies
COPY ..
RUN dotnet restore
RUN dotnet publish -c Relase -o /app/publish 


# Stage 3: Publish the binaries
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Stage 4: Final lean runtime image
COPY --from=build /app/publish
ENTRYPOINT ["dotnet","InvoiceManagementApi.dll"]
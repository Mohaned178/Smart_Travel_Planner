# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY SmartTravelPlanner.slnx .
COPY src/SmartTravelPlanner.Api/SmartTravelPlanner.Api.csproj src/SmartTravelPlanner.Api/
COPY src/SmartTravelPlanner.Application/SmartTravelPlanner.Application.csproj src/SmartTravelPlanner.Application/
COPY src/SmartTravelPlanner.Domain/SmartTravelPlanner.Domain.csproj src/SmartTravelPlanner.Domain/
COPY src/SmartTravelPlanner.Infrastructure/SmartTravelPlanner.Infrastructure.csproj src/SmartTravelPlanner.Infrastructure/

RUN dotnet restore src/SmartTravelPlanner.Api/SmartTravelPlanner.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet publish src/SmartTravelPlanner.Api/SmartTravelPlanner.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "SmartTravelPlanner.Api.dll"]

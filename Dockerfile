# Multi-stage build for Nostra.DataLoad solution
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy solution and csproj files first for layer caching
COPY ["Nostra.DataLoad.sln", "./"]
COPY ["MLModel1_WebApi1/MLModel1_WebApi1.csproj", "MLModel1_WebApi1/"]
COPY ["MLNetTest/MLNetTest.csproj", "MLNetTest/"]
COPY ["Nostra.DataLoad/Nostra.DataLoad.csproj", "Nostra.DataLoad/"]
COPY ["Nostra.DataLoad.APIClient/Nostra.DataLoad.APIClient.csproj", "Nostra.DataLoad.APIClient/"]
COPY ["Nostra.DataLoad.AutotaskAPIClient/Nostra.DataLoad.AutotaskAPIClient.csproj", "Nostra.DataLoad.AutotaskAPIClient/"]
COPY ["Nostra.DataLoad.Cin7APIClient/Nostra.DataLoad.Cin7APIClient.csproj", "Nostra.DataLoad.Cin7APIClient/"]
COPY ["Nostra.DataLoad.Core/Nostra.DataLoad.Core.csproj", "Nostra.DataLoad.Core/"]
COPY ["Nostra.DataLoad.Domain/Nostra.DataLoad.Domain.csproj", "Nostra.DataLoad.Domain/"]
COPY ["Nostra.DataLoad.Host/Nostra.DataLoad.Host.csproj", "Nostra.DataLoad.Host/"]
COPY ["Nostra.DataLoad.UI/Nostra.DataLoad.UI.csproj", "Nostra.DataLoad.UI/"]

# Restore packages
RUN dotnet restore "Nostra.DataLoad.sln"

# Copy the rest of the source code
COPY . .

# Build the Host project
RUN dotnet build "Nostra.DataLoad.Host/Nostra.DataLoad.Host.csproj" -c Release -o /app/build

# Publish the Host project
FROM build AS publish-host
RUN dotnet publish "Nostra.DataLoad.Host/Nostra.DataLoad.Host.csproj" -c Release -o /app/publish/host

# Build the UI project
RUN dotnet build "Nostra.DataLoad.UI/Nostra.DataLoad.UI.csproj" -c Release -o /app/build

# Publish the UI project
FROM build AS publish-ui
RUN dotnet publish "Nostra.DataLoad.UI/Nostra.DataLoad.UI.csproj" -c Release -o /app/publish/ui

# Runtime image for Host
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS host
WORKDIR /app
COPY --from=publish-host /app/publish/host .
EXPOSE 80
EXPOSE 443
EXPOSE 11111
EXPOSE 30000

# Health check for the API
HEALTHCHECK --interval=30s --timeout=30s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Environment variables for database connection
ENV ConnectionStrings__DefaultSqlConnection="Server=sqlserver;Database=Nostra_Dataload;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
ENV ConnectionStrings__TaskQueueConnection="Server=sqlserver;Database=Nostra_Dataload_Tasks;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Nostra.DataLoad.Host.dll"]

# Runtime image for UI
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS ui
WORKDIR /app
COPY --from=publish-ui /app/publish/ui .
EXPOSE 80
EXPOSE 443

# Health check for the UI
HEALTHCHECK --interval=30s --timeout=30s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Environment variables for API connection
ENV API_BASE_URL="http://host"
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Nostra.DataLoad.UI.dll"]
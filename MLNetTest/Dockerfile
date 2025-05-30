# Use the .NET SDK for building the app
# NOTE: Replace 8.0 with your actual .NET version from your .csproj file
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MLNetTest/*.csproj", "MLNetTest/"]
RUN dotnet restore "MLNetTest/MLNetTest.csproj"

# Copy all files and build
COPY . .
WORKDIR "/src/MLNetTest"
RUN dotnet build "MLNetTest.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "MLNetTest.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# If this is a console app rather than a web app, use:
# FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user to run the app and own app files
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Set the entry point
ENTRYPOINT ["dotnet", "MLNetTest.dll"]
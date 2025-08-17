# Use the official .NET 9.0 runtime image
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# Use the .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file
COPY SoccerUlanzi.csproj .
RUN dotnet restore "SoccerUlanzi.csproj"

# Copy the source code
COPY . .
RUN dotnet build "SoccerUlanzi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SoccerUlanzi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Create cache directory for logo storage
RUN mkdir -p cache

# Copy the published application
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "SoccerUlanzi.dll"]
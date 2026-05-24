# ==========================================
# V-Closet ASP.NET Core 8 Web API Dockerfile
# ==========================================

# Stage 1: Runtime Base Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build SDK Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to restore NuGet dependencies (Docker layer caching optimization)
COPY ["VCloset.API/VCloset.API.csproj", "VCloset.API/"]
COPY ["VCloset.Infrastructure/VCloset.Infrastructure.csproj", "VCloset.Infrastructure/"]
COPY ["VCloset.Application/VCloset.Application.csproj", "VCloset.Application/"]
COPY ["VCloset.Domain/VCloset.Domain.csproj", "VCloset.Domain/"]

RUN dotnet restore "VCloset.API/VCloset.API.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/VCloset.API"
RUN dotnet build "VCloset.API.csproj" -c Release -o /app/build

# Stage 3: Publish Image
FROM build AS publish
RUN dotnet publish "VCloset.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final Runtime Image
FROM base AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VCloset.API.dll"]

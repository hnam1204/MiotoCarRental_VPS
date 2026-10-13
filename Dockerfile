# Stage 1: Build the authoritative React bundle.
FROM node:22-bookworm-slim AS frontend
WORKDIR /src/ClientApp

COPY ClientApp/package.json ClientApp/package-lock.json ./
RUN npm ci

COPY ClientApp/ ./
RUN npm run build

# Stage 2: Build the ASP.NET Core application.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MiotoCarRental.csproj", "./"]
RUN dotnet restore "MiotoCarRental.csproj"

# Copy full source and publish
COPY . .
COPY --from=frontend /src/wwwroot/react ./wwwroot/react
RUN dotnet publish "MiotoCarRental.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create directory for persistent SQLite database
RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "MiotoCarRental.dll"]

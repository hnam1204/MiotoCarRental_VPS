# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MiotoCarRental.csproj", "./"]
RUN dotnet restore "MiotoCarRental.csproj"

# Copy full source and publish
COPY . .
RUN dotnet publish "MiotoCarRental.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create directory for persistent SQLite database
RUN mkdir -p /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "MiotoCarRental.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy everything and restore dependencies
COPY . ./
RUN dotnet restore

# Publish the app to the /app folder
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy the published app from build stage
COPY --from=build /app .

# Expose port 8080 (Render expects your app to listen on this port)
EXPOSE 8080

# Start the app
ENTRYPOINT ["dotnet", "WebApp.dll"]
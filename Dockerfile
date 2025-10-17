# --- Stage 1: Build the application ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and build the app
COPY . .
RUN dotnet restore "./AICodeReviewer.csproj"
RUN dotnet publish "./AICodeReviewer.csproj" -c Release -o /out

# --- Stage 2: Run the application ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy build output from previous stage
COPY --from=build /out .

# Optionally expose a port (Render sets PORT dynamically; EXPOSE is informational)
EXPOSE 10000

# Run the app in server mode
ENTRYPOINT ["dotnet", "AICodeReviewer.dll", "--server"]

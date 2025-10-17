# --- Stage 1: Build the application ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy everything and build the app
COPY . .
RUN dotnet restore "./AI_CodeReviewer.csproj"
RUN dotnet publish "./AI_CodeReviewer.csproj" -c Release -o /out

# --- Stage 2: Run the application ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy build output from previous stage
COPY --from=build /out .

# Tell Render which port to use dynamically
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
EXPOSE 10000

# Run the app
ENTRYPOINT ["dotnet", "AI_CodeReviewer.dll"]

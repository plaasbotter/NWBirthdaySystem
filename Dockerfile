# Use official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy csproj and restore as distinct layers (improves caching)
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the code
COPY . ./

# Publish the application to the /app/publish directory
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# If config.nw is required at runtime, copy it
COPY config.nw .

# Set a non-root user if needed for security (optional)
# USER app

ENTRYPOINT ["dotnet", "NWBirthdaySystem.dll"]
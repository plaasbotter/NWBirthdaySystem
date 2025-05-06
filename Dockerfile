# Use official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy the codebase
COPY ./NWBirthdaySystem ./
RUN dotnet restore

# Publish the application to the /app/publish directory
RUN dotnet publish -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# If config.nw is required at runtime, copy it
COPY config.nw .

ENTRYPOINT ["dotnet", "NWBirthdaySystem.dll"]
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY TrueDope.sln .
COPY src/TrueDope.Api/TrueDope.Api.csproj src/TrueDope.Api/
COPY tests/TrueDope.Api.Tests/TrueDope.Api.Tests.csproj tests/TrueDope.Api.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/src/TrueDope.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser

COPY --from=build /app/publish .

# Change ownership and switch to non-root user
RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 80
ENTRYPOINT ["dotnet", "TrueDope.Api.dll"]

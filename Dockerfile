# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore dependencies
COPY DocumentEditorServer.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install native libraries required for SkiaSharp and DocIORenderer PDF conversion
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libjpeg62-turbo \
    libpng16-16 \
    libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy built application
COPY --from=build /app/out .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run application
ENTRYPOINT ["dotnet", "DocumentEditorServer.dll"]

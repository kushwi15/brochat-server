# BroChat Backend

This is a production-ready ASP.NET Core 10 Web API built with Clean Architecture principles. It serves as the backend for the BroChat AI Chat Application.

## Features

- **Clean Architecture**: Domain, Application, Infrastructure, API Layers.
- **Hybrid Authentication**: JWT tokens for API access with HTTP-only cookies for refresh tokens.
- **SQL Server with EF Core**: Uses Code-First migrations with proper relationships.
- **Real-Time Streaming**: SignalR integration for token-by-token streaming of AI responses.
- **Gemini AI Integration**: Stream responses from Google's Gemini Models.
- **OpenAPI & Scalar UI**: Modern `.MapOpenApi()` with `Scalar.AspNetCore` for interactive API documentation.
- **Docker Ready**: Includes a multi-stage `Dockerfile` for seamless deployment.

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, Docker, or Cloud instance)
- Gemini API Key

## Setup Instructions

1. **Database Configuration**
   Update the `DefaultConnection` in `src/BroChat.Api/appsettings.json` or `appsettings.Development.json` with your SQL Server connection string.

2. **Configure Secrets**
   Set your `Jwt:Secret` (minimum 32 characters) and `Gemini:ApiKey` in `appsettings.json` or using .NET User Secrets:
   ```bash
   dotnet user-secrets set "Jwt:Secret" "YOUR_SUPER_SECRET_KEY"
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
   ```

3. **Apply Database Migrations**
   ```bash
   dotnet ef database update --project src/BroChat.Infrastructure --startup-project src/BroChat.Api
   ```

4. **Run the Application**
   ```bash
   dotnet run --project src/BroChat.Api
   ```

5. **View API Documentation**
   Navigate to `http://localhost:<port>/scalar/v1` to view the interactive Scalar API documentation.

## Running in Docker

To build and run the Docker image:

```bash
docker build -t brochat-api .
docker run -d -p 8080:80 \
  -e ConnectionStrings__DefaultConnection="Your_Connection_String" \
  -e Jwt__Secret="Your_Secret" \
  -e Gemini__ApiKey="Your_Api_Key" \
  brochat-api
```

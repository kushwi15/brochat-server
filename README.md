# BroChat Backend

This is a production-ready ASP.NET Core 10 Web API built with Clean Architecture principles. It serves as the backend for the BroChat AI Chat Application.

## Features

- **Clean Architecture**: Domain, Application, Infrastructure, API Layers.
- **Hybrid Authentication**: JWT tokens for API access with HTTP-only cookies for refresh tokens.
- **MongoDB Integration**: High-performance NoSQL document storage for users, conversations, and messages.
- **Real-Time Streaming**: SignalR integration for token-by-token streaming of AI responses.
- **Gemini AI Integration**: Stream responses from Google's Gemini Models.
- **OpenAPI & Scalar UI**: Modern API documentation with interactive testing.
- **Docker Ready**: Includes a multi-stage `Dockerfile` for seamless deployment.

## Prerequisites

- .NET 10 SDK
- MongoDB (Local instance or Atlas)
- Gemini API Key

## Setup Instructions

1. **Database Configuration**
   Update the `MongoDb` settings in `src/BroChat.Api/appsettings.json` or `appsettings.Development.json`:
   ```json
   "MongoDb": {
     "ConnectionString": "mongodb://localhost:27017",
     "DatabaseName": "brochat"
   }
   ```

2. **Configure Secrets**
   Set your `Jwt:Secret` (minimum 32 characters) and `Gemini:ApiKey` in `appsettings.json` or using .NET User Secrets:
   ```bash
   dotnet user-secrets set "Jwt:Secret" "YOUR_SUPER_SECRET_KEY"
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
   dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://localhost:27017"
   ```

3. **Run the Application**
   ```bash
   dotnet run --project src/BroChat.Api
   ```

4. **View API Documentation**
   Navigate to `http://localhost:<port>/swagger` to view the interactive API documentation.

## Running in Docker

To build and run the Docker image:

```bash
docker build -t brochat-api .
docker run -d -p 8080:80 \
  -e MongoDb__ConnectionString="mongodb://host.docker.internal:27017" \
  -e MongoDb__DatabaseName="brochat" \
  -e Jwt__Secret="Your_Secret" \
  -e Gemini__ApiKey="Your_Api_Key" \
  brochat-api
```

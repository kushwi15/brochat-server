FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln .
COPY src/BroChat.Domain/*.csproj ./src/BroChat.Domain/
COPY src/BroChat.Application/*.csproj ./src/BroChat.Application/
COPY src/BroChat.Infrastructure/*.csproj ./src/BroChat.Infrastructure/
COPY src/BroChat.Api/*.csproj ./src/BroChat.Api/
COPY tests/BroChat.Tests/*.csproj ./tests/BroChat.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish
WORKDIR /app/src/BroChat.Api
RUN dotnet publish -c Release -o /out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "BroChat.Api.dll"]

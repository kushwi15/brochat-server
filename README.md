# BroChat Server - Modular Monolith AI Backend

The BroChat Backend is a production-ready **.NET 10 Web API** architected with Clean Architecture and Modular Monolith principles. It powers the BroChat ecosystem with real-time streaming, multi-tenant data isolation, and robust security.

## 🏗️ Clean Architecture & Design Patterns

### 1. BroChat.Api (The Interface)
- **Controllers**: Thin controllers that delegate logic to services.
- **ChatHub**: A SignalR Hub that manages the lifecycle of an AI conversation. It handles connection-based `CancellationTokenSource` objects to allow users to cancel AI generation.
- **Middleware**: Includes `ExceptionMiddleware` for global error handling and a custom COOP (Cross-Origin-Opener-Policy) header middleware for secure Google OAuth popups.

### 2. BroChat.Application (The Core)
- **DTOs**: Strictly typed request/response models (e.g., `AuthResponse`, `RegisterRequest`).
- **Interfaces**: Defines the "contracts" for all infrastructure services, ensuring the core logic remains independent of specific technologies like MongoDB or Gemini.

### 3. BroChat.Domain (The Heart)
- **Entities**: Domain models like `User`, `Conversation`, `Message`. Note that `Message` includes a `Role` (User/AI) and a `Timestamp`.
- **Enums**: Strongly typed roles to prevent string-based logic errors.

### 4. BroChat.Infrastructure (The Implementation)
- **MongoDbContext**: Implements the multi-tenant logic. It automatically creates indexes for emails, TTL (Time-To-Live) indexes for refresh tokens, and per-user databases.
- **GeminiAiService**: A resilient HTTP client implementation that parses Server-Sent Events (SSE) from Google's Generative Language API.
- **JwtTokenService**: Handles the generation and validation of HS256-signed JWTs.

---

## 💾 MongoDB Multi-Tenant Strategy

BroChat uses a sophisticated data isolation model:
- **Shared DB (`brochat`)**: Contains global collections like `Users`, `RefreshTokens`, and `GuestUsages`.
- **Tenant DB (`u_{userId_short}`)**: Every registered user gets their own dedicated database for their `Conversations` and `Messages`.
- **Why?**: This provides ultimate data isolation. Even a "Select All" query error on the message collection can only ever return the current user's data.

### Indexing Strategy
- **Users**: Unique index on `Email`.
- **RefreshTokens**: **TTL Index** on `ExpiresAt` ensures expired tokens are automatically deleted by MongoDB.
- **GuestUsages**: Unique index on `GuestId` for fast rate-limit checking.

---

## 🛡️ Security & CSRF Hardening

### Authentication Flow
1. **Login**: User provides credentials -> Backend returns a **JWT Access Token** (body) and a **Refresh Token** (HttpOnly Cookie).
2. **Accessing Data**: Frontend sends JWT in the `Authorization` header. Backend validates the signature and issuer.
3. **Token Expiry**: When JWT expires, the frontend calls `/api/auth/refresh`. The backend validates the `refreshToken` cookie and issues a new pair.
4. **Logout**: Revokes the refresh token in the DB and clears the cookie.

### CSRF Resistance
The API is **CSRF-Proof** because:
- No sensitive actions rely on session cookies.
- All data-modifying endpoints (`[Authorize]`) require the `Authorization` header.
- SignalR connections are authenticated via a query string token which is manually passed by the client, not automatically by the browser.

---

## 🤖 Gemini AI Integration Logic

The `GeminiAiService` is optimized for speed and reliability:
- **Model**: `gemini-3-flash-preview`
- **SSE Parsing**: The service reads the stream line-by-line. It looks for `data: ` prefixes and parses the inner JSON to extract the text "delta."
- **Safety Filters**: If Gemini blocks a response due to safety filters, the service gracefully returns a predefined safety message instead of crashing.
- **Cancellation**: If a user disconnects or clicks "Stop," the `CancellationToken` is triggered, immediately halting the HTTP request to Google to save resources.

---

## 🚀 Deployment (Render / Docker)

### Environment Variables
For production, ensure these are set:
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `MongoDb__ConnectionString`: Your MongoDB Atlas URI.
- `Jwt__Secret`: A secure, random 32+ character string.
- `Gemini__ApiKey`: Your Google AI Studio key.
- `AllowedOrigins__0`: Your frontend URL (e.g., `https://brochat.vercel.app`).

### Docker Build
```bash
docker build -t brochat-server .
docker run -p 8080:8080 brochat-server
```

---

## 📧 Email System
Uses **MailKit** over SMTP for maximum compatibility. The `SmtpEmailService` supports HTML templates, ensuring that password reset emails look professional and match the BroChat branding.

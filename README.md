# Project Management System (CBE Internal)

## Folder Structure
- **Backend/** – ASP.NET Core Web API with Identity, JWT, Refresh Tokens, AD integration
- **Frontend/** – Reserved for future frontend (React)

## Features
- JWT-based authentication with Microsoft Identity
- Refresh token system
- Active Directory integration (auto-fetch user info)
- Role-based authorization (Admin, Manager, User)
- Background token cleanup
- Session and lockout support

## Developer Setup

### Backend
1. Create a `Backend/appsettings.json` using the `appsettings.json.setup`
2. Run:
   ```bash
   dotnet restore
   dotnet ef database update
   dotnet run

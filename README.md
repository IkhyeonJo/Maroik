# Maroik [https://www.maroik.com]

# Contact: admin@maroik.com

## Introduction
Maroik is a modern web application built with ASP.NET Core MVC, featuring a comprehensive set of tools for personal and business management. It includes features such as expense tracking, calendar management, bulletin boards, and role-based user management.

Mail server: Zoho mail

SMTP Server: Brevo

DDOS Protect: Cloudflare

Domain registration: AWS Route 53

## Key Features

### 1. Personal Finance Management
- Expense tracking and categorization
- Income recording
- Financial reports and analytics
- Export to Excel

### 2. Calendar and Schedule Management
- Monthly calendar view
- Event creation and management

### 3. Bulletin Board System
- Post creation, editing, and deletion
- Comment system
- File attachments
- Search functionality

### 4. User Management
- Role-based access control (Admin/User)
- User profile management
- Session-based authentication
- Password management

### 5. Admin Dashboard
- User management
- System settings
- Analytics dashboard

## Technologies Used

### Backend
- ASP.NET Core MVC 10.0
  - Hot Reload
  - Improved performance
  - Enhanced routing
  - Better dependency injection
- PostgreSQL 17
- Entity Framework Core 10.0
  - Improved query performance
  - Better async support
  - Enhanced change tracking
- Docker
  - Multi-stage builds
  - Container orchestration
  - Environment isolation
- Authentication & Authorization
  - Session Authentication
  - Role-based access control
- Cross-Origin Resource Sharing (CORS)

### Frontend
- AdminLTE 3
- Bootstrap 5.3
- jQuery 3.7
- HTML5/CSS3
- JavaScript ES2024
- TypeScript 7.0
  - The custom client scripts are authored in TypeScript at `Maroik.Website/TypeScripts/{admin,anonymous,user}/custom/**/site.ts` and compiled 1:1 by `tsc` to `Maroik.Website/wwwroot/**/custom/**/site.js` (no bundler, no Babel — the emitted `.js` is byte-faithful to the original, minus comments and an IIFE wrapper).
  - Compiled under `--strict`; each file is an IIFE-wrapped global script (no `import`/`export`).
  - Scripts keep only DOM wiring, widget init, AJAX transport, and notifications — business logic lives in `Maroik.Core.*` (DDD + Clean Architecture). Client-side rule checks are UX mirrors; the server always re-validates.
  - `wwwroot/**/site.js` is build output and git-ignored. Docker Compose builds generate it in the Dockerfile's Node stage; running the site on the host, or running the client-script tests, needs Node 22 + `npm ci` in `Maroik.Website/`.
  - MSBuild runs `tsc` on `dotnet build` when `node_modules/` is present; Docker builds run it in a dedicated `node:22-alpine` stage.
  - Unit-tested with Vitest + jsdom in `Maroik.Website/TypeScripts.Tests/` (one `*.test.ts` per script, exercising the compiled `site.js`).
  - Details: `Maroik.Website/TypeScripts/README.md`.
- NonfactorGrid
- Chart.js 4.4
- Font Awesome 6
- DataTables
- SweetAlert2

### Development Tools
- Visual Studio
- JetBrains Rider
- Git
- Docker Compose
- Postman

## Naming Conventions
- Code projects (assemblies, namespaces, types) follow the .NET Framework Design Guidelines for acronym casing: acronyms of three or more letters use PascalCase (e.g. `Ssl`, `Http`), while two-letter acronyms stay uppercase (e.g. `DB`, `IO`).
- Operational folders that are not code projects — `Maroik.SSL`, `Maroik.DB`, `Maroik.Log`, `Maroik.Crontab`, `Maroik.DeployOps` — intentionally keep their existing casing. These names are referenced by docker-compose volume mounts, deployment scripts, and live directories on the production server, so they are kept stable by design rather than renamed for stylistic consistency.

## Testing
Every source project has a matching `*.Tests` project (xUnit v3 on Microsoft.Testing.Platform), plus the client-script suite and an end-to-end suite. `Maroik.sln` builds them all; CI runs each with coverage (`.github/workflows/build-and-test.yml`, gate: line >= 80 %, branch >= 65 %).

| Tests | What they cover | Needs Docker |
|---|---|---|
| `Maroik.Core.Domain.Tests`, `Maroik.Core.Contract.Tests` | entities, policies, value objects, DTOs, the layering / package-reference architecture rules | no |
| `Maroik.Core.Service.Tests` | use-case orchestration (mocked repositories) | no |
| `Maroik.Core.Repository.Tests` | EF Core repositories against a real PostgreSQL 17 loaded from `Maroik.DB/.../Debugging/Init.sql` | yes |
| `Maroik.Core.PostgreSQL.Tests` | the EF model against the real schema of both init scripts (tables, columns, keys, foreign keys, indexes) | yes |
| `Maroik.Core.Client.Tests` | mail (in-process SMTP server), file storage, ClamAV, RabbitMQ publisher and health check (real RabbitMQ) | yes |
| `Maroik.Worker.Tests` | the e-mail consumer and the worker host, against a real RabbitMQ and an SMTP relay | yes |
| `Maroik.FileStorage.Tests` | the file-storage service | no |
| `Maroik.Website.Tests` | controllers, filters, views and start-up through `WebApplicationFactory` against a real PostgreSQL | yes |
| `Maroik.Website/TypeScripts.Tests` | every client script (Vitest + jsdom), run with `npm test` in `Maroik.Website/` | no |
| `Maroik.E2E.Tests` | the running site in a real browser (Playwright) | yes |

```bash
dotnet build Maroik.sln -p:RunClientScriptTests=false
dotnet test --project Maroik.Core.Service.Tests            # any single project
(cd Maroik.Website && npm ci && npm test)                  # client scripts
```

## Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- Docker and Docker Compose
- Visual Studio (Windows) or JetBrains Rider (Linux/Mac)

### Running the Project

#### Windows
1. Install Visual Studio
2. Open Maroik.sln
3. Set docker-compose as startup project
4. Press F5 to run in debug mode

#### Linux/Mac
1. Install JetBrains Rider
2. Start debug mode with docker-compose.debug.yml
3. Configure mail settings in appsettings files

## Deployment

### Ubuntu Server 24.04.4
1. Configure .env.prod with domain name and mail settings
2. Set email credentials in DBBackup.sh and UploadFileBackup.sh
3. Copy Maroik folder to /home/ubuntu/
4. Open ports 80, 443
5. Run Deploy.sh script

## Default Accounts

### Admin Account
- ID: admin@maroik.com
- Password: ghfkddl12gh!@

### User Account
- ID: user@maroik.com
- Password: ghfkddl12gh!@

## Screenshots

### Login
![Login](https://user-images.githubusercontent.com/20404991/132020270-488a1ab7-448c-44d9-938a-40ce32d6d364.jpg)

### User Dashboard
![User-Dashboard](https://user-images.githubusercontent.com/20404991/132020299-e5adb366-9041-44f9-ad56-f2bb606028d5.jpg)

### NonfactorGrid
![NonfactorGrid](https://user-images.githubusercontent.com/20404991/132020455-e66897ef-ece8-4e71-b323-6ebb72f6b110.jpg)

### User Profile
![UserProfile](https://user-images.githubusercontent.com/20404991/132020484-4b633287-a1b1-48b0-8340-ae3ead83235a.jpg)

## Contributing
1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Support
For issues and questions, please use the GitHub issue tracker.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Special Notes
- Mail account configuration is required for deployment.

# Salama — Clinic Management System

A full-stack clinic management system with an ASP.NET Core 10 Web API backend and a vanilla HTML/CSS/JS frontend.

## Features

- **Patient Portal** — Register, book appointments (with server-side doctor filtering), view diagnoses, medical history
- **Doctor Dashboard** — Manage appointments (complete/cancel), view patients, create diagnoses, manage certificates/clinics
- **Admin Dashboard** — Full CRUD for doctors, patients, clinics, specializations, certificates, and assignments
- **JWT Authentication** — Role-based access (Admin/Doctor/Patient) with refresh tokens
- **Dark/Light Mode** — Toggle on all pages; sidebar stays dark by design
- **Responsive Design** — Bootstrap 5.3.7 with mobile support

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, Entity Framework Core (database-first), SQL Server |
| Frontend | Vanilla HTML/CSS/JS, Bootstrap 5, BootstrapMade "Clinic" template |
| Auth | JWT with refresh tokens, role claims |

## Project Structure

```
├── Backend/
│   └── Salama/
│       ├── Controllers/          # 9 controllers, 65+ endpoints
│       │   ├── AuthController.cs
│       │   ├── AdminController.cs
│       │   ├── DoctorsController.cs
│       │   ├── AppointmentsController.cs
│       │   ├── DoctorDashboardController.cs
│       │   ├── PatientDashboardController.cs
│       │   ├── ClinicsController.cs
│       │   ├── SpecializationsController.cs
│       │   └── PatientsController.cs
│       ├── Models/               # EF Core entities + DTOs
│       ├── Data/                 # AppDbContext
│       ├── Helpers/              # JWT config
│       └── Program.cs
├── Frontend/
│   ├── assets/
│   │   ├── css/                  # main.css + dashboard.css (dark mode)
│   │   ├── js/                   # api.js, auth.js, theme.js, main.js
│   │   ├── img/                  # Images
│   │   └── vendor/               # Bootstrap, AOS, etc.
│   ├── index.html                # Homepage
│   ├── login.html / signup.html  # Auth pages
│   ├── admin.html                # Admin dashboard
│   ├── doctor-dashboard.html     # Doctor dashboard
│   ├── patient-dashboard.html    # Patient dashboard
│   ├── clinics.html              # Clinics listing
│   ├── doctors.html              # Doctors listing
│   ├── appointment.html          # Book appointment
│   └── ...                       # About, Departments, Contact, etc.
└── README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full)
- Modern web browser

## Setup

### 1. Database

Run these SQL commands to create the required columns and database:

```sql
-- Create the Salamaty database (or let EF handle it)
CREATE DATABASE Salamaty;
GO

USE Salamaty;
GO

-- Add refresh token columns to Users table (if not already present)
ALTER TABLE Users ADD RefreshToken NVARCHAR(500) NULL;
ALTER TABLE Users ADD RefreshTokenExpiry DATETIME2 NULL;
```

### 2. Backend

```bash
cd Backend/Salama
dotnet restore
dotnet run
```

The API will start at `http://localhost:5181` (or the port in `Properties/launchSettings.json`).

Update `appsettings.json` if your SQL Server connection string differs:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Trusted_Connection=True;Database=Salamaty;TrustServerCertificate=True"
  }
}
```

### 3. Frontend

Serve the `Frontend/` directory with any static file server:

```bash
# Using Python
cd Frontend
python -m http.server 8080

# Using Node.js (npx)
npx serve Frontend
```

Open `http://localhost:8080` in your browser.

## API Endpoints

### Auth (6+1)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Auth/register` | Patient registration |
| POST | `/api/Auth/login` | Login (returns JWT + refresh token) |
| POST | `/api/Auth/refresh` | Refresh access token |
| GET | `/api/Auth/me` | Get current user |
| POST | `/api/Auth/change-password` | Change password |
| POST | `/api/Auth/forgot-password` | Forgot password |
| POST | `/api/Auth/upload-profile-picture` | Upload profile picture |

### Public
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/Doctors` | List doctors (supports `?specializationId=&clinicId=&location=&search=&clinicName=`) |
| GET | `/api/Clinics` | List all clinics |
| GET | `/api/Specializations` | List all specializations |

### Admin (25)
Full CRUD for: Doctors, Patients, Clinics, Specializations, Certificates, Doctor-Clinic assignments, Doctor-Certificate assignments, Dashboard statistics.

### Doctor Dashboard (12)
Profile, upcoming/completed appointments, complete/cancel, patients, patient history, diagnoses CRUD, certificates, clinics.

### Patient Dashboard (6)
Profile, appointments (with filter), cancel (>72h rule), diagnoses, full history.

## Roles

- **Admin** — Created directly in DB. Full system management.
- **Doctor** — Created by Admin. Manages own appointments, patients, diagnoses.
- **Patient** — Self-registers via Sign Up. Books appointments, views diagnoses/history.

## License

Private project.

---

## Deploy to Azure (Free Tier)

### Step 1: Create Azure Account

1. Go to https://azure.microsoft.com/free
2. Sign up for free ($200 credit for 30 days, free services continue after)

### Step 2: Create SQL Database

1. Go to Azure Portal → https://portal.azure.com
2. Click **Create a resource** → **SQL Database**
3. Fill in:
   - **Resource group**: Create new → `salama-rg`
   - **Database name**: `Salamaty`
   - **Server**: Create new → pick a name like `salama-sql server`, set admin login/password
   - **Compute + storage**: Configure → select **Basic** (cheapest, ~$5/month, or look for free tier)
4. Click **Review + Create** → **Create**
5. Once deployed, go to the SQL server → **Networking** → check **Allow Azure services** → Save
6. Note down: **Server name**, **Admin login**, **Password**

### Step 3: Create App Service (API Backend)

1. Azure Portal → **Create a resource** → **Web App**
2. Fill in:
   - **Resource group**: `salama-rg` (same as above)
   - **Name**: `salama-api` (will be your URL: `salama-api.azurewebsites.net`)
   - **Runtime stack**: .NET 10
   - **Operating System**: Windows
   - **Region**: East US or closest to you
3. Click **Review + Create** → **Create**
4. Once deployed, go to the App Service → **Configuration** → **Application settings**
5. Add a new setting:
   - **Name**: `DefaultConnection`
   - **Value**: `Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=Salamaty;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
6. Also set `SCM_DO_BUILD_DURING_DEPLOYMENT` = `true`
7. Click **Save**

### Step 4: Deploy Backend Code

**Option A — GitHub Actions (recommended):**

1. Go to your App Service → **Deployment Center**
2. Select **GitHub** → authorize
3. Select your repo → branch: `main`
4. Set **Application stack**: .NET 10
5. Save — it will auto-deploy on every push

**Option B — Manual deploy:**

1. Install Azure CLI: https://docs.microsoft.com/cli/azure/install-azure-cli
2. Run:
   ```bash
   az login
   cd Backend/Salama
   az webapp deploy --resource-group salama-rg --name salama-api --src-path bin/Release/net10.0/publish.zip --type zip
   ```

### Step 5: Create Static Site (Frontend)

1. Azure Portal → **Create a resource** → **Static Web App**
2. Fill in:
   - **Resource group**: `salama-rg`
   - **Name**: `salama-frontend`
   - **Source**: GitHub
   - **Organization/Repository**: your repo
   - **Branch**: main
   - **Build Preset**: Custom
   - **App location**: `/Frontend`
   - **Api location**: (leave empty)
   - **Output location**: `/`
3. Click **Create**
4. The frontend will be live at `https://salama-frontend.azurestaticapps.net`

### Step 6: Create Admin User

After deployment, you need to create an admin user directly in Azure SQL:

1. Go to Azure SQL Database → **Query editor** (in portal)
2. Login with your admin credentials
3. Run:
   ```sql
   -- Create admin user (password is hashed with BCrypt)
   INSERT INTO Users (Name, Email, Phone, PasswordHash, Role, CreatedAt)
   VALUES ('Admin', 'admin@salama.com', '01012345678',
           '$2a$11$...hashed_password...', 'Admin', GETDATE());
   ```
   
   To generate the hashed password, run locally:
   ```bash
   dotnet run --project Backend/Salama
   # Then in a browser console on the signup page:
   # BCrypt.hashSync('YourAdminPassword', 10)
   ```

### Your URLs

| Service | URL |
|---------|-----|
| API Backend | `https://salama-api.azurewebsites.net` |
| Frontend | `https://salama-frontend.azurestaticapps.net` |
| Swagger (dev only) | `https://salama-api.azurewebsites.net/swagger` |

### Updating the API URL

If your API URL is different from `salama-api.azurewebsites.net`, update it in `Frontend/assets/js/api.js`:

```javascript
const API_BASE = (location.hostname === 'localhost' || location.hostname === '127.0.0.1')
  ? 'http://localhost:5181/api'
  : 'https://YOUR-API-NAME.azurewebsites.net/api';
```

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
│       ├── Data/                   # AppDbContext
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

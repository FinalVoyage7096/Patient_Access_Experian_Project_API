# Patient_Access_Experian_Project_API

[![CI](https://github.com/FinalVoyage7096/Patient_Access_Experian_Project_API/actions/workflows/ci.yml/badge.svg)](https://github.com/FinalVoyage7096/Patient_Access_Experian_Project_API/actions/workflows/ci.yml)

<img width="998" height="938" alt="image" src="https://github.com/user-attachments/assets/07fca733-4b98-411e-8322-8360b541cb3c" />

A small full-stack healthcare demo built with ASP.NET Core, Entity Framework Core, SQL Server, and a React/TypeScript/Tailwind client. It includes both a patient-access scheduling flow and a revenue-cycle transactions workflow.

<img width="1083" height="667" alt="image" src="https://github.com/user-attachments/assets/7aac67d0-6fd6-4a83-881e-b42c718bd2df" />

This project supports:
- Creating claims with service lines (CPT/HCPCS-style codes)
- Submitting claims with Idempotency-Key support
- Mock adjudication that writes immutable ledger transactions (Submit/Adjust/Pay/Deny)
- Revenue Cycle Dashboard (KPIs + recent claims + per-claim ledger modal)
- One-click Seed Demo Data to populate the dashboard instantly
- Browsing providers, clinics, and patients
- Viewing provider availability slots
- Booking, cancelling, and completing appointments
- Running a mock coverage eligibility check that writes an audit log
- CI build + test via GitHub Actions

## TECH STACK
### Backend
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server (dev)
- Swagger / OpenAPI
### Frontend
- React + TypeScript (Vite)
- TailwindCSS
### Testing
- Unit + integration tests (xUnit)
- EF Core SQLite in-memory for integration tests
### CI
- GitHub Actions workflow (.github/workflows/ci.yml)

## Repo Structure
```
Patient_Access_Experian_Project_API/          # ASP.NET Core Web API
Patient_Access_Experian_Project_API.Tests/    # Tests (unit + integration)
client/                                       # React app (Vite + TS + Tailwind)
.github/workflows/ci.yml                      # CI pipeline
```

## Getting Started
### Prereqs
- .NET SDK (matches project target framework)
- SQL Server (SQLExpress is fine) + SSMS (optional)
- Node.js + npm (for the client)

## Backend Setup
### Configure connection string 
```
{
  "ConnectionStrings": {
    "PatientAccessDb": "Server=.\\SQLEXPRESS;Database=PatientAccessDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Apply EntityFramework Core Migrations 
```
dotnet ef database update --project Patient_Access_Experian_Project_API
```
### Run the API
In Visual Studio: click https (green play button)
Or CLI: 
```
dotnet run --project Patient_Access_Experian_Project_API
```
Swagger should be available at 
```
https://localhost:<port>/swagger
```

## Frontend Setup
### Install Dependencies
```
cd client
npm install
```
### Configure proxy (Vite)
In client/vite.config.ts, ensure you proxy API calls:
```
server: {
  proxy: {
    "/api": "https://localhost:<YOUR_API_PORT>"
  }
}
```
### Run the client 
```
npm run dev
```
Open:
```
http://localhost:5173
```

## API Endpoints (High-level)
### Providers 
```
GET /api/providers
GET /api/providers/{providerId}
GET /api/providers/{providerId}/slots?fromUtc=...&toUtc=...&slotMinutes=30&clinicId=...
```
### Clinics
```
GET /api/clinics
```
### Patients
```
GET /api/patients
```
### Appointments
```
POST /api/appointments
GET /api/appointments/{appointmentId}
GET /api/appointments?clinicId=...&providerId=...&fromUtc=...&toUtc=...
POST /api/appointments/{appointmentId}/cancel
POST /api/appointments/{appointmentId}/complete
```
### Coverage
```
POST /api/coverage/eligibility
GET /api/coverage/logs?take=50&skip=0&patientId=...&fromUtc=...&toUtc=...
```
### Revenue Cycle (Claims & Ledger)
```
GET /api/payers
POST /api/payers

POST /api/claims
GET /api/claims
GET /api/claims/{claimId}
POST /api/claims/{claimId}/submit           (requires Idempotency-Key header)
POST /api/claims/{claimId}/adjudicate
GET /api/claims/{claimId}/transactions

GET /api/reconciliation/claims-summary?clinicId=...&payerId=...&fromUtc=...&toUtc=...
```
### Demo (Populate Dashboard)
```
POST /api/demo/seed-claims?clinicId=...&payerId=...&count=12&daysBack=30
```

## Tests
### Run all tests
```
dotnet test
```
Integration tests use SQLite in-memory with a custom WebApplicationFactory.
You do not need to manually run the API to run tests.

## CI
This repo includes a GitHub Actions workflow that:
- Restores dependencies
- Builds
- Runs tests

## Demo Workflow
### Scheduling (Patient Access)
- Open the React app
- Pick a Clinic, Patient, Provider
- Load available slots
- Click a slot to book an appointment
- Optionally run Coverage Eligibility and view Coverage Logs

### Revenue Cycle Dashboard
- Go to /dashboard
- Select a Clinic (Hospital)
- (Optional) Select a Payer
- Click Seed Demo Data
- KPI cards + Recent Claims populate immediately
- Click View ledger to see immutable transactions (Submit/Adjust/Pay/Deny)

## Future Improvements 
- Auth (JWT)
- Appointment reschedule endpoint
- Realistic coverage “rules engine”
- Docker + docker-compose (API + SQL Server + client)
- More dashboard metrics (trend charts, short-pay detection, time-to-pay distribution)

## License 
This project is for educational/demo purposes.




























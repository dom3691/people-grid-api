# PeopleGrid Backend

Enterprise HR Management System backend starter architecture.

## Architecture

- `PeopleGrid.Api` - ASP.NET Core Web API, controllers, middleware, auth, Swagger
- `PeopleGrid.Application` - application contracts, validators, feature folders
- `PeopleGrid.Domain` - entities and domain base classes
- `PeopleGrid.Infrastructure` - EF Core, SQL Server, tenancy, seeders, services
- `PeopleGrid.Shared` - response models, pagination, shared exceptions
- `PeopleGrid.UnitTests` and `PeopleGrid.IntegrationTests`

## Multi-tenancy

The platform database is `PeopleGrid_PlatformDb` and stores tenants only. Each tenant has its own HR database, resolved by subdomain, login tenant code, or JWT tenant code.

## First Super Admin

The tenant seeder creates:

- Email: `admin@peoplegrid.local`
- Username: `admin`
- Password: `Admin@12345`

Change this immediately before real use.

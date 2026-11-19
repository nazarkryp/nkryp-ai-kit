# AppName solution

<!-- [setup:project-description] Describe what the project does in two or three sentences. -->

Paths and commands in the `.agents/` files of this solution start at the solution folder: the folder
that holds `AppName.slnx`.

## Tech

- ASP.NET Core API (.NET 10, minimal APIs)
- MediatR, FluentValidation, Serilog, Scalar
<!-- @module:docker -->
- Docker
<!-- @end -->

## Services

Each service is one API: `src/AppName.<Service>.*` and `tests/AppName.<Service>.*`. The services share
`AppName.Common` and `AppName.Api.Infrastructure`.
<!-- @module:jwt -->
`AppName.Api.Authentication` checks the access tokens. All services of the solution use the same
signing key, issuer and audience.
<!-- @end -->

<!-- @each-service -->
- **ServiceName**: `src/AppName.ServiceName.Api`, http://localhost:ServicePort.
  <!-- [setup:service-description] Replace this comment with one sentence: what the service does. -->
<!-- @module:auth -->
  - Auth: own accounts. It issues the access tokens of the solution.
<!-- @end -->
<!-- @module:trust -->
  - Auth: accepts the access tokens of the auth service. It has no accounts.
<!-- @end -->
  <!-- [setup:service-database] Replace this comment with one bullet: "- Database: <database> with <data access library>" or "- Database: none". -->
<!-- @end -->

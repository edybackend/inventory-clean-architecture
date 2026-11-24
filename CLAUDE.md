# Engineering rules for AI agents

This file is loaded by Claude Code in every session. It defines how changes are made in this repository.
Hooks in `.claude/settings.json` enforce the parts that can be automated.

## Workflow
1. **Spec first.** A new behavior starts as a spec in `specs/NNN-name.md` (context, behavior, acceptance criteria, out of scope). Code follows the spec, not the other way around.
2. **Small steps.** One concern per commit, [Conventional Commits](https://www.conventionalcommits.org/) (`feat(domain): ...`, `fix(api): ...`).
3. **Definition of done** = the CI gates pass locally: `dotnet build` (warnings are errors), `dotnet test`, `dotnet format --verify-no-changes`, and in `web/`: `npm test && npm run build`. The Stop hook blocks finishing while the gates of the touched areas are red.

## Architecture boundaries (enforced by `tests/Inventory.ArchitectureTests`)
- `Inventory.Domain` depends on nothing. Business rules and invariants live here, never in endpoints or handlers.
- `Inventory.Application` depends only on Domain. No EF Core, SQLite or ASP.NET Core. It declares the ports (`IProductRepository`, `IUnitOfWork`, `IProductQueries`).
- `Inventory.Infrastructure` implements the ports with EF Core. `Inventory.Api` only wires and maps HTTP; endpoints never touch the `DbContext`.
- Writes load the aggregate and save through `IUnitOfWork`; reads are no-tracking projections to DTOs.

## Domain rules
- The quantity on hand changes **only** through a movement (`Receive`, `Issue`, `Adjust`). The history is append-only: never update or delete a movement; a correction is a new Adjustment.
- After any change, `QuantityOnHand` equals the sum of the movements. A test guards this; keep it green.
- Every change increments `Version`. The version is the optimistic concurrency token: do not remove it, and never "fix" a 409 by retrying silently.

## Error handling
- Expected failures are values (`Result<T>` with a `Failure`), not exceptions. Exceptions are for bugs and infrastructure faults.
- HTTP status codes are decided in one place (`FailureResults`): validation 400, not found 404, conflict 409, business rule 422. Responses are RFC 9457 problem details.

## C# conventions
- Classes `sealed` by default; file-scoped namespaces; `TimeProvider` instead of `DateTime.Now`; `Guid.CreateVersion7()` for ids.
- No MediatR/AutoMapper: handlers are plain classes injected where they are used; mapping is explicit.
- Logging with `[LoggerMessage]` source generators.

## Angular conventions
- Standalone components, signals for state, lazy-loaded routes. The typed `InventoryApi` service is the only place that builds URLs.
- The client validates for convenience; the API is the authority. Always show the server's problem details.

## Never
- Edit generated files: EF migrations (`dotnet ef migrations add`), `package-lock.json`. Blocked by hook.
- Read or write secrets (`.env`, keys). Blocked by permissions and hook.
- Disable an analyzer or a test to make the build pass. Fix the cause or explain the exception in `.editorconfig` with a comment.
- Add a dependency without a reason stated in the commit message.

# 003 · Concurrent stock changes

## Context
Two people can open the same product and issue units at the same time. Without protection, the second write silently overwrites the first ("lost update") and the system may sell stock it does not have.

## Behavior
- Optimistic concurrency: every product has a `version`, incremented on each movement.
- The client sends the `expectedVersion` it read. If it differs from the current version, the API returns `409` and nothing changes.
- The database update is also guarded by the version (`UPDATE ... WHERE Version = @read`), so two requests that read the same version at the same moment cannot both succeed.
- The UI shows "Stock changed since you opened this product" and reloads the current quantity.

## Acceptance criteria
- Given a stale `expectedVersion`, the API returns `409` with the current version in the problem details.
- Given two simultaneous Issues with the same `expectedVersion`, exactly one succeeds and the other returns `409`; the history has one new movement.
- No lock is held while the user is looking at the screen.

## Out of scope
Automatic retries, pessimistic locking, distributed locks.

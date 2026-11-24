# 002 · Record a stock movement

## Context
The quantity on hand is never edited directly. Every change is a movement in an append-only history, so the balance can always be explained.

## Behavior
- `POST /api/products/{id}/movements` with `type`, `quantity`, `reason` and `expectedVersion` (see spec 003).
- Types:
  - **Receipt** adds `quantity` units (goods received).
  - **Issue** removes `quantity` units (sale, consumption, loss).
  - **Adjustment** sets the balance to `quantity` after a physical count; the movement stores the difference.
- Each movement stores the signed change, the balance after it, the reason and the time it happened.
- Response `200 OK` with the new quantity on hand and the new version.
- `GET /api/products/{id}/movements?page=&pageSize=` returns the history, newest first.

## Acceptance criteria
- Receipt and Issue require `quantity` > 0; Adjustment requires `quantity` ≥ 0 and must change the balance.
- `reason` is required (1-200 characters).
- An Issue larger than the quantity on hand is rejected with `422` and nothing changes.
- After any sequence of movements, the quantity on hand equals the sum of the changes in the history.
- A product whose quantity is lower than its minimum stock is reported as below minimum.

## Out of scope
Reservations, batches and expiry dates, reversing a movement (a correction is a new Adjustment).

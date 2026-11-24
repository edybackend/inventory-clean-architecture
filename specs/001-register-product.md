# 001 · Register a product

## Context
Every stock item is identified by a SKU. A product starts with zero units; stock only enters through movements (spec 002).

## Behavior
- `POST /api/products` with `sku`, `name` and `minimumStock`.
- The SKU is normalized to upper case and must be unique.
- Response `201 Created` with the product id and a `Location` header.
- `GET /api/products?search=&belowMinimum=&page=&pageSize=` lists products (newest first), filtered by SKU or name and, optionally, only those below their minimum stock.
- `GET /api/products/{id}` returns the product with its current quantity, version and whether it is below the minimum.

## Acceptance criteria
- Given a valid SKU (3-20 letters, digits or `-`), a name (1-100 characters) and `minimumStock` ≥ 0, the product is stored with quantity 0 and version 1.
- Given an invalid field, the API returns `400` with a validation problem listing every invalid field.
- Given a SKU that already exists (in any letter case), the API returns `409`.
- `pageSize` is limited to 1-100; the response carries `items`, `page`, `pageSize` and `totalCount`.
- An unknown id returns `404`.

## Out of scope
Editing or deleting products, prices, categories, warehouses.

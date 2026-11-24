export type MovementType = 'Receipt' | 'Issue' | 'Adjustment';

export interface ProductSummary {
  id: string;
  sku: string;
  name: string;
  quantityOnHand: number;
  minimumStock: number;
  isBelowMinimum: boolean;
}

export interface ProductDetails extends ProductSummary {
  version: number;
  createdAt: string;
}

export interface Movement {
  id: string;
  type: MovementType;
  change: number;
  balanceAfter: number;
  reason: string;
  occurredAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MovementRecorded {
  movementId: string;
  quantityOnHand: number;
  version: number;
  isBelowMinimum: boolean;
}

export interface RecordMovementRequest {
  type: MovementType;
  quantity: number;
  reason: string;
  expectedVersion: number;
}

export interface RegisterProductRequest {
  sku: string;
  name: string;
  minimumStock: number;
}

/** RFC 9457 problem details returned by the API for every failure. */
export interface Problem {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

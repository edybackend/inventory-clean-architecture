import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Movement,
  MovementRecorded,
  PagedResult,
  Problem,
  ProductDetails,
  ProductSummary,
  RecordMovementRequest,
  RegisterProductRequest,
} from './product.models';

export interface ProductQuery {
  search: string;
  belowMinimum: boolean;
  page: number;
  pageSize: number;
}

/** Thin typed client for the Inventory API. Components never build URLs. */
@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  listProducts(query: ProductQuery): Observable<PagedResult<ProductSummary>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.search.trim()) {
      params = params.set('search', query.search.trim());
    }
    if (query.belowMinimum) {
      params = params.set('belowMinimum', true);
    }
    return this.http.get<PagedResult<ProductSummary>>(this.baseUrl, { params });
  }

  getProduct(id: string): Observable<ProductDetails> {
    return this.http.get<ProductDetails>(`${this.baseUrl}/${id}`);
  }

  listMovements(id: string, page = 1, pageSize = 20): Observable<PagedResult<Movement>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<Movement>>(`${this.baseUrl}/${id}/movements`, { params });
  }

  registerProduct(request: RegisterProductRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, request);
  }

  recordMovement(id: string, request: RecordMovementRequest): Observable<MovementRecorded> {
    return this.http.post<MovementRecorded>(`${this.baseUrl}/${id}/movements`, request);
  }
}

/** Reads the problem details from a failed call; falls back to a generic message when the body is not JSON. */
export function toProblem(error: unknown): Problem {
  if (error instanceof HttpErrorResponse && error.error && typeof error.error === 'object') {
    return { status: error.status, ...(error.error as Omit<Problem, 'status'>) };
  }
  const status = error instanceof HttpErrorResponse ? error.status : 0;
  return { status, detail: 'The server could not be reached. Try again.' };
}

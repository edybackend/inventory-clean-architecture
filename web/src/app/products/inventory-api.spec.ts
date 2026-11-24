import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InventoryApi, toProblem } from './inventory-api';

describe('InventoryApi', () => {
  let api: InventoryApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(InventoryApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the filters that are set', () => {
    api.listProducts({ search: '  usb ', belowMinimum: true, page: 2, pageSize: 10 }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.get('search')).toBe('usb');
    expect(request.request.params.get('belowMinimum')).toBe('true');
    expect(request.request.params.get('page')).toBe('2');
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 0 });
  });

  it('omits empty search and the below-minimum flag', () => {
    api.listProducts({ search: ' ', belowMinimum: false, page: 1, pageSize: 10 }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/products');
    expect(request.request.params.has('search')).toBe(false);
    expect(request.request.params.has('belowMinimum')).toBe(false);
    request.flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
  });

  it('posts the expected version with the movement', () => {
    api.recordMovement('p1', { type: 'Issue', quantity: 2, reason: 'Order', expectedVersion: 7 }).subscribe();

    const request = http.expectOne('/api/products/p1/movements');
    expect(request.request.method).toBe('POST');
    expect(request.request.body.expectedVersion).toBe(7);
    request.flush({ movementId: 'm1', quantityOnHand: 3, version: 8, isBelowMinimum: false });
  });
});

describe('toProblem', () => {
  it('reads problem details from the error body', () => {
    const error = new HttpErrorResponse({ status: 422, error: { title: 'Business rule violated', detail: 'Only 2 on hand.' } });

    expect(toProblem(error)).toEqual({ status: 422, title: 'Business rule violated', detail: 'Only 2 on hand.' });
  });

  it('falls back to a generic message when the server is unreachable', () => {
    expect(toProblem(new HttpErrorResponse({ status: 0 })).detail).toContain('could not be reached');
  });
});

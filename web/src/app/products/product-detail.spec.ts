import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProductDetail } from './product-detail';
import { ProductDetails } from './product.models';

const product = (version: number, quantityOnHand: number): ProductDetails => ({
  id: 'p1',
  sku: 'KB-100',
  name: 'Keyboard',
  quantityOnHand,
  minimumStock: 2,
  isBelowMinimum: quantityOnHand < 2,
  version,
  createdAt: '2026-01-01T00:00:00Z',
});

describe('ProductDetail', () => {
  let fixture: ComponentFixture<ProductDetail>;
  let http: HttpTestingController;

  const answerLoad = (details: ProductDetails) => {
    http.expectOne('/api/products/p1').flush(details);
    http.expectOne((r) => r.url === '/api/products/p1/movements').flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
  };

  const submit = (reason: string) => {
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector<HTMLInputElement>('input[formcontrolname="reason"]')!;
    input.value = reason;
    input.dispatchEvent(new Event('input'));
    element.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
  };

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'p1');
    fixture.detectChanges();
    answerLoad(product(3, 10));
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('sends the version it loaded', async () => {
    submit('Order 1');

    const request = http.expectOne('/api/products/p1/movements');
    expect(request.request.body).toEqual({ type: 'Issue', quantity: 1, reason: 'Order 1', expectedVersion: 3 });
    request.flush({ movementId: 'm1', quantityOnHand: 9, version: 4, isBelowMinimum: false });
    answerLoad(product(4, 9));
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Saved. On hand: 9.');
  });

  it('on 409 explains the conflict and reloads the current stock', async () => {
    submit('Order 2');

    http
      .expectOne('/api/products/p1/movements')
      .flush({ status: 409, detail: 'Stock changed' }, { status: 409, statusText: 'Conflict' });
    answerLoad(product(5, 4));
    await fixture.whenStable();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Stock changed since you opened this product');
    expect(text).toContain('version 5');
    expect(fixture.nativeElement.querySelector('.stock-number').textContent).toBe('4');
  });

  it('on 422 shows the business rule message from the API', async () => {
    submit('Order 3');

    http
      .expectOne('/api/products/p1/movements')
      .flush({ status: 422, detail: 'Cannot issue 50 unit(s): only 10 on hand.' }, { status: 422, statusText: 'Unprocessable' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('only 10 on hand');
  });
});

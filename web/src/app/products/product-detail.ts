import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryApi, toProblem } from './inventory-api';
import { Movement, MovementType, PagedResult, ProductDetails } from './product.models';

type Notice = { kind: 'success' | 'error' | 'conflict'; text: string };

@Component({
  selector: 'app-product-detail',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './product-detail.html',
})
export class ProductDetail implements OnInit {
  private readonly api = inject(InventoryApi);

  /** Route parameter, bound by withComponentInputBinding(). */
  readonly id = input.required<string>();

  protected readonly product = signal<ProductDetails | null>(null);
  protected readonly history = signal<PagedResult<Movement> | null>(null);
  protected readonly notice = signal<Notice | null>(null);
  protected readonly saving = signal(false);
  protected readonly historyPage = signal(1);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    type: ['Issue' as MovementType, Validators.required],
    quantity: [1, [Validators.required, Validators.min(0)]],
    reason: ['', [Validators.required, Validators.maxLength(200)]],
  });

  private readonly formType = toSignal(this.form.controls.type.valueChanges, { initialValue: this.form.controls.type.value });

  protected readonly quantityLabel = computed(() => (this.formType() === 'Adjustment' ? 'Counted quantity' : 'Quantity'));

  ngOnInit(): void {
    this.reload();
  }

  protected submit(): void {
    const product = this.product();
    if (!product || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const { type, quantity, reason } = this.form.getRawValue();

    // expectedVersion is the version this screen loaded: if someone else changed the product since, the API answers 409.
    this.api.recordMovement(product.id, { type, quantity, reason, expectedVersion: product.version }).subscribe({
      next: (recorded) => {
        this.notice.set({ kind: 'success', text: `Saved. On hand: ${recorded.quantityOnHand}.` });
        this.form.controls.reason.reset('');
        this.saving.set(false);
        this.reload();
      },
      error: (error: unknown) => {
        const problem = toProblem(error);
        if (problem.status === 409) {
          this.notice.set({
            kind: 'conflict',
            text: 'Stock changed since you opened this product. The current quantity was reloaded; check it and try again.',
          });
          this.reload();
        } else {
          this.notice.set({ kind: 'error', text: problem.detail ?? 'Could not record the movement.' });
        }
        this.saving.set(false);
      },
    });
  }

  protected goToHistoryPage(page: number): void {
    this.historyPage.set(page);
    this.loadHistory();
  }

  protected totalPages(result: PagedResult<Movement>): number {
    return Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  }

  private reload(): void {
    this.api.getProduct(this.id()).subscribe({
      next: (product) => this.product.set(product),
      error: (error: unknown) => this.notice.set({ kind: 'error', text: toProblem(error).detail ?? 'Product not found.' }),
    });
    this.historyPage.set(1);
    this.loadHistory();
  }

  private loadHistory(): void {
    this.api.listMovements(this.id(), this.historyPage(), 10).subscribe({
      next: (history) => this.history.set(history),
      error: () => this.history.set(null),
    });
  }
}

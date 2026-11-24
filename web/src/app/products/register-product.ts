import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { InventoryApi, toProblem } from './inventory-api';

@Component({
  selector: 'app-register-product',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register-product.html',
})
export class RegisterProduct {
  private readonly api = inject(InventoryApi);
  private readonly router = inject(Router);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    sku: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9-]{3,20}$/)]],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    minimumStock: [0, [Validators.required, Validators.min(0)]],
  });

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly fieldErrors = signal<Record<string, string[]>>({});

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.api.registerProduct(this.form.getRawValue()).subscribe({
      next: ({ id }) => void this.router.navigate(['/products', id]),
      error: (error: unknown) => {
        // The client validates for convenience; the server is the authority (400 per field, 409 duplicate SKU).
        const problem = toProblem(error);
        this.fieldErrors.set(problem.errors ?? {});
        this.error.set(problem.detail ?? 'Could not save the product.');
        this.saving.set(false);
      },
    });
  }
}

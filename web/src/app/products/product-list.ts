import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryApi, toProblem } from './inventory-api';
import { PagedResult, ProductSummary } from './product.models';

@Component({
  selector: 'app-product-list',
  imports: [FormsModule, RouterLink],
  templateUrl: './product-list.html',
})
export class ProductList implements OnInit {
  private readonly api = inject(InventoryApi);

  protected readonly pageSize = 10;
  protected readonly search = signal('');
  protected readonly belowMinimum = signal(false);
  protected readonly page = signal(1);
  protected readonly result = signal<PagedResult<ProductSummary> | null>(null);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  protected applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected totalPages(result: PagedResult<ProductSummary>): number {
    return Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  }

  private load(): void {
    this.api
      .listProducts({ search: this.search(), belowMinimum: this.belowMinimum(), page: this.page(), pageSize: this.pageSize })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.error.set(null);
        },
        error: (error: unknown) => this.error.set(toProblem(error).detail ?? 'Could not load products.'),
      });
  }
}

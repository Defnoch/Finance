import { Injectable, signal } from '@angular/core';
import { CategoryDto } from '../core/api/api-client';
import { ApiClientWrapperService } from '../core/services/api-client-wrapper.service';

@Injectable({ providedIn: 'root' })
export class CategoryStore {
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  private readonly loaded = signal(false);

  constructor(private apiClientWrapper: ApiClientWrapperService) {}

  load() {
    if (this.loading() || this.loaded()) return;

    this.loading.set(true);
    this.error.set(null);

    this.apiClientWrapper.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loaded.set(true);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Fout bij ophalen categorieën');
        this.loading.set(false);
      }
    });
  }

  reload() {
    this.loaded.set(false);
    this.load();
  }
}

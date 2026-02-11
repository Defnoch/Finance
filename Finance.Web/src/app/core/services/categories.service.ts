import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-tokens';
import { CategoryDto as ModelCategoryDto } from '../api/api-client';
import { AssignCategoryCommand as ModelAssignCategoryCommand } from '../models/assign-category.command';
import { UpdateCategoryCommand as ModelUpdateCategoryCommand } from '../models/update-category.command';
import { ApiClient, CategoryDto as ApiCategoryDto, AssignCategoryCommand as ApiAssignCategoryCommand, UpdateCategoryCommand as ApiUpdateCategoryCommand, CreateCategoryCommand } from '../api/api-client';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly api: ApiClient;

  constructor(
    http: HttpClient,
    @Inject(API_BASE_URL) baseUrl: string
  ) {
    this.api = new ApiClient(http, baseUrl);
  }

  getCategories(): Observable<ModelCategoryDto[]> {
    return this.api.categoriesAll();
  }

  assignCategory(command: ModelAssignCategoryCommand): Observable<void> {
    const apiCommand = new ApiAssignCategoryCommand();
    apiCommand.transactionId = command.transactionId;
    apiCommand.categoryId = command.categoryId;
    return this.api.assign(apiCommand);
  }

  addCategory(category: { name: string; kind: string; colorHex?: string | null }): Observable<ModelCategoryDto> {
    const command = new CreateCategoryCommand();
    command.name = category.name;
    command.kind = category.kind;
    command.colorHex = category.colorHex ?? undefined;
    return this.api.categoriesPOST(command);
  }

  updateCategory(command: ModelUpdateCategoryCommand): Observable<ModelCategoryDto> {
    const apiCommand = new ApiUpdateCategoryCommand();
    apiCommand.categoryId = command.categoryId;
    apiCommand.name = command.name;
    apiCommand.kind = command.kind;
    apiCommand.colorHex = command.colorHex ?? undefined;
    return this.api.categoriesPUT(command.categoryId, apiCommand);
  }

  deleteCategory(categoryId: string): Observable<void> {
    return this.api.categoriesDELETE(categoryId);
  }
}

import { Inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api-tokens';
import { ImportResultDto } from '../models/import-result.dto';

@Injectable({ providedIn: 'root' })
export class ImportService {
  constructor(
    private http: HttpClient,
    @Inject(API_BASE_URL) private baseUrl: string
  ) {}

  uploadIngCsv(file: File): Observable<ImportResultDto> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<ImportResultDto>(`${this.baseUrl}/api/import/ing`, formData);
  }
}


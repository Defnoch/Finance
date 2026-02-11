import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiClientWrapperService } from '../../core/services/api-client-wrapper.service';
import { ImportResultDto } from '../../core/api/api-client';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [CommonModule, FormsModule, MatCardModule, MatCheckboxModule, MatButtonModule, MatListModule],
  templateUrl: './import.component.html',
  styleUrls: ['./import.component.scss'],
})
export class ImportComponent {
  selectedFile: File | null = null;
  selectedSpaarFile: File | null = null;
  result: ImportResultDto | null = null;
  spaarResult: ImportResultDto | null = null;
  isLoading = false;
  isSpaarLoading = false;
  error: string | null = null;
  spaarError: string | null = null;
  overrideExisting = false;
  overrideSpaarExisting = false;

  constructor(private api: ApiClientWrapperService) {}

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
    } else {
      this.selectedFile = null;
    }
  }

  onSpaarFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedSpaarFile = input.files[0];
    } else {
      this.selectedSpaarFile = null;
    }
  }

  onUpload(): void {
    if (!this.selectedFile) {
      this.error = 'Selecteer eerst een CSV-bestand.';
      return;
    }

    this.isLoading = true;
    this.error = null;
    this.result = null;

    this.api.uploadAsnCsv(this.selectedFile, this.overrideExisting).subscribe({
      next: (res: ImportResultDto) => {
        this.result = res;
        this.isLoading = false;
      },
      error: (err: unknown) => {
        this.error = 'Uploaden mislukt.';
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  onSpaarUpload(): void {
    if (!this.selectedSpaarFile) {
      this.spaarError = 'Selecteer eerst een CSV-bestand.';
      return;
    }

    this.isSpaarLoading = true;
    this.spaarError = null;
    this.spaarResult = null;

    this.api.uploadAsnSpaar(this.selectedSpaarFile, this.overrideSpaarExisting).subscribe({
      next: (res: ImportResultDto) => {
        this.spaarResult = res;
        this.isSpaarLoading = false;
      },
      error: (err: unknown) => {
        this.spaarError = 'Uploaden mislukt.';
        console.error(err);
        this.isSpaarLoading = false;
      }
    });
  }
}

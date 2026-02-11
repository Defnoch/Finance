import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';

@Component({
  selector: 'app-details-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatTableModule],
  templateUrl: './details-dialog.component.html',
  styleUrls: ['./details-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DetailsDialogComponent {
  detailsName: string;
  detailsTransactions: any[];
  readonly dialogRef = inject(MatDialogRef<DetailsDialogComponent>);

  constructor() {
    const data = inject(MAT_DIALOG_DATA);
    this.detailsName = data.detailsName;
    this.detailsTransactions = data.detailsTransactions;
  }

  close() {
    this.dialogRef.close();
  }
}

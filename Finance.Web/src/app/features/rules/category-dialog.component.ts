import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import {
    MatDialogRef,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MAT_DIALOG_DATA
} from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-category-dialog',
    standalone: true,
    imports: [
        CommonModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        FormsModule,
        MatDialogTitle,
        MatDialogContent,
        MatDialogActions,
        MatDialogClose
    ],
    templateUrl: './category-dialog.component.html',
    styleUrls: ['./category-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CategoryDialogComponent {
    category: any;
    isNew: boolean;
    readonly dialogRef = inject(MatDialogRef<CategoryDialogComponent>);

    constructor() {
        // Data wordt via MAT_DIALOG_DATA geïnjecteerd
        const data = inject(MAT_DIALOG_DATA, { optional: true });
        this.category = data?.category ? { ...data.category } : { name: '', kind: 'Expense', colorHex: '#2196F3' };
        this.isNew = data?.isNew ?? true;
    }

    save() {
        this.dialogRef.close(this.category);
    }

    cancel() {
        this.dialogRef.close(null);
    }
}

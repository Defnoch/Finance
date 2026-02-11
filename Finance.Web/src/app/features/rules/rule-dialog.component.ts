import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {MatButtonModule} from '@angular/material/button';
import {
    MatDialogRef,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MAT_DIALOG_DATA
} from '@angular/material/dialog';
import {CommonModule} from '@angular/common';
import {MatFormFieldModule} from '@angular/material/form-field';
import {MatInputModule} from '@angular/material/input';
import {MatSelectModule} from '@angular/material/select';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';

@Component({
    selector: 'app-rule-dialog',
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
        MatDialogClose,
        MatIconModule
    ],
    templateUrl: './rule-dialog.component.html',
    styleUrls: ['./rule-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RuleDialogComponent {
    rule: any;
    isNew: boolean;
    categories: any[];
    readonly dialogRef = inject(MatDialogRef<RuleDialogComponent>);
    constructor() {
        const data = inject(MAT_DIALOG_DATA, {optional: true});
        this.rule = data?.rule ? { ...data.rule } : { name: '', priority: 1, categoryId: '', conditions: [], isEnabled: true };
        this.isNew = data?.isNew ?? true;
        this.categories = data?.categories ?? [];
    }
    save() {
        this.dialogRef.close(this.rule);
    }
    cancel() {
        this.dialogRef.close(null);
    }
    addCondition() {
        this.rule.conditions.push({ id: crypto.randomUUID(), field: '', operator: '', value: '' });
    }
    removeCondition(idx: number) {
        this.rule.conditions.splice(idx, 1);
    }
}

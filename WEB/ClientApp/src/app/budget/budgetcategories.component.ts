import { Component as NgComponent, OnInit, ViewChild, TemplateRef } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { PagingHeaders } from '../common/models/http.model';
import { BudgetCategory, BudgetCategorySearchOptions, BudgetCategorySearchResponse } from '../common/models/budget.model';
import { BudgetService } from '../common/services/budget.service';
import { ErrorService } from '../common/services/error.service';
import { AuthService } from '../common/services/auth.service';

@NgComponent({
    selector: 'app-budgetcategories',
    templateUrl: './budgetcategories.component.html',
    standalone: false
})
export class BudgetCategoriesComponent implements OnInit {

    public budgetCategories: BudgetCategory[] = [];
    public searchOptions = new BudgetCategorySearchOptions();
    public headers = new PagingHeaders();
    public loading = false;
    public editing: BudgetCategory;
    public saving = false;

    private modal: NgbModalRef;

    @ViewChild('categoryModal') categoryModal: TemplateRef<unknown>;

    constructor(
        private modalService: NgbModal,
        private budgetService: BudgetService,
        private errorService: ErrorService,
        private authService: AuthService
    ) {
    }

    ngOnInit(): void {
        this.searchOptions.pageSize = 0;
        this.load();
    }

    get canEdit(): boolean {
        return this.authService.isInRole("Manager") || this.authService.isInRole("Administrator");
    }

    load(): void {
        this.loading = true;
        this.searchOptions.includeChildren = true;
        this.searchOptions.pageIndex = 0;

        this.budgetService.searchCategories(this.searchOptions)
            .subscribe({
                next: (response: BudgetCategorySearchResponse) => {
                    this.budgetCategories = response.budgetCategories;
                    this.headers = response.headers;
                    this.loading = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Budget Categories", "Load");
                    this.loading = false;
                }
            });
    }

    add(): void {
        this.editing = new BudgetCategory();
        this.editing.sortOrder = this.budgetCategories.length;
        this.openModal();
    }

    edit(category: BudgetCategory): void {
        this.editing = new BudgetCategory();
        Object.assign(this.editing, category);
        this.openModal();
    }

    private openModal(): void {
        this.modal = this.modalService.open(this.categoryModal, { centered: true });
    }

    save(): void {
        if (!this.editing.name || !this.editing.name.trim()) return;

        this.saving = true;

        this.budgetService.saveCategory(this.editing)
            .subscribe({
                next: () => {
                    this.saving = false;
                    if (this.modal) this.modal.close();
                    this.load();
                },
                error: err => {
                    this.errorService.handleError(err, "Budget Category", "Save");
                    this.saving = false;
                }
            });
    }

    remove(category: BudgetCategory): void {
        if (category.lineCount > 0) return;

        if (!confirm(`Delete the budget category "${category.name}"?`)) return;

        this.budgetService.deleteCategory(category.budgetCategoryId)
            .subscribe({
                next: () => this.load(),
                error: err => this.errorService.handleError(err, "Budget Category", "Delete")
            });
    }

    cancel(): void {
        if (this.modal) this.modal.dismiss();
    }

}

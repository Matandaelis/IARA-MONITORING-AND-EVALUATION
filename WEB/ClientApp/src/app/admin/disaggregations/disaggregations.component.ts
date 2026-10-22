import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';
import { ToastrService } from 'ngx-toastr';
import { DisaggregationGroup, DisaggregationOption } from '../../common/models/disaggregation.model';
import { DisaggregationService } from '../../common/services/disaggregation.service';
import { ErrorService } from '../../common/services/error.service';
import { AuthService } from '../../common/services/auth.service';

@Component({
    selector: 'app-disaggregations',
    templateUrl: './disaggregations.component.html',
    standalone: false
})
export class DisaggregationsComponent implements OnInit {

    public groups: DisaggregationGroup[] = [];
    public filteredGroups: DisaggregationGroup[] = [];
    public searchTerm = '';
    public showInactive = false;
    public loading = false;
    public saving = false;

    public editingGroup: DisaggregationGroup = new DisaggregationGroup();
    public isNew = true;

    private modalRef: NgbModalRef;

    @ViewChild('groupModal') groupModal: TemplateRef<any>;

    constructor(
        private modalService: NgbModal,
        private disaggregationService: DisaggregationService,
        private errorService: ErrorService,
        private toastr: ToastrService,
        private authService: AuthService
    ) { }

    ngOnInit(): void {
        this.load();
    }

    get canEdit(): boolean {
        return this.authService.isInRole('Manager') || this.authService.isInRole('Administrator');
    }

    load(): void {
        this.loading = true;
        this.disaggregationService.getGroups(this.showInactive)
            .subscribe({
                next: (groups) => {
                    this.groups = groups || [];
                    this.filter();
                    this.loading = false;
                },
                error: (err) => {
                    this.errorService.handleError(err, 'Disaggregation Groups', 'Load');
                    this.loading = false;
                }
            });
    }

    filter(): void {
        const term = (this.searchTerm || '').trim().toLowerCase();
        if (!term) {
            this.filteredGroups = [...this.groups];
            return;
        }

        this.filteredGroups = this.groups.filter(g =>
            (g.name && g.name.toLowerCase().includes(term)) ||
            (g.description && g.description.toLowerCase().includes(term)) ||
            (g.options && g.options.some(o =>
                (o.name && o.name.toLowerCase().includes(term)) ||
                (o.code && o.code.toLowerCase().includes(term))
            ))
        );
    }

    onShowInactiveChange(): void {
        this.load();
    }

    openCreate(): void {
        this.isNew = true;
        this.editingGroup = new DisaggregationGroup();
        this.editingGroup.isActive = true;
        this.editingGroup.options = [
            { disaggregationOptionId: undefined, disaggregationGroupId: undefined, code: '', name: '', sortOrder: 1 },
            { disaggregationOptionId: undefined, disaggregationGroupId: undefined, code: '', name: '', sortOrder: 2 }
        ];
        this.openModal();
    }

    openEdit(group: DisaggregationGroup): void {
        this.isNew = false;
        // Deep copy group and options
        this.editingGroup = {
            disaggregationGroupId: group.disaggregationGroupId,
            name: group.name,
            description: group.description,
            isActive: group.isActive !== false,
            createdAt: group.createdAt,
            options: (group.options || []).map(o => ({ ...o })).sort((a, b) => a.sortOrder - b.sortOrder)
        };
        this.openModal();
    }

    private openModal(): void {
        this.modalRef = this.modalService.open(this.groupModal, {
            size: 'lg',
            backdrop: 'static',
            centered: true
        });
    }

    addOptionRow(): void {
        const nextOrder = (this.editingGroup.options?.length || 0) + 1;
        if (!this.editingGroup.options) {
            this.editingGroup.options = [];
        }
        this.editingGroup.options.push({
            disaggregationOptionId: undefined,
            disaggregationGroupId: this.editingGroup.disaggregationGroupId,
            code: '',
            name: '',
            sortOrder: nextOrder
        });
    }

    removeOptionRow(index: number): void {
        this.editingGroup.options.splice(index, 1);
        this.reindexOptions();
    }

    moveOptionUp(index: number): void {
        if (index <= 0) return;
        const temp = this.editingGroup.options[index];
        this.editingGroup.options[index] = this.editingGroup.options[index - 1];
        this.editingGroup.options[index - 1] = temp;
        this.reindexOptions();
    }

    moveOptionDown(index: number): void {
        if (index >= this.editingGroup.options.length - 1) return;
        const temp = this.editingGroup.options[index];
        this.editingGroup.options[index] = this.editingGroup.options[index + 1];
        this.editingGroup.options[index + 1] = temp;
        this.reindexOptions();
    }

    private reindexOptions(): void {
        this.editingGroup.options.forEach((opt, idx) => {
            opt.sortOrder = idx + 1;
        });
    }

    save(): void {
        const name = (this.editingGroup.name || '').trim();
        if (!name) {
            this.toastr.warning('Please enter a group name.', 'Validation');
            return;
        }

        this.editingGroup.name = name;
        this.editingGroup.description = (this.editingGroup.description || '').trim();

        // Validate options
        if (!this.editingGroup.options || this.editingGroup.options.length === 0) {
            this.toastr.warning('Please add at least one child option to this disaggregation group.', 'Validation');
            return;
        }

        const codes = new Set<string>();
        for (let i = 0; i < this.editingGroup.options.length; i++) {
            const opt = this.editingGroup.options[i];
            const optCode = (opt.code || '').trim();
            const optName = (opt.name || '').trim();

            if (!optCode || !optName) {
                this.toastr.warning(`Option #${i + 1} must have both a Code and a Name.`, 'Validation');
                return;
            }

            const codeLower = optCode.toLowerCase();
            if (codes.has(codeLower)) {
                this.toastr.warning(`Option code "${optCode}" is duplicated. Codes must be unique within the group.`, 'Validation');
                return;
            }
            codes.add(codeLower);

            opt.code = optCode;
            opt.name = optName;
            opt.sortOrder = i + 1;
        }

        this.saving = true;
        this.disaggregationService.saveGroup(this.editingGroup)
            .subscribe({
                next: () => {
                    this.saving = false;
                    this.toastr.success(
                        this.isNew ? 'Disaggregation group created successfully.' : 'Disaggregation group updated successfully.',
                        'Disaggregation Group'
                    );
                    if (this.modalRef) {
                        this.modalRef.close();
                    }
                    this.load();
                },
                error: (err) => {
                    this.saving = false;
                    this.errorService.handleError(err, 'Disaggregation Group', 'Save');
                }
            });
    }

    deleteGroup(group: DisaggregationGroup): void {
        if (!confirm(`Delete or deactivate disaggregation group "${group.name}"? If indicators are linked, it will be marked inactive.`)) {
            return;
        }

        this.disaggregationService.deleteGroup(group.disaggregationGroupId)
            .subscribe({
                next: (res: any) => {
                    if (res && res.message) {
                        this.toastr.info(res.message, 'Disaggregation Group');
                    } else {
                        this.toastr.success(`Disaggregation group "${group.name}" was deleted.`, 'Disaggregation Group');
                    }
                    this.load();
                },
                error: (err) => {
                    this.errorService.handleError(err, 'Disaggregation Group', 'Delete');
                }
            });
    }

    cancel(): void {
        if (this.modalRef) {
            this.modalRef.dismiss();
        }
    }
}

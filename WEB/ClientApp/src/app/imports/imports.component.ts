import { Component as NgComponent, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { PagingHeaders } from '../common/models/http.model';
import { ImportJob, ImportJobSearchOptions, ImportJobSearchResponse, BudgetImportRowReport, RecordsImportRowReport } from '../common/models/importjob.model';
import { ImportJobService } from '../common/services/import-job.service';
import { Project, ProjectSearchOptions } from '../common/models/project.model';
import { ProjectService } from '../common/services/project.service';
import { Questionnaire, QuestionnaireSearchOptions } from '../common/models/questionnaire.model';
import { QuestionnaireService } from '../common/services/questionnaire.service';
import { ErrorService } from '../common/services/error.service';
import { FadeThenShrink } from '../common/animations/fadethenshrink';

@NgComponent({
    selector: 'app-imports',
    templateUrl: './imports.component.html',
    animations: [FadeThenShrink],
    standalone: false
})
export class ImportsComponent implements OnInit {

    public importJobs: ImportJob[] = [];
    public searchOptions = new ImportJobSearchOptions();
    public headers = new PagingHeaders();
    public loading = false;

    public importType: string = "Budget";
    public projects: Project[] = [];
    public selectedProjectId: string;
    public questionnaires: Questionnaire[] = [];
    public selectedQuestionnaireId: string;
    public submitAfterCommit = false;

    public file: File;
    public uploading = false;

    public reportJob: ImportJob;
    public budgetRows: BudgetImportRowReport[];
    public recordsRows: RecordsImportRowReport[];

    constructor(
        private router: Router,
        private toastr: ToastrService,
        private errorService: ErrorService,
        private importJobService: ImportJobService,
        private projectService: ProjectService,
        private questionnaireService: QuestionnaireService
    ) {
    }

    ngOnInit(): void {
        this.load();
        this.loadProjects();
    }

    loadProjects(): void {
        const searchOptions = new ProjectSearchOptions();
        searchOptions.pageSize = 0;

        this.projectService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.projects = response.projects;
                    if (this.projects.length > 0) this.selectedProjectId = this.projects[0].projectId;
                },
                error: err => this.errorService.handleError(err, "Projects", "Load")
            });
    }

    loadQuestionnaires(): void {
        if (this.questionnaires.length > 0) return;

        const searchOptions = new QuestionnaireSearchOptions();
        searchOptions.pageSize = 0;

        this.questionnaireService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.questionnaires = response.questionnaires;
                    if (this.questionnaires.length > 0) this.selectedQuestionnaireId = this.questionnaires[0].questionnaireId;
                },
                error: err => this.errorService.handleError(err, "Questionnaires", "Load")
            });
    }

    importTypeChanged(): void {
        if (this.importType === "Records") this.loadQuestionnaires();
    }

    load(pageIndex = 0): void {
        this.loading = true;
        this.searchOptions.pageIndex = pageIndex;

        this.importJobService.search(this.searchOptions)
            .subscribe({
                next: (response: ImportJobSearchResponse) => {
                    this.importJobs = response.importJobs;
                    this.headers = response.headers;
                    this.loading = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Imports", "Load");
                    this.loading = false;
                }
            });
    }

    fileSelected(files: FileList): void {
        this.file = files.length > 0 ? files[0] : null;
    }

    upload(): void {
        if (!this.file) {
            this.toastr.error("Select a .xlsx or .csv file first", "Import");
            return;
        }

        if (this.importType === "Budget" && !this.selectedProjectId) {
            this.toastr.error("Select a project first", "Import");
            return;
        }

        if (this.importType === "Records" && !this.selectedQuestionnaireId) {
            this.toastr.error("Select a questionnaire first", "Import");
            return;
        }

        this.uploading = true;

        const importJob = new ImportJob();
        importJob.importType = this.importType;
        importJob.fileName = this.file.name;
        importJob.targetId = this.importType === "Budget" ? this.selectedProjectId : this.selectedQuestionnaireId;

        const reader = new FileReader();
        reader.onload = () => {
            importJob.fileContents = (reader.result as string).split(',')[1]; // strip data: prefix

            this.importJobService.upload(importJob)
                .subscribe({
                    next: saved => {
                        this.uploading = false;
                        this.file = null;
                        this.toastr.success(
                            `${saved.validRows} valid row(s), ${saved.errorRows} error(s) — review before committing`,
                            "Import Validated");
                        this.load();
                        this.showReport(saved);
                    },
                    error: err => {
                        this.errorService.handleError(err, "Import", "Upload");
                        this.uploading = false;
                    }
                });
        };
        reader.readAsDataURL(this.file);
    }

    showReport(importJob: ImportJob): void {
        this.reportJob = importJob;
        this.budgetRows = importJob.validationReport?.rows ?? [];
        this.recordsRows = importJob.recordsValidationReport?.rows ?? [];
    }

    commit(importJob: ImportJob, submitAfterCommit = false): void {
        const message = submitAfterCommit
            ? `Commit ${importJob.validRows} valid row(s) into live data and submit them for review? This cannot be undone from this page.`
            : `Commit ${importJob.validRows} valid row(s) into live data? This cannot be undone from this page.`;

        if (!confirm(message)) return;

        this.importJobService.commit(importJob.importJobId, submitAfterCommit)
            .subscribe({
                next: committed => {
                    this.toastr.success("The valid rows have been committed", "Import Committed");
                    this.load();
                },
                error: err => this.errorService.handleError(err, "Import", "Commit")
            });
    }

    delete(importJob: ImportJob): void {
        if (!confirm(`Delete the import "${importJob.fileName}"?`)) return;

        this.importJobService.delete(importJob.importJobId)
            .subscribe({
                next: () => this.load(),
                error: err => this.errorService.handleError(err, "Import", "Delete")
            });
    }

    statusClass(status: string): string {
        switch (status) {
            case "Committed": return "text-bg-success";
            case "Validated": return "text-bg-warning";
            case "Failed": return "text-bg-danger";
            default: return "text-bg-secondary";
        }
    }

    downloadTemplate(): void {
        if (this.importType === "Records") {
            if (!this.selectedQuestionnaireId) {
                this.toastr.error("Select a questionnaire first", "Import");
                return;
            }

            this.importJobService.template(this.selectedQuestionnaireId)
                .subscribe({
                    next: template => {
                        const blob = new Blob([template.contents], { type: "text/csv;charset=utf-8;" });
                        const link = document.createElement("a");
                        link.href = URL.createObjectURL(blob);
                        link.download = template.fileName;
                        link.click();
                    },
                    error: err => this.errorService.handleError(err, "Template", "Download")
                });
            return;
        }

        const header = "Project,Category,Description,Planned,Committed,Disbursed,Expended,Currency,PeriodStart,PeriodEnd";
        const sample = "\"Water for Senegal\",Staff,Field officers Q1,10000,8000,7000,6500,USD,2026-01-01,2026-03-31";
        const blob = new Blob([header + "\r\n" + sample + "\r\n"], { type: "text/csv;charset=utf-8;" });
        const link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = "budget-import-template.csv";
        link.click();
    }

}

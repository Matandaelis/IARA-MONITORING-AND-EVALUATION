import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { NgForm } from '@angular/forms';
import { ErrorService } from '../../common/services/error.service';
import { SetupService } from '../../common/services/setup.service';
import { EntityType } from '../../common/models/entitytype.model';
import { ToolsService } from '../../common/services/tools.service';
import { ImportService } from '../../common/services/import.service';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ImportError } from '../../common/models/import.model';
import { DownloadService } from '../../common/services/download.service';
import { Indicator } from '../../common/models/indicator.model';
import { Entity } from '../../common/models/entity.model';
import { AppDate } from '../../common/models/date.model';
import { KestraService, KestraSettings, KestraStatus, KestraConnectorFlow } from '../../common/services/kestra.service';

@Component({
    selector: 'tools',
    templateUrl: './tools.component.html',
    standalone: false
})
export class ToolsComponent implements OnInit {

    @ViewChild("csvModal") csvModal: any
    public csvErrors: ImportError[];

    public geoJson = {
        entityTypeId: undefined as string,
        entityType: undefined as EntityType,
        fileName: undefined as string,
        fileContents: undefined as string
    };

    public importCSVSettings = {
        fileName: undefined as string,
        fileContents: undefined as string
    };

    public exportCSVSettings = {
        indicatorIds: [] as string[],
        indicators: [] as Indicator[],
        entityIds: [] as string[],
        entities: [] as Entity[],
        dateIds: [] as string[],
        dates: [] as AppDate[]
    };

    // --- Kestra ETL connectors ---
    public kestraSettings: KestraSettings = new KestraSettings();
    public kestraStatus: KestraStatus;
    public kestraFlows: KestraConnectorFlow[] = [];
    public kestraTesting = false;
    public kestraSaving = false;

    constructor(
        private toastr: ToastrService,
        private errorService: ErrorService,
        private toolsService: ToolsService,
        private setupService: SetupService,
        private importService: ImportService,
        private modalService: NgbModal,
        private downloadService: DownloadService,
        private kestraService: KestraService
    ) { }

    ngOnInit() {
        this.loadKestra();
    }

    // --- Kestra ETL connectors ---

    loadKestra(): void {
        this.kestraService.getSettings()
            .subscribe({
                next: settings => this.kestraSettings = settings,
                error: err => this.errorService.handleError(err, "Kestra", "Load settings")
            });
    }

    saveKestraSettings(form: NgForm): void {
        if (form.invalid) {
            this.toastr.error("The form has not been completed correctly.");
            return;
        }

        this.kestraSaving = true;

        this.kestraService.saveSettings(this.kestraSettings)
            .subscribe({
                next: settings => {
                    this.kestraSettings = settings;
                    this.kestraSaving = false;
                    this.toastr.success("Kestra connection settings have been saved", "Kestra");
                },
                error: err => {
                    this.errorService.handleError(err, "Kestra", "Save settings");
                    this.kestraSaving = false;
                }
            });
    }

    testKestra(): void {
        this.kestraTesting = true;

        this.kestraService.status()
            .subscribe({
                next: status => {
                    this.kestraStatus = status;
                    this.kestraTesting = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Kestra", "Test connection");
                    this.kestraTesting = false;
                }
            });
    }

    loadKestraFlows(): void {
        this.kestraService.flows()
            .subscribe({
                next: flows => {
                    this.kestraFlows = flows;
                },
                error: err => this.errorService.handleError(err, "Kestra", "Load connectors")
            });
    }

    downloadFlow(flow: KestraConnectorFlow): void {
        const blob = new Blob([flow.template], { type: "application/x-yaml;charset=utf-8;" });
        const link = document.createElement("a");
        link.href = URL.createObjectURL(blob);
        link.download = `${flow.id}.yaml`;
        link.click();
    }

    copyToClipboard(text: string): void {
        navigator.clipboard.writeText(text).then(() => {
            this.toastr.success("Copied to clipboard");
        });
    }

    regenerateKestraKey(): void {
        if (!confirm("Generate a new external push API key? Existing Kestra flows will need the new key.")) return;

        this.kestraService.regenerateKey()
            .subscribe({
                next: result => {
                    this.kestraSettings.externalApiKey = result.externalApiKey;
                    this.toastr.success("A new external push API key has been generated", "Kestra");
                },
                error: err => this.errorService.handleError(err, "Kestra", "Regenerate key")
            });
    }

    uploadGeoJSON(form: NgForm) {

        if (form.invalid) {

            this.toastr.error("The form has not been completed correctly.");
            return;

        }

        if (!this.geoJson.fileName.endsWith(".json")) {
            this.toastr.error("The file should have a '.json' extension.");
            return;
        }

        this.toolsService.uploadGeoJson(this.geoJson.entityTypeId, this.geoJson.fileContents)
            .subscribe({
                next: () => {
                    this.toastr.success(`The GeoJSON ${this.geoJson.entityType.name} file has been uploaded`);
                    this.geoJson = { entityType: undefined, entityTypeId: undefined, fileContents: undefined, fileName: undefined };
                    form.resetForm();
                    form.reset();
                },
                error: err => this.errorService.handleError(err, "GeoJSON", "Upload")
            });

    }

    importCSV(form: NgForm) {

        if (form.invalid) {

            this.toastr.error("The form has not been completed correctly.");
            return;

        }

        if (!this.importCSVSettings.fileName.endsWith(".csv")) {
            this.toastr.error("The file should have a '.csv' extension.");
            return;
        }

        this.importService.importCSV(this.importCSVSettings.fileContents)
            .subscribe({
                next: () => {
                    this.toastr.success(`The CSV file has been imported`);
                    this.importCSVSettings = { fileContents: undefined, fileName: undefined };
                    form.resetForm();
                    form.reset();
                },
                error: err => {
                    if (Array.isArray(err.error) && err.error.length) {
                        this.csvErrors = err.error;
                        this.modalService.open(this.csvModal, { size: 'xl', centered: true, scrollable: true });
                    }
                    else {
                        this.errorService.handleError(err, "CSV", "Import")
                    }
                }
            })

    }

    exportCSV(form: NgForm) {

        if (form.invalid) {

            this.toastr.error("The form has not been completed correctly.");
            return;

        }

        this.downloadService.exportCSV(this.exportCSVSettings.indicatorIds, this.exportCSVSettings.entityIds, this.exportCSVSettings.dateIds)
            .subscribe({
                next: () => {
                },
                error: err => {
                    if (Array.isArray(err.error) && err.error.length) {
                        this.csvErrors = err.error;
                        this.modalService.open(this.csvModal, { size: 'xl', centered: true, scrollable: true });
                    }
                    else {
                        this.errorService.handleError(err, "CSV", "Export")
                    }
                }
            });

    }

    seedDemoProject() {

        this.setupService.seedDemoProject()
            .subscribe({
                next: () => {
                    this.toastr.success("The demo project has been seeded with deterministic entities, indicators, dates and two years of facility data. Use Entities, Indicators and Data to verify the aggregated &amp; calculated values.", "Demo Project Seeded");
                },
                error: err => this.errorService.handleError(err, "Demo Project", "Seed")
            });

    }

    openModal(content: TemplateRef<any>) {
        this.modalService.open(content, { ariaLabelledBy: 'modal-basic-title', size: 'lg' });
    }
}

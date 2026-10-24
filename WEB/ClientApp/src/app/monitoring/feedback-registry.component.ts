import { Component as NgComponent, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { BeneficiaryFeedback, FeedbackSeverity, FeedbackStatus, FeedbackSearchOptions, FeedbackStats } from '../common/models/feedback.model';
import { FeedbackService } from '../common/services/feedback.service';
import { MonitoringService } from '../common/services/monitoring.service';
import { ErrorService } from '../common/services/error.service';
import { PagingHeaders } from '../common/models/http.model';

@NgComponent({
    selector: 'app-feedback-registry',
    templateUrl: './feedback-registry.component.html',
    standalone: false
})
export class FeedbackRegistryComponent implements OnInit {

    projects: any[] = [];
    selectedProjectId = '';
    selectedStatus: FeedbackStatus = null;
    selectedSeverity: FeedbackSeverity = null;
    selectedChannel = '';
    searchTerm = '';

    items: BeneficiaryFeedback[] = [];
    headers: PagingHeaders = new PagingHeaders();
    loading = false;

    stats: FeedbackStats = {
        total: 0,
        received: 0,
        investigating: 0,
        resolved: 0,
        closed: 0,
        critical: 0,
        high: 0,
        medium: 0,
        low: 0
    };

    // Feedback creation & editing
    showForm = false;
    isEditing = false;
    currentFeedback: BeneficiaryFeedback = new BeneficiaryFeedback();

    readonly severityLevels = [
        { value: FeedbackSeverity.Low, label: 'Low' },
        { value: FeedbackSeverity.Medium, label: 'Medium' },
        { value: FeedbackSeverity.High, label: 'High' },
        { value: FeedbackSeverity.Critical, label: 'Critical' }
    ];

    readonly statusList = [
        { value: FeedbackStatus.Received, label: 'Received' },
        { value: FeedbackStatus.Investigating, label: 'Investigating' },
        { value: FeedbackStatus.Resolved, label: 'Resolved' },
        { value: FeedbackStatus.Closed, label: 'Closed' }
    ];

    readonly channelOptions = [
        'Community Meeting',
        'Suggestion Box',
        'SMS Hotline',
        'Help Desk / CFM Desk',
        'Field Visit',
        'Mobile App',
        'Direct Phone Call'
    ];

    readonly categoryOptions = [
        'General Inquiry',
        'Quality of Service',
        'Targeting / Eligibility',
        'Resource Allocation',
        'Staff Conduct / Safeguarding',
        'Environmental / Safety',
        'Appreciation / Positive Feedback'
    ];

    constructor(
        private feedbackService: FeedbackService,
        private monitoringService: MonitoringService,
        private errorService: ErrorService,
        private toastr: ToastrService
    ) {}

    ngOnInit(): void {
        this.loadProjects();
    }

    loadProjects(): void {
        this.monitoringService.getProjects().subscribe({
            next: projects => {
                this.projects = projects || [];
                if (this.projects.length > 0 && !this.selectedProjectId) {
                    this.selectedProjectId = this.projects[0].projectId;
                }
                this.loadFeedback(0);
                this.loadStats();
            },
            error: err => this.errorService.handleError(err, 'CFM', 'Load projects')
        });
    }

    loadStats(): void {
        this.feedbackService.getStats(this.selectedProjectId || undefined).subscribe({
            next: stats => this.stats = stats,
            error: () => {}
        });
    }

    loadFeedback(page = 0): void {
        this.loading = true;
        const options = new FeedbackSearchOptions();
        options.pageIndex = page;
        options.pageSize = 15;
        if (this.selectedProjectId) options.projectId = this.selectedProjectId;
        if (this.selectedStatus !== null && this.selectedStatus !== undefined) options.status = this.selectedStatus;
        if (this.selectedSeverity !== null && this.selectedSeverity !== undefined) options.severity = this.selectedSeverity;
        if (this.selectedChannel) options.channel = this.selectedChannel;
        if (this.searchTerm) options.q = this.searchTerm;

        this.feedbackService.search(options).subscribe({
            next: res => {
                this.items = res.items || [];
                this.headers = res.headers;
                this.loading = false;
            },
            error: err => {
                this.loading = false;
                this.errorService.handleError(err, 'CFM', 'Load feedback');
            }
        });
    }

    onFilterChange(): void {
        this.loadFeedback(0);
        this.loadStats();
    }

    openCreateForm(): void {
        this.isEditing = false;
        this.currentFeedback = new BeneficiaryFeedback();
        if (this.selectedProjectId) {
            this.currentFeedback.projectId = this.selectedProjectId;
        } else if (this.projects.length > 0) {
            this.currentFeedback.projectId = this.projects[0].projectId;
        }
        this.showForm = true;
    }

    editFeedback(item: BeneficiaryFeedback): void {
        this.isEditing = true;
        this.currentFeedback = { ...item };
        this.showForm = true;
    }

    saveFeedback(): void {
        if (!this.currentFeedback.projectId || !this.currentFeedback.description) {
            this.toastr.warning('Please select a project and provide a complaint/feedback description.', 'Required Fields');
            return;
        }

        this.feedbackService.save(this.currentFeedback).subscribe({
            next: saved => {
                this.toastr.success(
                    `Feedback ${saved.trackingCode || ''} saved successfully.${(saved.severity >= FeedbackSeverity.High ? ' Corrective Action Plan auto-triggered.' : '')}`,
                    'Feedback Saved'
                );
                this.showForm = false;
                this.loadFeedback(this.headers.pageIndex || 0);
                this.loadStats();
            },
            error: err => this.errorService.handleError(err, 'CFM', 'Save feedback')
        });
    }

    deleteFeedback(item: BeneficiaryFeedback): void {
        if (!confirm(`Are you sure you want to delete feedback ${item.trackingCode}?`)) return;
        this.feedbackService.delete(item.id).subscribe({
            next: () => {
                this.toastr.success('Feedback deleted successfully.', 'CFM');
                this.loadFeedback(this.headers.pageIndex || 0);
                this.loadStats();
            },
            error: err => this.errorService.handleError(err, 'CFM', 'Delete feedback')
        });
    }

    getSeverityBadgeClass(severity: FeedbackSeverity): string {
        switch (severity) {
            case FeedbackSeverity.Critical: return 'bg-danger text-white';
            case FeedbackSeverity.High: return 'bg-warning text-dark';
            case FeedbackSeverity.Medium: return 'bg-info text-dark';
            default: return 'bg-secondary text-white';
        }
    }

    getStatusBadgeClass(status: FeedbackStatus): string {
        switch (status) {
            case FeedbackStatus.Received: return 'bg-primary text-white';
            case FeedbackStatus.Investigating: return 'bg-warning text-dark';
            case FeedbackStatus.Resolved: return 'bg-success text-white';
            case FeedbackStatus.Closed: return 'bg-secondary text-white';
            default: return 'bg-light text-dark';
        }
    }
}

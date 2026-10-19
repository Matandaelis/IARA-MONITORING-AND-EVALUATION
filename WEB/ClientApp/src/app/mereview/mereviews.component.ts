import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrService } from 'ngx-toastr';
import {
    MeReview,
    MeReviewSearchOptions,
    ReviewDomainInfo,
    ReviewDashboardStats
} from '../common/models/mereview.model';
import { MeReviewService } from '../common/services/mereview.service';
import { MeReviewModalComponent } from './mereview.modal.component';

@Component({
    selector: 'app-mereviews',
    templateUrl: './mereviews.component.html',
    standalone: false
})
export class MeReviewsComponent implements OnInit {
    public reviews: MeReview[] = [];
    public domains: ReviewDomainInfo[] = [];
    public stats: ReviewDashboardStats = new ReviewDashboardStats();
    public searchOptions = new MeReviewSearchOptions();
    public totalRecords = 0;
    public totalPages = 0;
    public loading = false;

    constructor(
        private meReviewService: MeReviewService,
        private modalService: NgbModal,
        private toastr: ToastrService,
        private router: Router
    ) {
        this.searchOptions.pageSize = 15;
        this.searchOptions.pageIndex = 0;
    }

    ngOnInit(): void {
        this.loadDomains();
        this.loadStats();
        this.loadReviews();
    }

    loadDomains(): void {
        this.meReviewService.getDomains().subscribe({
            next: (data) => this.domains = data,
            error: (err) => console.error('Error loading domains', err)
        });
    }

    loadStats(): void {
        this.meReviewService.getStats().subscribe({
            next: (data) => this.stats = data,
            error: (err) => console.error('Error loading stats', err)
        });
    }

    loadReviews(): void {
        this.loading = true;
        this.searchOptions.includeParents = true;
        this.searchOptions.includeChildren = true;

        this.meReviewService.search(this.searchOptions).subscribe({
            next: (res) => {
                this.reviews = res.reviews || [];
                if (res.headers) {
                    this.totalRecords = res.headers.totalRecords;
                    this.totalPages = res.headers.totalPages;
                }
                this.loading = false;
            },
            error: (err) => {
                this.toastr.error('Failed to load reviews');
                this.loading = false;
            }
        });
    }

    onSearch(): void {
        this.searchOptions.pageIndex = 0;
        this.loadReviews();
    }

    onFilterDomain(domainKey?: string): void {
        this.searchOptions.domain = domainKey || undefined;
        this.searchOptions.pageIndex = 0;
        this.loadReviews();
    }

    onFilterStatus(status?: string): void {
        this.searchOptions.status = status || undefined;
        this.searchOptions.pageIndex = 0;
        this.loadReviews();
    }

    onPageChange(page: number): void {
        this.searchOptions.pageIndex = page;
        this.loadReviews();
    }

    openNewReviewModal(): void {
        const modalRef = this.modalService.open(MeReviewModalComponent, { size: 'lg', backdrop: 'static' });
        modalRef.result.then((newReview: MeReview) => {
            if (newReview && newReview.meReviewId) {
                this.toastr.success(`Review completed with score: ${newReview.overallScore}/100`);
                this.loadStats();
                this.loadReviews();
                this.router.navigate(['/mereviews', newReview.meReviewId]);
            }
        }, () => {});
    }

    viewReview(review: MeReview): void {
        this.router.navigate(['/mereviews', review.meReviewId]);
    }

    deleteReview(review: MeReview, event: Event): void {
        event.stopPropagation();
        if (confirm(`Are you sure you want to delete review "${review.title}"?`)) {
            this.meReviewService.delete(review.meReviewId).subscribe({
                next: () => {
                    this.toastr.success('Review deleted');
                    this.loadStats();
                    this.loadReviews();
                },
                error: () => this.toastr.error('Failed to delete review')
            });
        }
    }

    getScoreBadgeClass(score: number): string {
        if (score >= 80) return 'bg-success text-white';
        if (score >= 50) return 'bg-warning text-dark';
        return 'bg-danger text-white';
    }

    getStatusBadgeClass(status: string): string {
        switch (status?.toLowerCase()) {
            case 'approved': return 'badge bg-success';
            case 'requiresrevision': return 'badge bg-warning text-dark';
            case 'draft': return 'badge bg-secondary';
            default: return 'badge bg-info text-dark';
        }
    }
}

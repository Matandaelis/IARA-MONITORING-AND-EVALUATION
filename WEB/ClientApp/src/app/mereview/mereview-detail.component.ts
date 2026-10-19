import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import {
    MeReview,
    MeReviewFinding,
    MeReviewRecommendation
} from '../common/models/mereview.model';
import { MeReviewService } from '../common/services/mereview.service';

@Component({
    selector: 'app-mereview-detail',
    templateUrl: './mereview-detail.component.html',
    standalone: false
})
export class MeReviewDetailComponent implements OnInit {
    public review: MeReview;
    public loading = true;
    public severityFilter = 'All';

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private meReviewService: MeReviewService,
        private toastr: ToastrService
    ) {}

    ngOnInit(): void {
        this.route.paramMap.subscribe(params => {
            const id = params.get('meReviewId');
            if (id) {
                this.loadReview(id);
            }
        });
    }

    loadReview(id: string): void {
        this.loading = true;
        this.meReviewService.get(id).subscribe({
            next: (data) => {
                this.review = data;
                this.loading = false;
            },
            error: (err) => {
                this.toastr.error('Failed to load review scorecard');
                this.loading = false;
                this.router.navigate(['/mereviews']);
            }
        });
    }

    get filteredFindings(): MeReviewFinding[] {
        if (!this.review || !this.review.findings) return [];
        if (this.severityFilter === 'All') return this.review.findings;
        return this.review.findings.filter(f => f.severity.toLowerCase() === this.severityFilter.toLowerCase());
    }

    setSeverityFilter(severity: string): void {
        this.severityFilter = severity;
    }

    toggleRecommendation(rec: MeReviewRecommendation): void {
        this.meReviewService.toggleRecommendation(rec.meReviewRecommendationId).subscribe({
            next: (updated) => {
                rec.isImplemented = updated.isImplemented;
                this.toastr.success(rec.isImplemented ? 'Marked as implemented' : 'Marked as open');
            },
            error: () => this.toastr.error('Failed to update recommendation status')
        });
    }

    deleteReview(): void {
        if (confirm(`Delete review "${this.review?.title}"?`)) {
            this.meReviewService.delete(this.review.meReviewId).subscribe({
                next: () => {
                    this.toastr.success('Review deleted');
                    this.router.navigate(['/mereviews']);
                },
                error: () => this.toastr.error('Failed to delete review')
            });
        }
    }

    printScorecard(): void {
        window.print();
    }

    getScoreBadgeClass(score: number): string {
        if (score >= 80) return 'bg-success text-white';
        if (score >= 50) return 'bg-warning text-dark';
        return 'bg-danger text-white';
    }

    getSeverityBadgeClass(severity: string): string {
        switch (severity?.toLowerCase()) {
            case 'critical': return 'badge bg-danger text-white';
            case 'high': return 'badge bg-warning text-dark';
            case 'medium': return 'badge bg-info text-dark';
            case 'low': return 'badge bg-secondary text-white';
            case 'info': return 'badge bg-light text-secondary border';
            default: return 'badge bg-secondary';
        }
    }

    getPriorityBadgeClass(priority: string): string {
        switch (priority?.toLowerCase()) {
            case 'immediate': return 'badge bg-danger text-white';
            case 'shortterm': return 'badge bg-warning text-dark';
            case 'mediumterm': return 'badge bg-info text-dark';
            default: return 'badge bg-secondary text-white';
        }
    }
}

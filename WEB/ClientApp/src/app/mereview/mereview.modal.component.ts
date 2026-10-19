import { Component, OnInit } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import { ToastrService } from 'ngx-toastr';
import {
    EvaluateReviewRequest,
    ReviewDomainInfo,
    SamplingPlanRequest,
    MeReview
} from '../common/models/mereview.model';
import { MeReviewService } from '../common/services/mereview.service';
import { TheoryOfChangeService } from '../common/services/theoryofchange.service';
import { TheoryOfChangeSearchOptions } from '../common/models/theoryofchange.model';
import { LogFrameService } from '../common/services/logframe.service';
import { LogFrameSearchOptions } from '../common/models/logframe.model';
import { QuestionnaireService } from '../common/services/questionnaire.service';
import { QuestionnaireSearchOptions } from '../common/models/questionnaire.model';

@Component({
    selector: 'app-mereview-modal',
    templateUrl: './mereview.modal.component.html',
    standalone: false
})
export class MeReviewModalComponent implements OnInit {
    public domains: ReviewDomainInfo[] = [];
    public selectedDomainKey = 'theory-of-change';
    public inputMode: 'entity' | 'pdf' | 'text' | 'sampling' = 'entity';

    public selectedEntityId: string = '';
    public reviewTitle: string = '';
    public narrativeText: string = '';
    public selectedFile: File = null;

    // Entity lists
    public theoriesOfChange: any[] = [];
    public logFrames: any[] = [];
    public questionnaires: any[] = [];

    // Sampling Plan fields
    public samplingPlan = new SamplingPlanRequest();

    public loading = false;
    public evaluating = false;

    constructor(
        public activeModal: NgbActiveModal,
        private meReviewService: MeReviewService,
        private tocService: TheoryOfChangeService,
        private logframeService: LogFrameService,
        private questionnaireService: QuestionnaireService,
        private toastr: ToastrService
    ) {}

    ngOnInit(): void {
        this.loadDomains();
        this.loadEntities();
    }

    loadDomains(): void {
        this.meReviewService.getDomains().subscribe({
            next: (data) => {
                this.domains = data;
                this.onDomainChange();
            },
            error: () => this.toastr.error('Failed to load review domains')
        });
    }

    loadEntities(): void {
        const opt = new TheoryOfChangeSearchOptions();
        opt.pageSize = 50;
        this.tocService.search(opt).subscribe({
            next: (res) => this.theoriesOfChange = res.theoriesOfChange || []
        });

        const lfOpt = new LogFrameSearchOptions();
        lfOpt.pageSize = 50;
        this.logframeService.search(lfOpt).subscribe({
            next: (res) => this.logFrames = res.logFrames || []
        });

        const qOpt = new QuestionnaireSearchOptions();
        qOpt.pageSize = 50;
        this.questionnaireService.search(qOpt).subscribe({
            next: (res) => this.questionnaires = res.questionnaires || []
        });
    }

    onDomainChange(): void {
        const domain = this.domains.find(d => d.key === this.selectedDomainKey);
        if (this.selectedDomainKey === 'sampling-plan') {
            this.inputMode = 'sampling';
        } else if (domain && domain.supportsEntityEvaluation) {
            this.inputMode = 'entity';
        } else {
            this.inputMode = 'pdf';
        }

        this.selectedEntityId = '';
    }

    onFileSelected(event: any): void {
        const fileList: FileList = event.target.files;
        if (fileList && fileList.length > 0) {
            this.selectedFile = fileList[0];
            if (!this.reviewTitle) {
                this.reviewTitle = this.selectedFile.name.replace(/\.[^/.]+$/, '');
            }
        }
    }

    runEvaluation(): void {
        if (this.inputMode === 'pdf') {
            if (!this.selectedFile) {
                this.toastr.warning('Please select a PDF file to evaluate');
                return;
            }
            this.evaluating = true;
            this.meReviewService.evaluatePdf(this.selectedFile, this.selectedDomainKey, this.reviewTitle || this.selectedFile.name)
                .subscribe({
                    next: (res) => {
                        this.evaluating = false;
                        this.activeModal.close(res);
                    },
                    error: (err) => {
                        this.evaluating = false;
                        this.toastr.error('PDF evaluation failed');
                    }
                });
            return;
        }

        const req = new EvaluateReviewRequest();
        req.domain = this.selectedDomainKey;
        req.title = this.reviewTitle;

        if (this.inputMode === 'entity') {
            if (this.selectedDomainKey !== 'data-quality' && !this.selectedEntityId) {
                this.toastr.warning('Please select an entity to evaluate');
                return;
            }
            if (this.selectedEntityId) {
                req.targetEntityId = this.selectedEntityId;
            }
        } else if (this.inputMode === 'sampling') {
            req.samplingPlan = this.samplingPlan;
            req.samplingPlan.title = this.reviewTitle || 'Field Study Sample Size Plan';
        } else if (this.inputMode === 'text') {
            if (!this.narrativeText) {
                this.toastr.warning('Please paste the narrative text to evaluate');
                return;
            }
            req.narrativeText = this.narrativeText;
        }

        this.evaluating = true;
        this.meReviewService.evaluate(req).subscribe({
            next: (res) => {
                this.evaluating = false;
                this.activeModal.close(res);
            },
            error: (err) => {
                this.evaluating = false;
                this.toastr.error('Review evaluation failed');
            }
        });
    }

    get currentDomain(): ReviewDomainInfo {
        return this.domains.find(d => d.key === this.selectedDomainKey);
    }
}

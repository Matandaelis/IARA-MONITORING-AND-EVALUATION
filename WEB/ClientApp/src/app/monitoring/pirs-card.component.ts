import { Component as NgComponent, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Indicator } from '../common/models/indicator.model';
import { IndicatorTarget } from '../common/models/indicatortarget.model';
import { IndicatorService } from '../common/services/indicator.service';
import { IndicatorTargetService } from '../common/services/indicatortarget.service';
import { monitoringTypeLabel } from '../common/models/monitoring.model';

@NgComponent({
    selector: 'app-pirs-card',
    templateUrl: './pirs-card.component.html',
    standalone: false
})
export class PirsCardComponent implements OnInit {

    @Input() indicatorId: string;
    @Input() indicator: Indicator;
    @Input() targets: IndicatorTarget[] = [];
    @Input() isModal = false;

    @Output() close = new EventEmitter<void>();

    loading = false;

    constructor(
        private indicatorService: IndicatorService,
        private targetService: IndicatorTargetService
    ) {}

    ngOnInit(): void {
        if (this.indicatorId && !this.indicator) {
            this.loadIndicator(this.indicatorId);
        }
    }

    loadIndicator(id: string): void {
        this.loading = true;
        this.indicatorService.get(id).subscribe({
            next: ind => {
                this.indicator = ind;
                this.loading = false;
                this.loadTargets(id);
            },
            error: () => {
                this.loading = false;
            }
        });
    }

    loadTargets(id: string): void {
        this.targetService.getTargets(id).subscribe({
            next: t => this.targets = t || [],
            error: () => {}
        });
    }

    getMonitoringType(val?: number): string {
        return val !== null && val !== undefined ? monitoringTypeLabel(val) : 'General M&E';
    }

    printCard(): void {
        window.print();
    }
}

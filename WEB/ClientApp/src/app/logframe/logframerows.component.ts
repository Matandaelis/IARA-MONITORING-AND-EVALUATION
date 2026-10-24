import { Component, EventEmitter, Input, Output, ViewEncapsulation } from "@angular/core";
import { LogFrameRow, LogFrameLiveIndicatorMetric } from "../common/models/logframerow.model";
import { LogFrameRowRollup, LogFrameIndicatorRollup } from "../common/models/logframe.model";

@Component({
    selector: 'app-logframerows',
    templateUrl: './logframerows.component.html',
    styleUrls: ['./logframerows.component.css'],
    encapsulation: ViewEncapsulation.None,
    standalone: false
})
export class LogFrameRowsComponent {

    @Input() rows: LogFrameRow[] = [];
    @Input() rowRollupMap: { [rowId: string]: LogFrameRowRollup } = {};

    @Output() rowClick = new EventEmitter<LogFrameRow>();

    public rowClicked(row?: LogFrameRow): void {
        this.rowClick.emit(row);
    }

    public getRowRollup(row: LogFrameRow): LogFrameRowRollup | undefined {
        if (!row || !this.rowRollupMap) return undefined;
        return this.rowRollupMap[row.logFrameRowId] || this.rowRollupMap[row.logFrameRowId?.toLowerCase()];
    }

    public getIndicatorRollup(row: LogFrameRow, indicatorId: string): LogFrameIndicatorRollup | undefined {
        const rowRollup = this.getRowRollup(row);
        if (!rowRollup || !rowRollup.indicators) return undefined;
        return rowRollup.indicators.find(i => i.indicatorId?.toLowerCase() === indicatorId?.toLowerCase());
    }

    public getLiveMetric(row: LogFrameRow, indicatorId: string): LogFrameLiveIndicatorMetric | undefined {
        if (!row || !row.liveIndicatorMetrics) return undefined;
        return row.liveIndicatorMetrics.find(m => m.indicatorId?.toLowerCase() === indicatorId?.toLowerCase());
    }

    public getStatusBadgeClass(status?: string): string {
        switch (status?.toLowerCase()) {
            case 'ontrack':
            case 'success':
                return 'bg-success text-white';
            case 'underperforming':
            case 'danger':
                return 'bg-danger text-white';
            case 'exceeding':
            case 'primary':
                return 'bg-primary text-white';
            case 'warning':
                return 'bg-warning text-dark';
            default:
                return 'bg-secondary text-white';
        }
    }

    public getStatusLabel(status?: string): string {
        switch (status?.toLowerCase()) {
            case 'ontrack':
            case 'success':
                return 'On Track';
            case 'underperforming':
            case 'danger':
                return 'Under';
            case 'exceeding':
            case 'primary':
                return 'Exceed';
            case 'warning':
                return 'Attention';
            default:
                return '';
        }
    }
}

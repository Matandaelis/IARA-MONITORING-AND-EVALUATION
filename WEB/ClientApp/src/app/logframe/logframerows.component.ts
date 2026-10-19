import { Component, EventEmitter, Input, Output, ViewEncapsulation } from "@angular/core";
import { LogFrameRow } from "../common/models/logframerow.model";
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

    public getStatusBadgeClass(status?: string): string {
        switch (status?.toLowerCase()) {
            case 'ontrack': return 'bg-success text-white';
            case 'underperforming': return 'bg-danger text-white';
            case 'exceeding': return 'bg-primary text-white';
            default: return 'bg-secondary text-white';
        }
    }

    public getStatusLabel(status?: string): string {
        switch (status?.toLowerCase()) {
            case 'ontrack': return 'On Track';
            case 'underperforming': return 'Under';
            case 'exceeding': return 'Exceed';
            default: return '';
        }
    }
}

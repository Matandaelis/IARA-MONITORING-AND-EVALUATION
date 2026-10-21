import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LogFrameTreeService } from './logframe-tree.service';
import { TreeNode } from 'primeng/api';

@Component({
    selector: 'app-logframe-tree',
    templateUrl: './logframe-tree.component.html',
    standalone: false
})
export class LogFrameTreeComponent implements OnInit {
    readonly store = inject(LogFrameTreeService);

    logFrameId: string | null = null;

    constructor(private readonly route: ActivatedRoute, private readonly router: Router) {}

    ngOnInit(): void {
        this.logFrameId = this.route.snapshot.paramMap.get('logFrameId');
        if (this.logFrameId) {
            this.store.load(this.logFrameId);
        }
    }

    goBack(): void {
        this.router.navigate(['/']);
    }
}

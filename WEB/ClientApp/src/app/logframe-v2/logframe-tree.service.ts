import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { TreeNode } from 'primeng/api';

export interface LogFrameNodeDto {
    id: string;
    parentId: string | null;
    code: string;
    title: string;
    description?: string;
    level: string;
    depth: number;
    sortOrder: number;
    indicatorCount: number;
    achievement?: number;
    status?: string;
    rowVersion: string;
}

export interface LogFrameTreeDto {
    logFrameId: string;
    name: string;
    nodes: LogFrameNodeDto[];
}

@Injectable({ providedIn: 'root' })
export class LogFrameTreeService {
    private readonly http = inject(HttpClient);

    private readonly _tree = signal<LogFrameTreeDto | null>(null);
    private readonly _loading = signal(false);
    private readonly _error = signal<string | null>(null);

    readonly tree = this._tree.asReadonly();
    readonly loading = this._loading.asReadonly();
    readonly error = this._error.asReadonly();
    readonly nodes = computed(() => this._tree()?.nodes ?? []);

    readonly roots = computed<TreeNode<LogFrameNodeDto>[]>(() => buildTree(this.nodes()));

    load(logFrameId: string): void {
        this._loading.set(true);
        this._error.set(null);

        this.http.get<LogFrameTreeDto>(`${environment.baseApiUrl}logframes/${logFrameId}/tree`)
            .subscribe({
                next: tree => {
                    this._tree.set(tree);
                    this._loading.set(false);
                },
                error: err => {
                    this._error.set(err?.error?.detail ?? 'Unable to load the logframe.');
                    this._loading.set(false);
                }
            });
    }
}

function buildTree(nodes: LogFrameNodeDto[]): TreeNode<LogFrameNodeDto>[] {
    const map = new Map<string, TreeNode<LogFrameNodeDto>>();

    for (const node of nodes) {
        map.set(node.id, {
            data: node,
            children: [],
            expanded: node.depth < 2,
            leaf: true
        });
    }

    const roots: TreeNode<LogFrameNodeDto>[] = [];

    for (const node of nodes) {
        const treeNode = map.get(node.id)!;
        if (node.parentId && map.has(node.parentId)) {
            const parent = map.get(node.parentId)!;
            parent.children!.push(treeNode);
            parent.leaf = false;
        } else {
            roots.push(treeNode);
        }
    }

    const sort = (list: TreeNode<LogFrameNodeDto>[]) => {
        list.sort((a, b) => (a.data!.sortOrder - b.data!.sortOrder));
        list.forEach(n => sort(n.children ?? []));
    };

    sort(roots);
    return roots;
}

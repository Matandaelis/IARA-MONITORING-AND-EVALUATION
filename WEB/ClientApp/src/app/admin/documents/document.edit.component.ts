import { Component as NgComponent, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { NgForm } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { ConfirmModalComponent, ConfirmModalOptions } from '../../common/components/confirm.component';
import { Document } from '../../common/models/document.model';
import { Tag, TagSearchOptions } from '../../common/models/tag.model';
import { BreadcrumbService } from '../../common/services/breadcrumb.service';
import { ErrorService } from '../../common/services/error.service';
import { DocumentService } from '../../common/services/document.service';
import { TagService } from '../../common/services/tag.service';
import { DownloadService } from '../../common/services/download.service';
import { AuthService } from '../../common/services/auth.service';

@NgComponent({
    selector: 'document-edit',
    templateUrl: './document.edit.component.html',
    standalone: false
})
export class DocumentEditComponent implements OnInit {

    public document: Document = new Document();
    public isNew = true;
    public tags: Tag[] = [];
    public newTagName = "";

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private toastr: ToastrService,
        private breadcrumbService: BreadcrumbService,
        private modalService: NgbModal,
        private documentService: DocumentService,
        private tagService: TagService,
        private downloadService: DownloadService,
        private errorService: ErrorService,
        private authService: AuthService
    ) {
    }

    ngOnInit(): void {

        this.route.params.subscribe(params => {

            const documentId = params["documentId"];
            this.document.itemId = this.route.snapshot.parent.params.itemId;
            this.isNew = documentId === "add";

            if (!this.isNew) {

                this.document.documentId = documentId;
                this.loadDocument();

            }

        });

        this.loadTags();

    }

    private loadTags(): void {

        const searchOptions = new TagSearchOptions();
        searchOptions.pageSize = 0;

        this.tagService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.tags = response.tags;
                },
                error: err => {
                    this.errorService.handleError(err, "Tags", "Load");
                }
            });

    }

    hasTag(tag: Tag): boolean {
        return (this.document.tags || []).some(o => o.tagId === tag.tagId);
    }

    toggleTag(tag: Tag, checked: boolean): void {
        if (!this.document.tags) this.document.tags = [];
        if (checked) {
            if (!this.hasTag(tag)) this.document.tags.push(tag);
        } else {
            this.document.tags = this.document.tags.filter(o => o.tagId !== tag.tagId);
        }
    }

    createTag(): void {
        const name = (this.newTagName || "").trim();
        if (!name) return;

        const tag = new Tag();
        tag.name = name;

        this.tagService.save(tag)
            .subscribe({
                next: created => {
                    this.tags.push(created);
                    this.newTagName = "";
                    if (!this.document.tags) this.document.tags = [];
                    this.document.tags.push(created);
                    this.toastr.success("The tag has been created", "Create Tag");
                },
                error: err => {
                    this.errorService.handleError(err, "Tag", "Create");
                }
            });
    }

    get canManageTags(): boolean {
        return this.authService.isInRole("Manager");
    }

    private loadDocument(): void {

        this.documentService.get(this.document.documentId)
            .subscribe({
                next: document => {
                    this.document = document;
                    this.changeBreadcrumb();
                },
                error: err => {
                    this.errorService.handleError(err, "Document", "Load");
                    if (err instanceof HttpErrorResponse && err.status === 404)
                        this.router.navigate(["../../"], { relativeTo: this.route });
                }
            });

    }

    save(form: NgForm): void {

        if (form.invalid) {

            this.toastr.error("The form has not been completed correctly.", "Form Error");
            return;

        }

        this.documentService.save(this.document)
            .subscribe({
                next: document => {
                    this.toastr.success("The document has been saved", "Save Document");
                    if (this.isNew) this.router.navigate(["../", document.documentId], { relativeTo: this.route });
                },
                error: err => {
                    this.errorService.handleError(err, "Document", "Save");
                }
            });

    }

    delete(): void {

        let modalRef = this.modalService.open(ConfirmModalComponent, { centered: true });
        (modalRef.componentInstance as ConfirmModalComponent).options = { title: "Delete Document", text: "Are you sure you want to delete this document?", deleteStyle: true, ok: "Delete" } as ConfirmModalOptions;
        modalRef.result.then(
            () => {

                this.documentService.delete(this.document.documentId)
                    .subscribe({
                        next: () => {
                            this.toastr.success("The document has been deleted", "Delete Document");
                            this.router.navigate(["../../"], { relativeTo: this.route });
                        },
                        error: err => {
                            this.errorService.handleError(err, "Document", "Delete");
                        }
                    });

            }, () => { });
    }

    changeBreadcrumb(): void {
        this.breadcrumbService.changeBreadcrumb(this.route.snapshot, this.document.fileName !== undefined ? this.document.fileName.substring(0, 25) : "(new document)");
    }

    download(fileId: string): void {
        this.downloadService.downloadDocument(fileId).subscribe();
    }

}

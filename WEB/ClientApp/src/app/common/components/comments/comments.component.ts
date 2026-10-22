import { Component, Input, OnChanges, OnInit } from '@angular/core';
import { Comment, CommentSearchOptions } from '../../models/comment.model';
import { CommentService } from '../../services/comment.service';
import { UserService } from '../../services/user.service';
import { UserSearchOptions } from '../../models/user.model';
import { AuthService } from '../../services/auth.service';
import { SignalRService } from '../../services/signal-r.service';
import { ErrorService } from '../../services/error.service';

@Component({
    selector: 'app-comments',
    templateUrl: './comments.component.html',
    styleUrls: ['./comments.component.css'],
    standalone: false
})
export class CommentsComponent implements OnInit, OnChanges {

    @Input() subjectType: string;
    @Input() subjectId: string;
    // client route used for mention notifications (e.g. "/questionnaire/abc123/responses/def456")
    @Input() notificationLink: string;

    public comments: Comment[] = [];
    public loading = false;
    public saving = false;
    public body = "";

    // pending @mentions: userId -> "@Full Name"
    private pendingMentions = new Map<string, string>();
    public mentionSearch = "";
    public mentionResults: { id: string; fullName: string; email: string }[] = [];
    public mentionSearching = false;

    constructor(
        private commentService: CommentService,
        private userService: UserService,
        private authService: AuthService,
        private signalRService: SignalRService,
        private errorService: ErrorService
    ) {
    }

    ngOnInit(): void {
        this.load();

        // live comments: another user commenting on the same record appears instantly
        this.signalRService.commentAdded$.subscribe(event => {
            if (event.subjectType !== this.subjectType || event.subjectId !== this.subjectId) return;
            if (this.comments.some(o => o.commentId === event.comment.commentId)) return;
            this.comments.push(event.comment);
        });

        this.signalRService.watchComments(this.subjectType, this.subjectId);
    }

    ngOnChanges(): void {
        this.signalRService.watchComments(this.subjectType, this.subjectId);
        this.load();
    }

    get currentUserId(): string {
        return this.authService.profile?.userId;
    }

    isAdmin(): boolean {
        return this.authService.isInRole("Administrator");
    }

    load(): void {
        if (!this.subjectType || !this.subjectId) return;

        this.loading = true;

        const searchOptions = new CommentSearchOptions();
        searchOptions.subjectType = this.subjectType;
        searchOptions.subjectId = this.subjectId;
        searchOptions.includeParents = true;
        searchOptions.includeChildren = true;
        searchOptions.pageSize = 0;

        this.commentService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.comments = response.comments;
                    this.loading = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Comments", "Load");
                    this.loading = false;
                }
            });
    }

    save(): void {
        if (!this.body || !this.body.trim()) return;

        this.saving = true;

        const comment = new Comment();
        comment.subjectType = this.subjectType;
        comment.subjectId = this.subjectId;
        comment.body = this.body.trim();
        comment.notificationLink = this.notificationLink;
        // only keep mentions actually referenced in the final text
        comment.mentionUserIds = Array.from(this.pendingMentions.entries())
            .filter(([_, token]) => this.body.includes(token))
            .map(([userId, _]) => userId);

        this.commentService.save(comment)
            .subscribe({
                next: saved => {
                    this.comments.push(saved);
                    this.body = "";
                    this.pendingMentions.clear();
                    this.saving = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Comment", "Save");
                    this.saving = false;
                }
            });
    }

    searchMentions(): void {
        if (!this.mentionSearch || this.mentionSearch.trim().length < 2) {
            this.mentionResults = [];
            return;
        }

        this.mentionSearching = true;

        const searchOptions = new UserSearchOptions();
        searchOptions.q = this.mentionSearch.trim();
        searchOptions.pageSize = 5;

        this.userService.search(searchOptions)
            .subscribe({
                next: response => {
                    this.mentionResults = response.users
                        .filter(u => !u.disabled)
                        .map(u => ({ id: u.id, fullName: u.fullName || u.email, email: u.email }));
                    this.mentionSearching = false;
                },
                error: err => {
                    this.errorService.handleError(err, "Users", "Search");
                    this.mentionSearching = false;
                }
            });
    }

    addMention(user: { id: string; fullName: string; email: string }): void {
        const token = `@${user.fullName}`;
        this.pendingMentions.set(user.id, token);
        this.body = (this.body || "") + (this.body && !this.body.endsWith(" ") ? " " : "") + token + " ";
        this.mentionSearch = "";
        this.mentionResults = [];
    }

    react(comment: Comment, reaction: string): void {
        this.commentService.react(comment.commentId, reaction)
            .subscribe({
                next: updated => {
                    const index = this.comments.findIndex(o => o.commentId === comment.commentId);
                    if (index >= 0) this.comments[index] = updated;
                },
                error: err => {
                    this.errorService.handleError(err, "Comment", "React");
                }
            });
    }

    toggleReaction(comment: Comment, reaction: string): void {
        const existing = comment.reactionOf(this.currentUserId);
        if (existing && existing.reaction === reaction) {
            this.react(comment, null); // remove
        } else {
            this.react(comment, reaction);
        }
    }

    countReactions(comment: Comment, reaction: string): number {
        return (comment.reactions || []).filter(r => r.reaction === reaction).length;
    }

    canDelete(comment: Comment): boolean {
        return comment.createdById === this.currentUserId || this.isAdmin();
    }

    delete(comment: Comment): void {
        this.commentService.delete(comment.commentId)
            .subscribe({
                next: () => {
                    this.comments = this.comments.filter(o => o.commentId !== comment.commentId);
                },
                error: err => {
                    this.errorService.handleError(err, "Comment", "Delete");
                }
            });
    }

    initials(name: string): string {
        if (!name) return "?";
        const parts = name.trim().split(/\s+/);
        return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : "")).toUpperCase();
    }

}

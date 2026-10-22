import { SearchOptions, PagingHeaders } from './http.model';
import { User } from './user.model';

export class CommentReaction {
    commentReactionId: string;
    commentId: string;
    userId: string;
    reaction: string;
    user: User;

    constructor() {
        this.commentReactionId = "00000000-0000-0000-0000-000000000000";
    }
}

export class Comment {
    commentId: string;
    subjectType: string;
    subjectId: string;
    body: string;
    createdOnUtc: Date;
    createdById: string;
    modifiedOnUtc: Date;
    modifiedById: string;
    createdBy: User;
    modifiedBy: User;
    reactions: CommentReaction[] = [];

    // request-only fields
    mentionUserIds: string[] = [];
    notificationLink: string;

    constructor() {
        this.commentId = "00000000-0000-0000-0000-000000000000";
    }

    reactionOf(userId: string): CommentReaction {
        return (this.reactions || []).find(r => r.userId === userId);
    }
}

export class CommentSearchOptions extends SearchOptions {
    subjectType: string;
    subjectId: string;
    createdById: string;
}

export class CommentSearchResponse {
    comments: Comment[] = [];
    headers: PagingHeaders;
}

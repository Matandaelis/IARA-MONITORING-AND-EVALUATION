import { SearchOptions, PagingHeaders } from './http.model';
import { User } from './user.model';

export class Tag {
    tagId: string;
    name: string;
    description: string;
    createdOnUtc: Date;
    createdById: string;
    createdBy: User;
    documentCount: number;

    constructor() {
        this.tagId = "00000000-0000-0000-0000-000000000000";
    }
}

export class TagSearchOptions extends SearchOptions {
    q: string;
    documentId: string;
}

export class TagSearchResponse {
    tags: Tag[] = [];
    headers: PagingHeaders;
}

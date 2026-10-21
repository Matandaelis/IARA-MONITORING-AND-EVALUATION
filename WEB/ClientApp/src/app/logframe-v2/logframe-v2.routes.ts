import { Route } from '@angular/router';
import { LogFrameTreeComponent } from './logframe-tree.component';

export const LogFrameV2Routes: Route[] = [
    {
        path: '',
        component: LogFrameTreeComponent,
        pathMatch: 'full'
    }
];

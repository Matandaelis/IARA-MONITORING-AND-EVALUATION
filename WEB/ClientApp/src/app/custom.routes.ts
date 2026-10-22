import { Route } from '@angular/router';
import { AccessGuard } from './common/auth/auth.accessguard';
import { HomeComponent } from './home/home.component';
import { ErrorComponent } from './error/error.component';
import { ErrorsComponent } from './error/errors.component';
import { SetupComponent } from './setup/setup.component';
import { AccountComponent } from './account/account.component';
import { DataEntryComponent } from './forms/dataentry.component';
import { TheoriesOfChangeComponent } from './theoryofchange/theoriesofchange';
import { TheoryOfChangeComponent } from './theoryofchange/theoryofchange';
import { LogFramesComponent } from './logframe/logframes.component';
import { LogFrameComponent } from './logframe/logframe.component';
import { GanttsComponent } from './gantt/gantts.component';
import { GanttComponent } from './gantt/gantt.component';
import { QuestionnairesComponent } from './questionnaire/questionnaires.component';
import { QuestionnaireComponent } from './questionnaire/questionnaire.component';
import { FolderComponent } from './folder/folder.component';
import { ChartsComponent } from './charts/charts.component';
import { ChartComponent } from './charts/chart.component';
import { MeReviewsComponent } from './mereview/mereviews.component';
import { MeReviewDetailComponent } from './mereview/mereview-detail.component';
import { BudgetsComponent } from './budget/budgets.component';
import { BudgetCategoriesComponent } from './budget/budgetcategories.component';
import { FxRatesComponent } from './budget/fxrates.component';
import { ImportsComponent } from './imports/imports.component';

export const CustomRoutes: Route[] = [
    {
        path: '',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: HomeComponent,
        pathMatch: 'full',
        data: {
            breadcrumb: 'Home',
            menu: 'home'
        },
    },
    {
        path: 'account',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: AccountComponent,
        pathMatch: 'full',
        data: {
            breadcrumb: 'Account'
        },
    },
    {
        path: 'setup',
        component: SetupComponent,
        pathMatch: 'full'
    },
    {
        path: 'dataentry',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: DataEntryComponent,
        pathMatch: 'full',
        data: {
            breadcrumb: 'Data Entry',
            menu: 'home'
        },
    },
    {
        path: 'theoryofchange',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: TheoriesOfChangeComponent,
        data: {
            breadcrumb: 'Theories of Change',
            menu: 'home'
        },
        children: [
            {
                path: ':theoryOfChangeId',
                component: TheoryOfChangeComponent,
                canActivate: [AccessGuard],
                canActivateChild: [AccessGuard],
                data: {
                    breadcrumb: 'Add Theory of Change',
                    menu: 'home'
                }
            }
        ]
    },
    {
        path: 'logframe',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: LogFramesComponent,
        data: {
            breadcrumb: 'Logical Frameworks',
            menu: 'home'
        },
        children: [
            {
                path: ':logFrameId',
                component: LogFrameComponent,
                canActivate: [AccessGuard],
                canActivateChild: [AccessGuard],
                data: {
                    breadcrumb: 'Add Logical Framework',
                    menu: 'home'
                }
            }
        ]
    },
    {
        path: 'questionnaire',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: QuestionnairesComponent,
        data: {
            breadcrumb: 'Questionnaires',
            menu: 'home'
        },
        children: [
            {
                path: ':responseId',
                component: QuestionnaireComponent,
                canActivate: [AccessGuard],
                canActivateChild: [AccessGuard],
                data: {
                    breadcrumb: 'Add Questionnaire',
                    menu: 'home'
                }
            }
        ]
    },
    {
        path: 'ganttchart',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: GanttsComponent,
        data: {
            breadcrumb: 'Gantt Charts',
            menu: 'home'
        },
        children: [
            {
                path: ':projectId',
                component: GanttComponent,
                canActivate: [AccessGuard],
                canActivateChild: [AccessGuard],
                data: {
                    breadcrumb: 'Add Gantt Chart',
                    menu: 'home'
                }
            }
        ]
    },
    {
        path: 'charts',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: ChartsComponent,
        data: {
            breadcrumb: 'Charts',
            menu: 'home'
        },
        children: [
            {
                path: ':chartId',
                component: ChartComponent,
                canActivate: [AccessGuard],
                canActivateChild: [AccessGuard],
                data: {
                    breadcrumb: 'Chart',
                    menu: 'home'
                }
            }
        ]
    },
    {
        path: 'mereviews',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: MeReviewsComponent,
        data: {
            breadcrumb: 'M&E Reviews',
            menu: 'home'
        }
    },
    {
        path: 'mereviews/:meReviewId',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: MeReviewDetailComponent,
        data: {
            breadcrumb: 'M&E Review Scorecard',
            menu: 'home'
        }
    },
    {
        path: 'budget',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: BudgetsComponent,
        pathMatch: 'full',
        data: {
            breadcrumb: 'Budget & Expenditures',
            menu: 'home'
        }
    },
    {
        path: 'budget/:projectId',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: BudgetsComponent,
        data: {
            breadcrumb: 'Budget & Expenditures',
            menu: 'home'
        }
    },
    {
        path: 'budgetcategories',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: BudgetCategoriesComponent,
        data: {
            breadcrumb: 'Budget Categories',
            menu: 'home'
        }
    },
    {
        path: 'fxrates',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: FxRatesComponent,
        data: {
            breadcrumb: 'FX Rates',
            menu: 'home'
        }
    },
    {
        path: 'imports',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: ImportsComponent,
        data: {
            breadcrumb: 'Data Import',
            menu: 'home'
        }
    },
    {
        path: 'folder',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: FolderComponent,
        data: {
            breadcrumb: 'Folder',
            menu: 'home'
        },
        children: [
            {
                path: '**',
                component: FolderComponent,
                data: {
                    menu: 'home'
                }
            }
        ]
    },
    {
        path: 'errors',
        canActivate: [AccessGuard],
        canActivateChild: [AccessGuard],
        component: ErrorsComponent,
        data: {
            breadcrumb: 'Errors',
            menu: null
        },
        children: [
            {
                path: ':id',
                component: ErrorComponent,
                canActivate: [AccessGuard],
                canActivateChild: [AccessGuard],
                data: {
                    breadcrumb: 'Error',
                    menu: null

                }
            }
        ]
    }
];

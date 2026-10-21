import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LogFrameV2Routes } from './logframe-v2.routes';
import { LogFrameTreeComponent } from './logframe-tree.component';
import { PrimeNgModule } from '../prime-ng.module';

@NgModule({
    imports: [CommonModule, RouterModule.forChild(LogFrameV2Routes), PrimeNgModule],
    declarations: [LogFrameTreeComponent]
})
export class LogFrameV2Module { }

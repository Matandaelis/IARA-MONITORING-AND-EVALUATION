import { NgModule } from '@angular/core';
import { AccordionModule } from 'primeng/accordion';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { CheckboxModule } from 'primeng/checkbox';
import { ChipModule } from 'primeng/chip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { DrawerModule } from 'primeng/drawer';
import { FileUploadModule } from 'primeng/fileupload';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { MultiSelectModule } from 'primeng/multiselect';
import { ProgressBarModule } from 'primeng/progressbar';
import { RadioButtonModule } from 'primeng/radiobutton';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { SplitterModule } from 'primeng/splitter';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { TooltipModule } from 'primeng/tooltip';
import { TreeTableModule } from 'primeng/treetable';

@NgModule({
    exports: [
        AccordionModule,
        ButtonModule,
        CardModule,
        CheckboxModule,
        ChipModule,
        ConfirmDialogModule,
        DatePickerModule,
        DialogModule,
        DrawerModule,
        FileUploadModule,
        InputNumberModule,
        InputTextModule,
        MessageModule,
        MultiSelectModule,
        ProgressBarModule,
        RadioButtonModule,
        SelectModule,
        SkeletonModule,
        SplitterModule,
        TableModule,
        TagModule,
        TextareaModule,
        ToastModule,
        ToolbarModule,
        TooltipModule,
        TreeTableModule
    ]
})
export class PrimeNgModule { }

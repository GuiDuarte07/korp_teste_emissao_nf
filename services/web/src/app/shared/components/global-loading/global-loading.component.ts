import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LoadingService } from '../../../core/services/loading.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-global-loading',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  templateUrl: './global-loading.component.html',
  styleUrl: './global-loading.component.scss',
})
export class GlobalLoadingComponent {
  loading$: Observable<boolean>;
  loadingMessage$: Observable<string>;

  constructor(private loadingService: LoadingService) {
    this.loading$ = this.loadingService.loading$;
    this.loadingMessage$ = this.loadingService.loadingMessage$;
  }
}

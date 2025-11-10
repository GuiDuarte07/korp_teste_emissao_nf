import { Component } from '@angular/core';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { GlobalLoadingComponent } from './shared/components/global-loading/global-loading.component';
import { ChatFabComponent } from './features/chatbot/components/chat-fab/chat-fab.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [MainLayoutComponent, GlobalLoadingComponent, ChatFabComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'web';
}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  trigger,
  state,
  style,
  transition,
  animate,
} from '@angular/animations';
import { ChatWindowComponent } from '../chat-window/chat-window.component';

@Component({
  selector: 'app-chat-fab',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    ChatWindowComponent,
  ],
  templateUrl: './chat-fab.component.html',
  styleUrl: './chat-fab.component.scss',
  animations: [
    trigger('chatWindow', [
      state(
        'closed',
        style({
          opacity: 0,
          transform: 'scale(0.8) translateY(20px)',
          visibility: 'hidden',
        })
      ),
      state(
        'open',
        style({
          opacity: 1,
          transform: 'scale(1) translateY(0)',
          visibility: 'visible',
        })
      ),
      transition('closed => open', animate('200ms ease-out')),
      transition('open => closed', animate('150ms ease-in')),
    ]),
  ],
})
export class ChatFabComponent {
  isOpen = false;

  toggleChat(): void {
    this.isOpen = !this.isOpen;
  }
}

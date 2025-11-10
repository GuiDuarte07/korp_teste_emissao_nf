import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { ChatMessage } from '../../models';

@Component({
  selector: 'app-chat-message',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './chat-message.component.html',
  styleUrl: './chat-message.component.scss',
})
export class ChatMessageComponent {
  @Input() message!: ChatMessage;

  isUser(): boolean {
    return this.message.role === 'user';
  }

  isAssistant(): boolean {
    return this.message.role === 'assistant';
  }

  isFunction(): boolean {
    return this.message.role === 'function';
  }
}

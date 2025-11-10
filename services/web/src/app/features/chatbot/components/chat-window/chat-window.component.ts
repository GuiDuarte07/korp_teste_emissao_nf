import {
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  AfterViewChecked,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ChatbotService } from '../../services/chatbot.service';
import { ChatMessageComponent } from '../chat-message/chat-message.component';
import { ChatInputComponent } from '../chat-input/chat-input.component';
import { ChatMessage } from '../../models';

@Component({
  selector: 'app-chat-window',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    ChatMessageComponent,
    ChatInputComponent,
  ],
  templateUrl: './chat-window.component.html',
  styleUrl: './chat-window.component.scss',
})
export class ChatWindowComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  messages: ChatMessage[] = [];
  loading = false;
  private shouldScrollToBottom = false;

  constructor(public chatbotService: ChatbotService) {}

  ngOnInit(): void {
    this.chatbotService.messages$.subscribe((messages) => {
      this.messages = messages;
      this.shouldScrollToBottom = true;
    });

    this.chatbotService.loading$.subscribe((loading) => {
      this.loading = loading;
      if (loading) {
        this.shouldScrollToBottom = true;
      }
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  onSendMessage(message: string): void {
    this.chatbotService.sendMessage(message);
  }

  clearChat(): void {
    if (confirm('Deseja limpar o histórico do chat?')) {
      this.chatbotService.clearHistory();
    }
  }

  getVisibleMessages(): ChatMessage[] {
    // Filtrar apenas mensagens de user e assistant com conteúdo
    // Ocultar mensagens de 'function' e mensagens vazias
    return this.messages.filter(
      (msg) =>
        (msg.role === 'user' || msg.role === 'assistant') &&
        msg.content &&
        msg.content.trim().length > 0
    );
  }

  private scrollToBottom(): void {
    try {
      this.messagesContainer.nativeElement.scrollTop =
        this.messagesContainer.nativeElement.scrollHeight;
    } catch (err) {
      console.error('Erro ao fazer scroll:', err);
    }
  }
}

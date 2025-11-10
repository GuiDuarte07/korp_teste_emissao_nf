import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ChatMessage } from '../models';
import { AiService } from './ai.service';
import { ActionExecutorService } from './action-executor.service';

@Injectable({
  providedIn: 'root',
})
export class ChatbotService {
  private messagesSubject = new BehaviorSubject<ChatMessage[]>([]);
  private loadingSubject = new BehaviorSubject<boolean>(false);

  messages$: Observable<ChatMessage[]> = this.messagesSubject.asObservable();
  loading$: Observable<boolean> = this.loadingSubject.asObservable();

  constructor(
    private aiService: AiService,
    private actionExecutor: ActionExecutorService
  ) {
    // Mensagem inicial do assistente
    this.addMessage({
      role: 'assistant',
      content:
        'Olá! Sou o assistente virtual do sistema de notas fiscais. Posso ajudá-lo a:\n\n' +
        '• Criar produtos no inventário\n' +
        '• Criar notas fiscais\n' +
        '• Listar produtos disponíveis\n' +
        '• Consultar status de notas fiscais\n' +
        '• Imprimir notas fiscais\n' +
        '• Cancelar notas fiscais\n\n' +
        'Como posso ajudá-lo hoje?',
      timestamp: new Date(),
    });
  }

  async sendMessage(userMessage: string): Promise<void> {
    if (!userMessage.trim()) return;

    // Adicionar mensagem do usuário
    this.addMessage({
      role: 'user',
      content: userMessage,
      timestamp: new Date(),
    });

    this.loadingSubject.next(true);

    try {
      // Enviar para IA
      const aiResponse = await this.aiService.chat(this.messagesSubject.value);

      // Verificar se IA quer chamar alguma função
      if (aiResponse.tool_calls && aiResponse.tool_calls.length > 0) {
        // Processar cada tool call
        for (const toolCall of aiResponse.tool_calls) {
          const functionName = toolCall.function.name;
          const functionArgs = JSON.parse(toolCall.function.arguments);

          // Adicionar mensagem da IA com a chamada de função (SEM exibir para o usuário)
          // Esta mensagem é apenas para o histórico da IA
          this.addMessage({
            role: 'assistant',
            content: '', // Vazia, não será exibida
            toolCalls: aiResponse.tool_calls,
            timestamp: new Date(),
          });

          // Executar função no backend
          const result = await this.actionExecutor.execute(
            functionName,
            functionArgs
          );

          // Adicionar resultado da função ao histórico (não exibido)
          this.addMessage({
            role: 'function',
            name: functionName,
            content: JSON.stringify(result),
            toolCallId: toolCall.id,
            timestamp: new Date(),
          });

          // Enviar resultado de volta para IA formatar resposta
          const finalResponse = await this.aiService.chatWithFunctionResult(
            this.messagesSubject.value,
            toolCall.id,
            functionName,
            result
          );

          // APENAS esta mensagem será exibida ao usuário
          this.addMessage({
            role: 'assistant',
            content: finalResponse.content || 'Ação executada com sucesso!',
            timestamp: new Date(),
          });
        }
      } else {
        // Resposta direta sem função
        this.addMessage({
          role: 'assistant',
          content: aiResponse.content || 'Desculpe, não entendi.',
          timestamp: new Date(),
        });
      }
    } catch (error: any) {
      console.error('Erro no chatbot:', error);
      this.addMessage({
        role: 'assistant',
        content:
          'Desculpe, ocorreu um erro ao processar sua mensagem. ' +
          (error?.message || 'Tente novamente.'),
        timestamp: new Date(),
      });
    } finally {
      this.loadingSubject.next(false);
    }
  }

  private addMessage(message: ChatMessage): void {
    const messages = this.messagesSubject.value;
    this.messagesSubject.next([...messages, message]);
  }

  clearHistory(): void {
    this.messagesSubject.next([]);
  }

  getMessages(): ChatMessage[] {
    return this.messagesSubject.value;
  }
}

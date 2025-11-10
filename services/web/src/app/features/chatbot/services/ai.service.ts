import { Injectable } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { ChatMessage } from '../models';
import { IAIProvider } from './ai-provider.interface';
import { GeminiProvider } from './gemini-provider.service';
import { OpenAIProvider } from './openai-provider.service';

/**
 * Serviço principal de IA que abstrai o provedor utilizado
 * Permite trocar facilmente entre Gemini, OpenAI, etc.
 */
@Injectable({
  providedIn: 'root',
})
export class AiService {
  private provider: IAIProvider;

  constructor(
    private geminiProvider: GeminiProvider,
    private openaiProvider: OpenAIProvider
  ) {
    // Selecionar provider baseado na configuração
    this.provider = this.selectProvider();
    console.log(`🤖 Usando provider: ${this.provider.getProviderName()}`);
  }

  private selectProvider(): IAIProvider {
    switch (environment.aiProvider) {
      case 'gemini':
        return this.geminiProvider;
      case 'openai':
        return this.openaiProvider;
      default:
        console.warn(
          `Provider '${environment.aiProvider}' desconhecido, usando Gemini`
        );
        return this.geminiProvider;
    }
  }

  async chat(messages: ChatMessage[]): Promise<any> {
    return await this.provider.chat(messages);
  }

  async chatWithFunctionResult(
    messages: ChatMessage[],
    toolCallId: string,
    functionName: string,
    result: any
  ): Promise<any> {
    return await this.provider.chatWithFunctionResult(
      messages,
      toolCallId,
      functionName,
      result
    );
  }

  /**
   * Retorna o nome do provider atual
   */
  getCurrentProvider(): string {
    return this.provider.getProviderName();
  }
}

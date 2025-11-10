import { Injectable } from '@angular/core';
import OpenAI from 'openai';
import { environment } from '../../../../environments/environment';
import { ChatMessage, AI_FUNCTIONS } from '../models';
import { IAIProvider } from './ai-provider.interface';

@Injectable({
  providedIn: 'root',
})
export class OpenAIProvider implements IAIProvider {
  private openai: OpenAI;

  constructor() {
    this.openai = new OpenAI({
      apiKey: environment.openAiApiKey,
      dangerouslyAllowBrowser: true,
    });
  }

  getProviderName(): string {
    return 'OpenAI';
  }

  async chat(messages: ChatMessage[]): Promise<any> {
    try {
      const formattedMessages = messages.map((msg) => ({
        role: msg.role,
        content: msg.content || '',
        ...(msg.toolCalls && { tool_calls: msg.toolCalls }),
        ...(msg.toolCallId && { tool_call_id: msg.toolCallId }),
        ...(msg.name && { name: msg.name }),
      }));

      const response = await this.openai.chat.completions.create({
        model: environment.openAiModel || 'gpt-3.5-turbo',
        messages: formattedMessages as any,
        tools: AI_FUNCTIONS as any,
        tool_choice: 'auto',
      });

      return response.choices[0].message;
    } catch (error: any) {
      console.error('Erro na chamada OpenAI:', error);
      throw new Error(error?.message || 'Erro ao comunicar com o OpenAI');
    }
  }

  async chatWithFunctionResult(
    messages: ChatMessage[],
    toolCallId: string,
    functionName: string,
    result: any
  ): Promise<any> {
    const updatedMessages = [
      ...messages,
      {
        role: 'function' as const,
        name: functionName,
        content: JSON.stringify(result),
        toolCallId: toolCallId,
      },
    ];

    return await this.chat(updatedMessages);
  }
}

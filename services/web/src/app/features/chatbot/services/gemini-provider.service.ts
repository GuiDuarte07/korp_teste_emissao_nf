import { Injectable } from '@angular/core';
import { GoogleGenerativeAI } from '@google/generative-ai';
import { environment } from '../../../../environments/environment';
import { ChatMessage, AI_FUNCTIONS } from '../models';
import { IAIProvider } from './ai-provider.interface';

@Injectable({
  providedIn: 'root',
})
export class GeminiProvider implements IAIProvider {
  private genAI: GoogleGenerativeAI;
  private model: any;
  private lastRequestTime: number = 0;
  private readonly MIN_REQUEST_INTERVAL = 4000; // 4 segundos entre requisições

  constructor() {
    this.genAI = new GoogleGenerativeAI(environment.geminiApiKey);
    this.model = this.genAI.getGenerativeModel({
      model: environment.geminiModel || 'gemini-2.0-flash',
      generationConfig: {
        temperature: 0.7,
        topP: 0.95,
        topK: 64,
        maxOutputTokens: 8192,
      },
    });
  }

  getProviderName(): string {
    return 'Google Gemini';
  }

  private async waitForRateLimit(): Promise<void> {
    const now = Date.now();
    const timeSinceLastRequest = now - this.lastRequestTime;

    if (timeSinceLastRequest < this.MIN_REQUEST_INTERVAL) {
      const waitTime = this.MIN_REQUEST_INTERVAL - timeSinceLastRequest;
      console.log(
        `⏳ Aguardando ${Math.ceil(waitTime / 1000)}s para evitar rate limit...`
      );
      await new Promise((resolve) => setTimeout(resolve, waitTime));
    }

    this.lastRequestTime = Date.now();
  }

  private async retryWithBackoff<T>(
    fn: () => Promise<T>,
    maxRetries: number = 3
  ): Promise<T> {
    for (let attempt = 0; attempt < maxRetries; attempt++) {
      try {
        await this.waitForRateLimit();
        return await fn();
      } catch (error: any) {
        const is429 =
          error?.message?.includes('429') ||
          error?.message?.includes('Resource exhausted');
        const is503 =
          error?.message?.includes('503') ||
          error?.message?.includes('overloaded');

        if ((is429 || is503) && attempt < maxRetries - 1) {
          const waitTime = Math.pow(2, attempt) * 5000; // 5s, 10s, 20s
          console.log(
            `⚠️ Rate limit/overload (tentativa ${
              attempt + 1
            }/${maxRetries}). Aguardando ${waitTime / 1000}s...`
          );
          await new Promise((resolve) => setTimeout(resolve, waitTime));
          continue;
        }

        throw error;
      }
    }

    throw new Error('Máximo de tentativas excedido');
  }

  async chat(messages: ChatMessage[]): Promise<any> {
    return this.retryWithBackoff(async () => {
      try {
        // Converter formato de tools para Gemini
        const tools = this.convertToGeminiTools();

        // Converter histórico de mensagens para formato Gemini
        const geminiMessages = this.convertMessagesToGemini(messages);

        // Se não houver mensagens ou histórico inválido, criar conversa simples
        if (geminiMessages.length === 0) {
          throw new Error('Nenhuma mensagem válida para processar');
        }

        // Para uma única mensagem do usuário, enviar diretamente
        if (geminiMessages.length === 1 && geminiMessages[0].role === 'user') {
          const chat = this.model.startChat({
            tools: [{ functionDeclarations: tools }],
          });
          const result = await chat.sendMessage(geminiMessages[0].parts);
          return this.formatResponse(result.response);
        }

        // Para múltiplas mensagens, criar chat com histórico
        // Garantir que temos pelo menos 2 mensagens (history + última)
        if (geminiMessages.length < 2) {
          const chat = this.model.startChat({
            tools: [{ functionDeclarations: tools }],
          });
          const result = await chat.sendMessage(geminiMessages[0].parts);
          return this.formatResponse(result.response);
        }

        // Criar chat com histórico (todas exceto a última)
        const chat = this.model.startChat({
          history: geminiMessages.slice(0, -1),
          tools: [{ functionDeclarations: tools }],
        });

        // Enviar última mensagem
        const lastMessage = geminiMessages[geminiMessages.length - 1];
        const result = await chat.sendMessage(lastMessage.parts);

        return this.formatResponse(result.response);
      } catch (error: any) {
        console.error('Erro na chamada Gemini:', error);
        throw new Error(error?.message || 'Erro ao comunicar com o Gemini');
      }
    });
  }

  private formatResponse(response: any): any {
    const functionCalls = response.functionCalls();

    // Se houver chamadas de função, retornar no formato esperado
    if (functionCalls && functionCalls.length > 0) {
      return {
        content: response.text() || '',
        tool_calls: functionCalls.map((fc: any, index: number) => ({
          id: `call_${Date.now()}_${index}`,
          type: 'function',
          function: {
            name: fc.name,
            arguments: JSON.stringify(fc.args),
          },
        })),
      };
    }

    // Resposta direta sem função
    return {
      content: response.text(),
      tool_calls: null,
    };
  }

  async chatWithFunctionResult(
    messages: ChatMessage[],
    toolCallId: string,
    functionName: string,
    result: any
  ): Promise<any> {
    return this.retryWithBackoff(async () => {
      try {
        // Converter mensagens para formato Gemini
        const geminiMessages = this.convertMessagesToGemini(messages);
        const tools = this.convertToGeminiTools();

        // Criar chat com histórico
        const chat = this.model.startChat({
          history: geminiMessages,
          tools: [{ functionDeclarations: tools }],
        });

        // Enviar resultado da função
        const functionResponse = {
          functionResponse: {
            name: functionName,
            response: result,
          },
        };

        const finalResult = await chat.sendMessage([functionResponse]);
        const response = finalResult.response;

        return {
          content: response.text(),
          tool_calls: null,
        };
      } catch (error: any) {
        console.error('Erro ao processar resultado da função:', error);
        throw new Error(error?.message || 'Erro ao processar resposta');
      }
    });
  }

  private convertToGeminiTools(): any[] {
    return AI_FUNCTIONS.map((func) => ({
      name: func.function.name,
      description: func.function.description,
      parameters: {
        type: 'object',
        properties: func.function.parameters.properties,
        required: func.function.parameters.required || [],
      },
    }));
  }

  private convertMessagesToGemini(messages: ChatMessage[]): any[] {
    const geminiMessages: any[] = [];

    for (const msg of messages) {
      // Pular mensagens de função (são tratadas separadamente)
      if (msg.role === 'function') continue;

      if (msg.role === 'user') {
        geminiMessages.push({
          role: 'user',
          parts: [{ text: msg.content }],
        });
      } else if (msg.role === 'assistant') {
        // Se tem tool_calls, adicionar como chamada de função
        if (msg.toolCalls && msg.toolCalls.length > 0) {
          const functionCalls = msg.toolCalls.map((tc) => ({
            functionCall: {
              name: tc.function.name,
              args: JSON.parse(tc.function.arguments),
            },
          }));

          geminiMessages.push({
            role: 'model',
            parts: functionCalls,
          });
        } else if (msg.content) {
          // Resposta normal do assistente (só adicionar se tiver conteúdo)
          geminiMessages.push({
            role: 'model',
            parts: [{ text: msg.content }],
          });
        }
      } else if (msg.role === 'system') {
        // System messages convertidas para user (Gemini não tem role 'system')
        geminiMessages.push({
          role: 'user',
          parts: [{ text: `[Instruções]: ${msg.content}` }],
        });
      }
    }

    // CRÍTICO: Garantir que a primeira mensagem seja sempre 'user'
    // Se a primeira mensagem for 'model' (mensagem inicial do assistente), remover
    while (geminiMessages.length > 0 && geminiMessages[0].role === 'model') {
      geminiMessages.shift();
    }

    // Se após remoção não houver mensagens do user, adicionar uma genérica
    if (geminiMessages.length === 0 || geminiMessages[0].role !== 'user') {
      geminiMessages.unshift({
        role: 'user',
        parts: [{ text: 'Olá!' }],
      });
    }

    return geminiMessages;
  }
}

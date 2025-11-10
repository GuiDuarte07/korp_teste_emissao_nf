import { ChatMessage } from '../models';

/**
 * Interface abstrata para provedores de IA
 * Permite trocar facilmente entre OpenAI, Gemini, Claude, etc.
 */
export interface IAIProvider {
  /**
   * Envia mensagens para a IA e recebe resposta
   * @param messages Histórico de mensagens da conversa
   * @returns Resposta da IA com possíveis chamadas de função
   */
  chat(messages: ChatMessage[]): Promise<any>;

  /**
   * Envia resultado de uma função executada de volta para a IA
   * @param messages Histórico de mensagens
   * @param toolCallId ID da chamada de ferramenta
   * @param functionName Nome da função executada
   * @param result Resultado da execução
   * @returns Resposta final formatada pela IA
   */
  chatWithFunctionResult(
    messages: ChatMessage[],
    toolCallId: string,
    functionName: string,
    result: any
  ): Promise<any>;

  /**
   * Nome do provedor (para debug/logs)
   */
  getProviderName(): string;
}

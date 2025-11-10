export interface ChatMessage {
  role: 'user' | 'assistant' | 'system' | 'function';
  content: string;
  timestamp?: Date;
  functionCall?: {
    name: string;
    arguments: string;
  };
  toolCalls?: ToolCall[];
  toolCallId?: string;
  name?: string;
}

export interface ToolCall {
  id: string;
  type: 'function';
  function: {
    name: string;
    arguments: string;
  };
}

export interface FunctionResult {
  success: boolean;
  data?: any;
  error?: string;
}

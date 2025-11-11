export const environment = {
  production: false,
  apiUrl: 'http://localhost:5263/api',

  aiProvider: 'gemini' as 'gemini' | 'openai',

  // Preencha localmente
  openAiApiKey: '',
  openAiModel: 'gpt-3.5-turbo',

  // Preencha localmente
  geminiApiKey: '',
  geminiModel: 'gemini-2.0-flash',
};

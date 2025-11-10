export interface AIFunction {
  type: 'function';
  function: {
    name: string;
    description: string;
    parameters: {
      type: 'object';
      properties: Record<string, any>;
      required?: string[];
    };
  };
}

export const AI_FUNCTIONS: AIFunction[] = [
  {
    type: 'function',
    function: {
      name: 'create_product',
      description:
        'Criar um novo produto no sistema de inventário com código, descrição e estoque inicial',
      parameters: {
        type: 'object',
        properties: {
          code: {
            type: 'string',
            description:
              'Código único do produto (ex: NB001, CAD001). Máximo 10 caracteres.',
          },
          description: {
            type: 'string',
            description: 'Descrição completa do produto. Mínimo 3 caracteres.',
          },
          initialStock: {
            type: 'number',
            description: 'Quantidade inicial em estoque (deve ser maior que 0)',
            minimum: 1,
          },
        },
        required: ['code', 'description', 'initialStock'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'create_invoice',
      description:
        'Criar uma nova nota fiscal com os produtos especificados. Use os códigos exatos dos produtos.',
      parameters: {
        type: 'object',
        properties: {
          products: {
            type: 'array',
            description:
              'Lista de produtos com código e quantidade. Exemplo: [{"productCode": "NB001", "quantity": 2}]',
            items: {
              type: 'object',
              properties: {
                productCode: {
                  type: 'string',
                  description: 'Código do produto',
                },
                quantity: {
                  type: 'number',
                  description: 'Quantidade do produto',
                },
              },
              required: ['productCode', 'quantity'],
            },
          },
        },
        required: ['products'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'list_products',
      description:
        'Listar todos os produtos disponíveis no sistema com seus códigos, descrições e estoque',
      parameters: {
        type: 'object',
        properties: {
          searchTerm: {
            type: 'string',
            description: 'Termo de busca opcional para filtrar produtos',
          },
        },
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'get_invoice_by_number',
      description:
        'Consultar informações detalhadas de uma nota fiscal pelo número',
      parameters: {
        type: 'object',
        properties: {
          invoiceNumber: {
            type: 'string',
            description: 'Número da nota fiscal (formato: NF-XXXXXX)',
          },
        },
        required: ['invoiceNumber'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'list_invoices',
      description:
        'Listar todas as notas fiscais do sistema com filtros opcionais',
      parameters: {
        type: 'object',
        properties: {
          status: {
            type: 'string',
            description:
              'Filtrar por status: Open (Aberta) ou Closed (Fechada)',
            enum: ['Open', 'Closed'],
          },
          includeCancelled: {
            type: 'boolean',
            description: 'Incluir notas fiscais canceladas',
          },
        },
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'cancel_invoice',
      description:
        'Cancelar uma nota fiscal existente. Isso irá liberar as reservas de estoque.',
      parameters: {
        type: 'object',
        properties: {
          invoiceNumber: {
            type: 'string',
            description: 'Número da nota fiscal a ser cancelada',
          },
        },
        required: ['invoiceNumber'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'print_invoice',
      description:
        'Imprimir uma nota fiscal aberta. Após impressa, a nota fiscal será fechada e não poderá ser alterada.',
      parameters: {
        type: 'object',
        properties: {
          invoiceNumber: {
            type: 'string',
            description: 'Número da nota fiscal a ser impressa',
          },
        },
        required: ['invoiceNumber'],
      },
    },
  },
];

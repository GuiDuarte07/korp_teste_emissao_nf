# Korp - Sistema de Emissão de Notas Fiscais

Sistema de microserviços para gerenciamento de produtos, estoque e emissão de notas fiscais.

## 🏗️ Arquitetura

- **ApiGateway** (porta 5263): Gateway de entrada com padrão SAGA para orquestração
- **InventoryService** (porta 5038): Gerenciamento de produtos e estoque
- **InvoiceService** (porta 5099): Gerenciamento de notas fiscais
- **RabbitMQ** (portas 5672/15672): Message broker para comunicação entre serviços
- **PostgreSQL**: Dois bancos de dados independentes (portas 5432/5434)

## 🚀 Como Executar

### Opção 1: Docker Compose (Recomendado)

```bash
# Na pasta infra/
cd infra

# Subir todos os serviços
docker-compose up -d

# Ver logs
docker-compose logs -f

# Parar todos os serviços
docker-compose down
```

### Opção 2: Local (Desenvolvimento)

```bash
# 1. Subir infraestrutura (PostgreSQL + RabbitMQ)
cd infra
docker-compose up -d postgres-inventory postgres-invoice rabbitmq

# 2. Executar InventoryService
cd ../services/InventoryService
dotnet run

# 3. Executar InvoiceService (em outro terminal)
cd ../services/InvoiceService
dotnet run

# 4. Executar ApiGateway (em outro terminal)
cd ../services/ApiGateway/ApiGateway
dotnet run
```

## 📡 Endpoints

### ApiGateway: http://localhost:5263

**Produtos:**
- `GET    /api/inventory/products` - Lista todos os produtos
- `GET    /api/inventory/products/{id}` - Busca produto por ID
- `POST   /api/inventory/products` - Cria novo produto
- `PUT    /api/inventory/products/{id}` - Atualiza produto
- `DELETE /api/inventory/products/{id}` - Deleta produto

**Notas Fiscais:**
- `GET    /api/invoices` - Lista todas as notas
- `GET    /api/invoices/{id}` - Busca nota por ID
- `POST   /api/invoices` - Cria nota + reserva estoque (SAGA)
- `DELETE /api/invoices/{id}` - Deleta nota (apenas Open)
- `POST   /api/invoices/{id}/print` - Imprime nota (fecha e debita estoque)

**Swagger UI:**
- http://localhost:5263/swagger

**RabbitMQ Management:**
- http://localhost:15672
- Usuário: `korp_admin`
- Senha: `R@bb1t@2025#Secure!`

## 🔄 Fluxo SAGA de Criação de Nota Fiscal

```
1. Frontend envia POST /api/invoices com lista de produtos
2. ApiGateway inicia SAGA:
   ├─ Passo 1: Cria nota fiscal (status: Open) no InvoiceService
   ├─ Passo 2: Reserva estoque no InventoryService
   └─ Se falhar: COMPENSAÇÃO - deleta nota criada
3. Retorna nota criada com status Open
```

## 📋 Exemplo de Uso

### 1. Criar Produto

```bash
POST http://localhost:5263/api/inventory/products
Content-Type: application/json

{
  "code": "PROD001",
  "description": "Notebook Dell",
  "stock": 50
}
```

### 2. Criar Nota Fiscal (SAGA)

```bash
POST http://localhost:5263/api/invoices
Content-Type: application/json

{
  "items": [
    {
      "productId": "guid-do-produto",
      "quantity": 2
    }
  ]
}
```

### 3. Imprimir Nota (Fecha e Debita Estoque)

```bash
POST http://localhost:5263/api/invoices/{invoice-id}/print
```

## 🛠️ Tecnologias

- .NET 8.0
- Entity Framework Core 9.0
- MassTransit 8.5.5 (Request/Response pattern)
- RabbitMQ
- PostgreSQL
- Docker

## 📝 Regras de Negócio

- Nota fiscal criada começa com status **Open**
- Ao criar nota, estoque é **reservado** (não debitado)
- Somente notas **Open** podem ser deletadas
- Ao **imprimir** nota:
  - Status muda para **Closed**
  - Estoque é **debitado** (confirmação das reservas)
  - Nota não pode mais ser impressa novamente
- **Padrão SAGA** garante consistência: se reserva falhar, nota é deletada automaticamente

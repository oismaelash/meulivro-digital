# 📚 BookWise — Catálogo Inteligente de Livros

> Sistema completo de gerenciamento de livros com **IA integrada** (DeepSeek e Claude), construído com Clean Architecture, .NET 8, PostgreSQL e React.

---

## ✨ Destaques

| Feature | Tecnologia |
|---|---|
| API REST com versionamento | .NET 8 + ASP.NET Core |
| Banco de dados relacional | PostgreSQL + EF Core 8 |
| ORM + Migrations | Entity Framework Core |
| Documentação interativa | Swagger / OpenAPI |
| Frontend SPA | React 18 + TypeScript |
| Gerenciamento de estado | Context API + useReducer |
| **IA: Geração de sinopses** | DeepSeek ou Claude (Anthropic) |
| **IA: Recomendações** | DeepSeek ou Claude (Anthropic) |
| **IA: Chatbot de livros** | DeepSeek ou Claude (Anthropic) |
| **IA: Análise de tendências** | DeepSeek ou Claude (Anthropic) |
| Autenticação | JWT + OTP (WhatsApp) + Google OIDC |
| Busca remota de livros | Google Books + Open Library |
| Testes unitários (Backend) | xUnit + Moq |
| Testes unitários (Frontend) | Vitest + Testing Library |
| Containerização | Docker + Docker Compose |

---

## 🏗️ Arquitetura

```
bookwise/
├── backend/
│   ├── BookWise.Domain/          # Entidades, interfaces, regras de negócio
│   ├── BookWise.Application/     # Services, DTOs, ViewModels, interfaces
│   ├── BookWise.Infrastructure/  # EF Core, repositórios, AI service
│   ├── BookWise.API/             # Controllers v1, Swagger, Program.cs
│   └── BookWise.Tests/           # Testes unitários (xUnit + Moq)
└── frontend/
    └── src/
        ├── pages/                # Dashboard, Books, Authors, Genres, AI
        ├── services/             # API service (typed, centralized)
        ├── store/                # Context + Reducer (global state)
        └── test/                 # Testes unitários (Vitest)
```

### Padrões e Boas Práticas Implementadas
- **Clean Architecture** — separação total entre Domain, Application, Infrastructure e API
- **Repository Pattern + Unit of Work** — abstração da camada de dados
- **Dependency Injection** — todos os serviços injetados via DI nativa do .NET
- **DTOs e ViewModels** — separação entre entidades de domínio e contratos de API
- **Soft Delete** — registros nunca são deletados fisicamente
- **API Versionamento** — rotas em `/api/v1/`
- **Respostas padronizadas** — `ApiResponse<T>` consistente com HTTP status codes
- **Auto-migrations + seed** — aplica migrations no startup e faz seed idempotente de gêneros
- **Environments** — `appsettings.json` + variáveis de ambiente (Docker/CI)

---

## 🤖 Features de IA

### 1. Geração de Sinopse
Gera automaticamente uma sinopse atraente para qualquer livro com base no título, autor e gênero.
```
POST /api/v1/ai/synopsis
```

### 2. Recomendações Personalizadas
Recomenda livros similares do catálogo usando análise semântica do Claude.
```
GET /api/v1/ai/recommendations/{bookId}
```

### 3. Análise de Tendências
Analisa a distribuição do catálogo por gênero e gera insights estratégicos.
```
GET /api/v1/ai/trends
```

### 4. Chatbot de Livros
Assistente conversacional com acesso ao catálogo completo para responder perguntas em linguagem natural.
```
POST /api/v1/ai/chat
```

---

## 🚀 Como Executar

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 16](https://www.postgresql.org/) ou [Docker](https://www.docker.com/)
- Chave(s) de IA (pelo menos uma): [Anthropic](https://console.anthropic.com/) e/ou [DeepSeek](https://platform.deepseek.com/)

---

### Variáveis de ambiente (Docker/produção)

Crie um arquivo `.env` na raiz do repositório (usado pelo Docker Compose) com, no mínimo:

```bash
Auth__Jwt__SigningKey=uma_chave_longa_e_segura
```

E configure pelo menos um provider de IA:

```bash
ANTHROPIC_API_KEY=...
# ou
DEEPSEEK_API_KEY=...
```

Opcional (dependendo das features usadas):

```bash
GoogleBooks__ApiKey=...
Auth__Google__ClientId=...
PilotStatus__ApiKey=...
PilotStatus__TemplateId=...
```

---

### Opção 1: Docker Compose (Recomendado)

```bash
# 1. Clone o repositório
git clone https://github.com/seu-usuario/bookwise.git
cd bookwise

# 2. Suba tudo
docker compose up --build

# Acesse:
# Frontend: http://localhost:4000
# API + Swagger: http://localhost:5000
# Postgres: localhost:45432
```

---

### Opção 2: Docker Compose (Dev com hot reload)

```bash
docker compose -f docker-compose.dev.yml up --build
```

---

### Opção 3: Docker Compose (Prod)

```bash
docker compose -f docker-compose.prod.yml up --build
```

---

### Opção 4: Manual

#### Backend

```bash
cd backend

# Configure via variáveis de ambiente (recomendado)
# ConnectionStrings__DefaultConnection="Host=localhost;Port=45432;Database=bookwise;Username=postgres;Password=postgres"
# Anthropic__ApiKey="..."
# DeepSeek__ApiKey="..."
# Auth__Jwt__SigningKey="uma_chave_longa_e_segura"

# Restaure e execute
dotnet restore
dotnet run --project BookWise.API

# O startup aplica migrations automaticamente e faz seed de gêneros
# Swagger (somente em Development): http://localhost:5000 (pode redirecionar para https://localhost:5001)
```

#### Frontend

```bash
cd frontend

# Configure o ambiente
echo "VITE_API_URL=http://localhost:5000/api/v1" > .env.development

# Instale e execute
npm install
npm run dev -- --port 4000

# Acesse: http://localhost:4000
```

---

## 🧪 Testes

### Backend (xUnit)
```bash
cd backend
dotnet test BookWise.Tests/BookWise.Tests.csproj --verbosity normal
```

### Frontend (Vitest)
```bash
cd frontend
npm test
npm run test:coverage
```

---

## 📖 Endpoints da API

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/v1/books` | Listar todos os livros |
| GET | `/api/v1/books/{id}` | Buscar livro por ID |
| GET | `/api/v1/books/search?term=` | Pesquisar livros |
| GET | `/api/v1/books/remote-search?term=&sources=google,openlibrary` | Buscar livros remotamente |
| POST | `/api/v1/books/import` | Importar livro remoto (cria autor se necessário) |
| POST | `/api/v1/books` | Criar livro |
| PUT | `/api/v1/books/{id}` | Atualizar livro |
| DELETE | `/api/v1/books/{id}` | Remover livro (soft delete) |
| GET | `/api/v1/authors` | Listar autores |
| POST | `/api/v1/authors` | Criar autor |
| PUT | `/api/v1/authors/{id}` | Atualizar autor |
| DELETE | `/api/v1/authors/{id}` | Remover autor |
| GET | `/api/v1/genres` | Listar gêneros |
| POST | `/api/v1/genres` | Criar gênero |
| PUT | `/api/v1/genres/{id}` | Atualizar gênero |
| DELETE | `/api/v1/genres/{id}` | Remover gênero |
| POST | `/api/v1/auth/otp/request` | Solicitar OTP (WhatsApp) |
| POST | `/api/v1/auth/otp/verify` | Validar OTP e emitir JWT |
| POST | `/api/v1/auth/google` | Login Google e emitir JWT |
| GET | `/api/v1/auth/me` | Dados do usuário autenticado |
| POST | `/api/v1/ai/synopsis` | Gerar sinopse com IA |
| GET | `/api/v1/ai/recommendations/{id}` | Recomendações por IA |
| GET | `/api/v1/ai/trends` | Análise de tendências |
| POST | `/api/v1/ai/chat` | Chat com IA |

---

## 🎯 Decisões Técnicas

**Por que Clean Architecture?**
Permite substituir qualquer camada (ex: trocar PostgreSQL por outro banco) sem impacto no domínio. A testabilidade é maximizada pois o domínio e a aplicação não dependem de frameworks externos.

**Por que Unit of Work + Repository?**
Centraliza o controle de transações e permite mockar o acesso a dados facilmente nos testes unitários, sem necessitar de banco real.

**Por que DeepSeek e Claude (Anthropic)?**
O backend suporta múltiplos providers para equilibrar custo, latência e qualidade. A aplicação usa as chaves configuradas (`DeepSeek__ApiKey` e/ou `Anthropic__ApiKey`) e direciona as rotas de IA para o provider disponível.

**Por que Context API + useReducer ao invés de Redux?**
Para a escala deste projeto, Context + useReducer oferece gerenciamento de estado robusto sem a complexidade adicional do Redux. O padrão é familiar para desenvolvedores React e mantém o código mais conciso.

---

## 📄 Licença

MIT — desenvolvido como parte de um processo seletivo técnico.

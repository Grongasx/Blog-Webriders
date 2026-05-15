# THROTTLE Blog — Back-end C# ASP.NET Core MVC

## Estrutura do Projeto

```
ThrottleBlog/
│
├── ThrottleBlog.csproj          # Projeto web .NET 8
├── Program.cs                   # Bootstrap, DI, pipeline, seed
├── appsettings.json             # Connection string (SQL Server)
│
├── Models/
│   └── Domain.cs                # Entidades: Post, Category, Tag,
│                                #   ApplicationUser, BlogSettings,
│                                #   NewsletterSubscriber
│
├── Data/
│   └── AppDbContext.cs          # EF Core DbContext + seed de categorias
│
├── ViewModels/
│   └── ViewModels.cs            # LoginVM, HomeVM, PostDetailVM,
│                                #   PostFormVM, DashboardVM,
│                                #   SettingsVM, PaginationVM...
│
├── Services/
│   └── Services.cs              # IPostService / PostService
│                                # ICategoryService / CategoryService
│                                # ISettingsService / SettingsService
│                                # IDashboardService / DashboardService
│                                # INewsletterService / NewsletterService
│                                # SlugHelper (utilitário estático)
│
├── Controllers/
│   ├── HomeController.cs        # Blog público (home, detalhe, categoria, busca, newsletter)
│   ├── AuthController.cs        # Login / Logout via Identity
│   └── AdminController.cs       # Painel admin (dashboard, posts CRUD,
│                                #   categorias, configurações, senha)
│
└── Views/
    ├── _ViewImports.cshtml
    ├── _ViewStart.cshtml
    ├── Shared/
    │   ├── _Layout.cshtml       # Layout público do blog
    │   └── _AdminLayout.cshtml  # Layout do painel admin (sidebar + topbar)
    ├── Auth/
    │   └── Login.cshtml         # Tela de login standalone
    ├── Home/
    │   ├── Index.cshtml         # Homepage pública
    │   └── Detail.cshtml        # Detalhe do post (Markdown → HTML)
    └── Admin/
        ├── Index.cshtml         # Dashboard (stats + gráfico + recentes)
        ├── Posts.cshtml         # Listagem com busca, filtros e paginação
        ├── PostForm.cshtml      # Formulário criar/editar post
        ├── Categories.cshtml    # Grid de categorias
        ├── CategoryForm.cshtml  # Formulário criar/editar categoria
        └── Settings.cshtml      # 4 seções: info, features, senha, sociais
```

---

## Stack & Pacotes

| Pacote | Uso |
|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | ORM + migrations |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Autenticação/autorização |
| `Markdig` | Renderização de Markdown → HTML nos posts |

---

## Como Executar

### 1. Pré-requisitos
- .NET 8 SDK
- SQL Server (LocalDB, Express ou completo)

### 2. Configurar connection string
Edite `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ThrottleBlog;Trusted_Connection=True;"
  }
}
```

### 3. Criar e aplicar migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
> O seed automático em `Program.cs` cria o usuário `admin / throttle2025`,
> a role `Admin` e as 6 categorias padrão na primeira execução.

### 4. Rodar
```bash
dotnet run
```

### 5. Acessar
| URL | Descrição |
|---|---|
| `https://localhost:5001/` | Blog público |
| `https://localhost:5001/admin` | Painel admin (redireciona para login) |
| `https://localhost:5001/admin/login` | Login — `admin` / `throttle2025` |

---

## Rotas Principais

### Blog Público (`HomeController`)
```
GET  /                      → Homepage (hero, últimos posts, categorias, newsletter)
GET  /post/{slug}           → Detalhe do post (conteúdo Markdown renderizado)
GET  /categoria/{slug}      → Posts filtrados por categoria (paginado)
GET  /buscar?q=honda        → Pesquisa de posts
POST /newsletter            → Inscrição na newsletter
```

### Autenticação (`AuthController`)
```
GET  /admin/login           → Formulário de login
POST /admin/login           → Processar login (Identity)
POST /admin/logout          → Logout
```

### Admin (`AdminController`)  — requer role `Admin`
```
GET  /admin                       → Dashboard
GET  /admin/posts                 → Listagem (filtros: busca, categoria, status)
GET  /admin/posts/novo            → Formulário novo post
GET  /admin/posts/{id}/editar     → Formulário editar post
POST /admin/posts/salvar          → Criar ou atualizar post
POST /admin/posts/{id}/publicar   → Publicar rascunho
POST /admin/posts/{id}/despublicar→ Mover para rascunho
POST /admin/posts/{id}/excluir    → Excluir post

GET  /admin/categorias            → Listar categorias
GET  /admin/categorias/nova       → Formulário nova categoria
GET  /admin/categorias/{id}/editar→ Formulário editar categoria
POST /admin/categorias/salvar     → Criar ou atualizar categoria
POST /admin/categorias/{id}/excluir→ Excluir categoria

GET  /admin/configuracoes         → Formulário de settings
POST /admin/configuracoes         → Salvar settings
POST /admin/configuracoes/senha   → Alterar credenciais admin
```

---

## Modelos de Dados

```
ApplicationUser (IdentityUser)
  + DisplayName, AvatarUrl, CreatedAt
  → Posts[]

Category
  Id, Name, Slug*, ImageUrl, SortOrder
  → Posts[]

Post
  Id, Title, Slug*, Excerpt, Content (Markdown)
  FeaturedImage, ReadingTime, Status (Draft|Published)
  IsFeatured, CreatedAt, UpdatedAt, PublishedAt
  → Category, Author, PostTags[]

Tag
  Id, Name, Slug*
  → PostTags[]

PostTag (N:N)
  PostId, TagId

BlogSettings (1 linha)
  BlogName, Tagline, TopBarText, BlogUrl
  NewsletterEnabled, CommentsEnabled, MaintenanceMode
  TickerEnabled, FeaturedEnabled
  Instagram, YouTube, Twitter, LinkedIn

NewsletterSubscriber
  Id, Name, Email, SubscribedAt, IsActive
```
*campos com índice único

---

## Segurança
- Login via **ASP.NET Core Identity** com lockout após 5 tentativas
- Todas as rotas `/admin/*` protegidas com `[Authorize(Roles = "Admin")]`
- Anti-forgery token em todos os formulários POST
- Senha mínima de 8 caracteres configurável em `Program.cs`
- Cookie de sessão com SlidingExpiration de 8h

---

## Próximos Passos Sugeridos
- [ ] Adicionar `wwwroot/css/site.css` e `admin.css` (adaptar os HTMLs fornecidos)
- [ ] Adicionar `wwwroot/js/site.js` e `admin.js`
- [ ] Adicionar migration e rodar `dotnet ef database update`
- [ ] Upload de imagem local (substituir URL externa por `IFormFile` + disco/blob)
- [ ] Contador de visualizações nos posts
- [ ] Sistema de comentários (se `CommentsEnabled`)
- [ ] Testes unitários nos Services (`xUnit` + `Moq`)

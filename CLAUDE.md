# Blog Webriders (ThrottleBlog)

Blog ASP.NET Core MVC (.NET 8) com painel admin, Identity e PostgreSQL (Neon).

## Comandos

```bash
dotnet build
dotnet run
dotnet ef migrations add <Nome>
dotnet ef database update
```

URLs locais típicas: `https://localhost:5001/` (público), `https://localhost:5001/admin` (admin).

## Stack

- ASP.NET Core MVC 8, Razor Views
- EF Core + **Npgsql** (PostgreSQL / Neon)
- ASP.NET Core Identity (role `Admin`)
- Markdig (Markdown nos posts)
- Deploy: Vercel (`vercel.json`, runtime `vercel-dotnet`)

## Estrutura

| Pasta | Conteúdo |
|-------|----------|
| `Controllers/` | `HomeController`, `AuthController`, `AdminController`, controllers de seção |
| `Models/Domain.cs` | Entidades |
| `Data/AppDbContext.cs` | DbContext + seeds |
| `Services/Services.cs` | Lógica de negócio |
| `ViewModels/` | VMs para views |
| `Views/` | Razor (público + `Admin/`) |
| `Infrastructure/` | Workers, seeders |
| `wwwroot/` | CSS/JS estáticos |

## Convenções

- Namespace raiz: `ThrottleBlog`
- Serviços registrados em `Program.cs` com interfaces `I*Service`
- Rotas admin: prefixo `/admin`, cookie login em `/admin/login`
- Slugs gerados via `SlugHelper` em `Services.cs`
- Posts: conteúdo em Markdown; status `Draft` / `Published`
- Não commitar credenciais; connection string em `appsettings.json` — tratar como segredo em PRs e logs

## Banco de dados

- Provider: PostgreSQL (`UseNpgsql`)
- Migrations em `Migrations/`
- Seed em `Program.cs`: usuário `admin`, role `Admin`, categorias via `WebridersCategorySeeder`
- Para tarefas Neon (pooling, branches, connection string): usar a skill `neon-postgres` em `.claude/skills/`

## Segurança

- Rotas `/admin/*` exigem `[Authorize(Roles = "Admin")]`
- Anti-forgery em formulários POST
- Não expor senhas, tokens ou connection strings em código novo ou mensagens

## Escopo de mudanças

- Preferir diffs pequenos e alinhados ao estilo existente
- Não alterar `obj/`, `bin/` nem artefatos de build
- Atualizar `README.md` só quando a mudança afetar setup ou rotas documentadas

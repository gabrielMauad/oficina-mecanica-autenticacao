# oficina-mecanica-autenticacao

Microsserviço de **autenticação da oficina**: emite o JWT (HS256) usado pelos demais serviços do
sistema de oficina mecânica. É um serviço **sem banco de dados** — o usuário administrador vem da
configuração (`Auth__AdminEmail` / `Auth__AdminSenha`).

Extraído do módulo `Autenticacao` do monólito (Clean Architecture: `Application` → `Adapters` →
`Infrastructure`/`Web`, host em `src/Bootstrap/Api`).

## Endpoint

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/v1/auth/login` | Body `{"email","senha"}`. 200 com `{"token","expiresAt"}` (papel `Oficina`, 1 h) ou 401 |

Também expõe `/healthz`, `/healthz/live`, `/healthz/ready` (refletem apenas o processo),
`/openapi/v1.json` e `/scalar`.

## Configuração

| Chave | Valor local |
|---|---|
| `Jwt__Secret` | `docker-dev-secret-key-replace-in-prod-000` |
| `Jwt__Issuer` | `oficina-mecanica-app` |
| `Jwt__Audience` | `oficina-mecanica-api` |
| `Jwt__ValidIssuers__0` / `__1` | `oficina-mecanica-app` / `oficina-mecanica-auth` |
| `Auth__AdminEmail` / `Auth__AdminSenha` | `admin@oficina.com` / `admin123` |
| `OpenApi__Enabled` | `true` |
| `OTEL_SERVICE_NAME` | `oficina-mecanica-autenticacao` (padrão) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://oficina-jaeger:4317` |

## Como rodar

Com Docker (porta `8081`), na rede externa `oficina-local`:

```bash
# 1. no repositório oficina-mecanica-app: cria a rede oficina-local e sobe o Jaeger
docker compose -f docker-compose.infra.yml up -d

# 2. neste repositório
docker compose up --build
```

```bash
curl -X POST http://localhost:8081/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@oficina.com","senha":"admin123"}'
```

Documentação interativa: <http://localhost:8081/scalar>.

Sem Docker: `dotnet run --project src/Bootstrap/Api` (perfil `Development`, porta `5294`).

## Como testar

```bash
dotnet build OficinaMecanica.Autenticacao.slnx
dotnet test OficinaMecanica.Autenticacao.slnx
```

Os testes de integração (`tests/IntegrationTests`) usam `WebApplicationFactory` sem banco e não
exigem Docker.

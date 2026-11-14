# Configurar Postman para testar OrderHub

Guia rápido para importar a collection e environment do OrderHub no Postman.

---

## 📥 Importar Collection

1. **Abra o Postman**
2. Clique em **Import** (atalho: `Ctrl+K`)
3. Selecione a aba **Upload Files**
4. Escolha: `deploy/OrderHub-Auth-Tests.postman_collection.json`
5. Clique em **Import**

---

## 📋 Importar Environment

1. **Abra o Postman**
2. Clique no **ícone de engrenagem** (⚙️) no canto superior direito
3. Selecione **Environments** (ou pressione `Ctrl+,`)
4. Clique em **Import**
5. Escolha: `deploy/OrderHub-Dev.postman_environment.json`
6. Confirme

---

## ✅ Ativar Environment

1. Clique no **dropdown** no canto superior direito (onde diz "No Environment")
2. Selecione **OrderHub — Development**
3. Pronto! As variáveis estão disponíveis

---

## 🚀 Executar Testes

### Ordem recomendada

Execute os testes **em sequência** (de cima para baixo):

```
1️⃣ Keycloak Health
   ↓
2️⃣ Obter Access Token ← ⭐ Este salva o token automaticamente
   ↓
3️⃣ GET /products SEM token (401)
   ↓
4️⃣ GET /products COM token (200)
   ↓
5️⃣ Gateway Health
   ↓
6️⃣ Catalog Health
   ↓
7️⃣ Decodificar JWT (debug)
   ↓
8️⃣ Token expirado (execute após 5 minutos)
```

### Executar tudo de uma vez

Você pode rodar toda a collection automaticamente:

1. Clique com **botão direito** no nome da collection
2. Selecione **Run collection**
3. Na janela **Runner**, clique em **Run OrderHub — OAuth 2.0 Authentication Tests**

Postman executará todos os testes em sequência e mostrará relatório com ✅ (passou) ou ❌ (falhou).

---

## 🔑 Variáveis de Ambiente

Estas variáveis são **preenchidas automaticamente** após cada teste:

### Fixas (você não precisa alterar)

| Variável | Valor | Uso |
|---|---|---|
| `keycloak_base_url` | `http://localhost:8180` | URL do Keycloak |
| `gateway_base_url` | `http://localhost:5000` | URL do Gateway |
| `catalog_base_url` | `http://localhost:5001` | URL do Catalog |
| `client_id` | `orderhub-cli` | Client ID |
| `client_secret` | `orderhub-cli-dev-secret` | Client Secret (type: secret) |

### Dinâmicas (preenchidas pelos testes)

| Variável | Preenchida por | Uso |
|---|---|---|
| `{{access_token}}` | Teste #2 | Token JWT para headers |
| `{{token_type}}` | Teste #2 | Tipo de token (Bearer) |
| `{{expires_in}}` | Teste #2 | Tempo de expiração (300s) |
| `{{jwt_iss}}` | Teste #7 | Issuer do token (debug) |
| `{{jwt_aud}}` | Teste #7 | Audience do token (debug) |
| `{{jwt_sub}}` | Teste #7 | Subject do token (debug) |
| `{{jwt_scope}}` | Teste #7 | Scopes do token (debug) |

---

## 🧪 Analisar Resultados

### Visualizar resposta

Após executar cada requisição:

1. Clique na aba **Body** (ou **Pretty** se JSON)
2. Veja a resposta formatada

### Verificar status

- ✅ Badge verde = `2xx` (sucesso)
- ❌ Badge vermelho = `4xx` ou `5xx` (erro)

### Ver testes

Clique na aba **Tests** para ver quais validações foram executadas:

```javascript
pm.test('Token obtido com sucesso', function () {
    pm.expect(jsonData.access_token).to.exist;
});
```

---

## 🐛 Troubleshooting

### Erro: `Could not connect`

**Causa:** Gateway ou Keycloak não está rodando.

**Solução:**
```powershell
docker compose ps | Select-String "orderhub"
```

Se não estiver rodando:
```powershell
docker compose up -d
```

---

### Erro: `401 Unauthorized` no teste #4

**Causa:** Token expirou (300 segundos) ou issuer inválido.

**Solução:**
1. Execute o teste #2 novamente para gerar um novo token
2. Verifique se o ambiente **OrderHub — Development** está selecionado
3. Decodifique o token (teste #7) para verificar `iss` e `aud`

---

### Erro: `invalid_client` no teste #2

**Causa:** Client ID ou secret incorretos.

**Verificar:**
- Em Postman, abra o environment **OrderHub — Development**
- Confirme:
  - `client_id`: `orderhub-cli`
  - `client_secret`: `orderhub-cli-dev-secret`

---

## 📊 Matriz de Testes

| # | Nome | Método | URL | Headers | Esperado |
|---|---|---|---|---|---|
| 1 | Keycloak Health | GET | `/realms/orderhub/.well-known/...` | — | `200` |
| 2 | Obter Token | POST | `/realms/orderhub/protocol/openid-connect/token` | `Content-Type: form` | `200` + JWT |
| 3 | GET /products SEM token | GET | `/catalog/v1/products` | — | `401` |
| 4 | GET /products COM token | GET | `/catalog/v1/products` | `Authorization: Bearer {{token}}` | `200` |
| 5 | Gateway Health | GET | `/health/ready` | — | `200` |
| 6 | Catalog Health | GET | `/health/ready` | — | `200` |
| 7 | Decodificar JWT | — | — | Script JS | Claims |
| 8 | Token expirado | GET | `/catalog/v1/products` | `Authorization: Bearer {{token}}` | `401` (após 300s) |

---

## 🎯 Checklist

- [ ] Collection importada
- [ ] Environment importado
- [ ] Environment selecionado (dropdown canto superior)
- [ ] Teste #1 retorna `200` ✅
- [ ] Teste #2 retorna `200` com JWT ✅
- [ ] Teste #3 retorna `401` ✅
- [ ] Teste #4 retorna `200` com produtos ✅
- [ ] Teste #7 mostra claims corretos ✅

Se todos os itens passarem, **autenticação OAuth 2.0 está funcionando!** 🎉

---

## 💡 Dicas

### Ver variáveis

Coloque o cursor sobre `{{variável}}` em qualquer request para ver o valor atual.

### Debugar JavaScript

Se o teste falhar:
1. Clique na aba **Tests**
2. Veja o script que executou
3. Clique em **Console** (ícone de `>_` no canto inferior)
4. Veja logs e erros

### Usar cURL

Você pode exportar qualquer request como cURL:
1. Clique com botão direito no request
2. Selecione **Copy as cURL**
3. Cole no terminal PowerShell

---

## 📚 Links úteis

- [Documentação do Postman](https://learning.postman.com/)
- [Variáveis no Postman](https://learning.postman.com/docs/sending-requests/variables/)
- [Pre-request Scripts](https://learning.postman.com/docs/writing-scripts/pre-request-scripts/)
- [Tests](https://learning.postman.com/docs/writing-scripts/test-scripts/)

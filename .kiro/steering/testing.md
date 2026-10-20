---
inclusion: fileMatch
fileMatchPattern: ["tests/**/*.cs", "**/*Tests/**/*.cs", "tests/load/**", "tests/chaos/**"]
---
# Testes

- Framework: xUnit. Nome do teste: `Method_Scenario_ExpectedResult`. Estrutura Arrange/Act/Assert.
- **Unitários:** `Domain` e `Application`, sem I/O, sem Docker.
- **Integração:** `Api` e `Infrastructure` com Testcontainers (PostgreSQL, RabbitMQ). Nada de mocks de banco.
- **Arquitetura:** NetArchTest por serviço (regras de `clean-architecture.md`).
- **Contrato de eventos:** teste garantindo que mudanças em eventos `V1` só adicionam campos opcionais.
- **Idempotência:** todo consumer tem teste que processa a mesma mensagem duas vezes e verifica efeito único.
- **Carga:** scripts k6 em `tests/load/`, com cenário, meta de latência e resultado registrado em `docs/experimentos/`.
- **Caos:** scripts em `tests/chaos/` (derrubar container, atrasar rede, pausar broker), sempre com hipótese e resultado documentados.
- Testes não dependem de ordem nem de relógio real.

# ADR 0002: Keycloak como provedor OAuth 2.0/OIDC

- **Status:** Aceito
- **Data:** 2026-10-19

## Contexto
As APIs precisam de autenticação padronizada sem implementar um servidor de autorização.

## Decisão
Usar Keycloak em container. APIs são resource servers e validam JWT; serviço para serviço usa client_credentials.

## Consequências
Provedor portátil e open source; adiciona um componente a operar e a tornar altamente disponível.

## Conceito da disciplina relacionado
Segurança em sistemas distribuídos.

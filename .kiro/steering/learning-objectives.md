---
inclusion: always
---
# Objetivos de aprendizagem

O projeto exercita a disciplina de Sistemas Distribuídos. Objetivos: avaliar arquitetura física e lógica, analisar operação e desempenho, aplicar alta disponibilidade e balanceamento de carga, e usar VMs e ferramentas de gerenciamento.

## Regra para o agente

Toda spec e todo ADR devem declarar **qual conceito da disciplina é exercitado** e **qual evidência será produzida** (experimento reproduzível). Funcionalidade sem conceito associado deve ser questionada.

## Tópicos e onde são exercitados

- **No sistema:** fundamentos e arquitetura, processos/threads, modelos de computação, troca de mensagens, cliente-servidor, invocação remota (gRPC), suporte de SO (cgroups), consistência e replicação, segurança, tolerância a falhas, transações distribuídas, middleware, alta disponibilidade, balanceamento, VMs e gerenciamento.
- **Parcialmente no sistema:** comunicação em grupo (pub/sub), arquivos distribuídos (MinIO), sincronização (lock distribuído).
- **Em `labs/`:** peer-to-peer (gossip), comunicação em grupo com ordenação, IPC com sockets TCP/UDP, sincronização (Lamport, vetorial, eleição, Cristian/Berkeley), mini-DFS, grade computacional, mini-RPC.

Mapa completo: `docs/mapa-disciplina.md`. Roteiro de fases: `docs/roadmap.md`.

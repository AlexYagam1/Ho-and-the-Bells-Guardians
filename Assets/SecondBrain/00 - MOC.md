# MOC do projeto

Índice principal do second brain de **Ho and the Bells Guardians**. As notas descrevem o estado confirmado do repositório e apontam lacunas sem presumir funcionalidades.

## Visão geral

- [[01 - Visão geral do projeto]] — identidade do projeto e estado atual.
- [[04 - Base técnica]] — versão do Unity, renderização, pacotes e organização.
- [[05 - Skills de desenvolvimento]] — skills de agente disponíveis e compatibilidade.

## Conteúdo do jogo

- [[02 - Cena atual]] — cena configurada para abrir e incluir nos builds.
- [[03 - Input e controles]] — mapas, ações e bindings configurados.
- [[06 - Player e câmera]] — player em primeira pessoa, Cinemachine, visual e validação.
- [[07 - Escalada e estamina]] — quinas, pulo duplo, paredes escaláveis e aura de capacidade/cansaço.

## Grafo

```mermaid
graph TD
    MOC[ MOC do projeto ] --> P[ Visão geral do projeto ]
    MOC --> TEC[ Base técnica ]
    MOC --> SKILLS[ Skills de desenvolvimento ]
    MOC --> CENA[ Cena atual ]
    MOC --> INPUT[ Input e controles ]
    MOC --> PLAYER[ Player e câmera ]
    MOC --> CLIMB[ Escalada e estamina ]
    CLIMB --> PLAYER
    CLIMB --> INPUT
    CLIMB --> CENA
    PLAYER --> CENA
    PLAYER --> INPUT
    PLAYER --> TEC
    P --> CENA
    P --> TEC
    SKILLS --> TEC
    SKILLS --> INPUT
    CENA --> TEC
    INPUT --> TEC
    INPUT --> CENA
```

## Como manter

Consulte esta página como ponto de entrada. Ao acrescentar uma nota, ligue-a aqui e a pelo menos uma nota relacionada. Veja também as instruções em [[../../AGENTS|AGENTS.md]].

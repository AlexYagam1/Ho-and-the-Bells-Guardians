# Base técnica

## Confirmado nos arquivos do projeto

- Unity Editor: **6000.6.5f1** (`ProjectSettings/ProjectVersion.txt`).
- Renderização: Universal Render Pipeline (URP), pacote `com.unity.render-pipelines.universal` na versão **17.6.0**.
- Input System: `com.unity.inputsystem` **1.20.0**; asset de ações em [[03 - Input e controles]].
- Unity Pipeline: `com.unity.pipeline` **1.0.0-pre.1**, instalado para permitir controle do Editor aberto via Unity CLI/MCP.
- Unity CLI atualizado para **1.0.0**; conexão local ao Editor via Pipeline confirmada.
- Cinemachine efetivamente resolvido pelo Unity: **6.6.0**, origem `BuiltIn`, com APIs `Unity.Cinemachine` (`CinemachineCamera`/`CinemachineBrain`). A solicitação inicial de 3.1.7 foi resolvida pelo Editor para 6.6.0; manifesto e lock foram alinhados à versão efetiva. Não há downgrade do Editor.
- Outros pacotes declarados incluem AI Navigation 2.0.15, Timeline 6.6.0, uGUI 2.6.0 e Visual Scripting 1.9.12.
- Assets incluem `Scenes`, `Settings`, `SecondBrain`, `Scripts/Player`, `Prefabs/Player`, `Materials/AdventureDemo` e ferramentas em `Editor`.
- HDR ativo no URP usado pela cena; o perfil vibrante usa Neutral, saturação +15, contraste +10 e bloom zero. Ver [[06 - Player e câmera]].
- A cena listada para build é [[02 - Cena atual]].
- Skills e roteador de desenvolvimento de jogos instalados por projeto: [[05 - Skills de desenvolvimento]].

## Notas de manutenção

Verifique `Packages/manifest.json` e `ProjectSettings/ProjectVersion.txt` antes de atualizar estas versões. Não documente um pacote como usado em gameplay só porque está instalado.

O servidor Pipeline só inicia depois que o Editor importa o pacote; se adicionado enquanto o Editor está aberto, pode ser necessário alternar para a janela do Unity e aguardar a importação.

O catálogo de skills Unity instalado declara baseline Unity 6.3 LTS (6000.3), enquanto este projeto registra Unity 6000.6.5f1. Trate as skills como orientação e confirme APIs e comportamentos dependentes da versão para o Editor do projeto.

## Relações

- Contexto do jogo: [[01 - Visão geral do projeto]].
- Cena e renderização: [[02 - Cena atual]].
- Input: [[03 - Input e controles]].
- Skills de agente: [[05 - Skills de desenvolvimento]].
- Índice: [[00 - MOC]].

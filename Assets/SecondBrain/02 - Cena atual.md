# Cena atual

## Configuração

- `Assets/Scenes/SampleScene.unity` é a única cena encontrada em `Assets/Scenes`.
- Ela está habilitada em `ProjectSettings/EditorBuildSettings.asset`, portanto é a cena atualmente listada para o build.
- Os objetos nomeados encontrados na cena são `Main Camera`, `Directional Light`, `Global Volume` e `MCP Test Cube`. Também há objetos sem nome no YAML serializado.
- Um cubo de teste chamado `MCP Test Cube` foi inserido via Unity CLI e salvo em `SampleScene.unity`; ele valida a conexão do Pipeline e não representa conteúdo de gameplay confirmado.
- A cena contém configurações de iluminação e navegação; não há evidência nesta nota de uma área de jogo ou de lógica de gameplay.

## Relações

- O projeto e seu estado: [[01 - Visão geral do projeto]].
- Ações de input disponíveis, sem lógica de jogo associada confirmada: [[03 - Input e controles]].
- Configurações de renderização: [[04 - Base técnica]].
- Índice: [[00 - MOC]].

## Próximas informações a registrar

Quando cenas de jogo forem criadas, documente sua finalidade, objetos principais, dependências entre cenas e como testá-las. Atualize também a lista de cenas habilitadas no build quando ela mudar.

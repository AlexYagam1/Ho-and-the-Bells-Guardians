# Cena atual

## Configuração

- `Assets/Scenes/SampleScene.unity` é a única cena encontrada em `Assets/Scenes`.
- Ela está habilitada em `ProjectSettings/EditorBuildSettings.asset`, portanto é a cena atualmente listada para o build.
- Os objetos principais são `Main Camera`, `Directional Light`, `Global Volume`, `MCP Test Cube`, `Adventure Player` e `Adventure Test Area`.
- Um cubo de teste chamado `MCP Test Cube` foi inserido via Unity CLI e salvo em `SampleScene.unity`; ele valida a conexão do Pipeline e não representa conteúdo de gameplay confirmado.
- `Adventure Player` instancia `Assets/Prefabs/Player/AdventurePlayer.prefab` em (0, 0.05, -7), olhando para +Z.
- A área de teste tem chão de 24 x 24 m, paredes, cinco degraus de 0.25 m, rampa de 15 graus, patamar e teto baixo para testar salto e colisão. O cubo original foi preservado.
- A câmera principal usa CinemachineBrain em LateUpdate e pós-processamento URP/SMAA High. O Global Volume referencia `Assets/Settings/AdventureVibrantProfile.asset`; o perfil original foi preservado.
- Luz direcional quente, sombras suaves e iluminação ambiente Trilight dão suporte ao visual vibrante. Os materiais da demonstração são URP/Lit, não shaders toon.
- Abra a cena e entre em Play Mode; clique na Game View para capturar o cursor. Ver controles e validação em [[06 - Player e câmera]].
- `Traversal Test Area` acrescenta `Climbable Wall` azul em (8, 2.25, -3), bloco de quina dourado em (3, 0.5, -3), parede fora do limite angular, borda estreita e borda com teto bloqueado. O player escala faces sólidas estáticas quase verticais sem exigir o marcador `ClimbableSurface`; os demais objetos demonstram restrições geométricas de acesso. Ver [[07 - Escalada e estamina]].

## Relações

- O projeto e seu estado: [[01 - Visão geral do projeto]].
- Ações de input disponíveis, sem lógica de jogo associada confirmada: [[03 - Input e controles]].
- Configurações de renderização: [[04 - Base técnica]].
- Índice: [[00 - MOC]].

## Próximas informações a registrar

Quando cenas de jogo forem criadas, documente sua finalidade, objetos principais, dependências entre cenas e como testá-las. Atualize também a lista de cenas habilitadas no build quando ela mudar.

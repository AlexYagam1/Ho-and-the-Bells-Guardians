# Cena atual

## Configuração

- `Assets/Scenes/SampleScene.unity` é a única cena encontrada em `Assets/Scenes`.
- Ela está habilitada em `ProjectSettings/EditorBuildSettings.asset`, portanto é a cena atualmente listada para o build.
- Os objetos principais são `Main Camera`, `Directional Light`, `Global Volume`, `MCP Test Cube`, `Adventure Player` e `Adventure Test Area`.
- Um cubo de teste chamado `MCP Test Cube` foi inserido via Unity CLI e salvo em `SampleScene.unity`; ele valida a conexão do Pipeline e não representa conteúdo de gameplay confirmado.
- `Adventure Player` é a única instância jogável na cena. Instancia `Assets/Prefabs/Player/AdventurePlayer.prefab` em (0, 0.05, -7), olhando para +Z. Ver a validação de spawn e câmera em [[06 - Player e câmera]].
- A área de teste tem chão de 24 x 24 m, paredes, cinco degraus de 0.25 m, rampa de 15 graus, patamar e teto baixo para testar salto e colisão. O cubo original foi preservado.
- A câmera principal usa CinemachineBrain em LateUpdate e pós-processamento URP/SMAA High. O Global Volume referencia `Assets/Settings/AdventureVibrantProfile.asset`; o perfil original foi preservado.
- Luz direcional quente, sombras suaves e iluminação ambiente Trilight dão suporte ao visual vibrante. Os materiais da demonstração são URP/Lit, não shaders toon.
- Abra a cena e entre em Play Mode; clique na Game View para capturar o cursor. Ver controles e validação em [[06 - Player e câmera]].
- `Traversal Test Area` acrescenta `Climbable Wall` azul em (8, 2.25, -3), bloco de quina dourado em (3, 0.5, -3), parede fora do limite angular, borda estreita e borda com teto bloqueado. O player escala faces sólidas estáticas quase verticais sem exigir o marcador `ClimbableSurface`; os demais objetos demonstram restrições geométricas de acesso. Ver [[07 - Escalada e estamina]].

## Distritos de teste

Na Game View, capture o cursor, siga para o norte do pátio inicial (+Z) e passe pela abertura central de 4 m no muro. A partir da entrada, as quatro placas organizam os percursos da esquerda para a direita.

| Distrito | Percurso |
|---|---|
| Escalada | Parede vertical com patamar e peça com frente a 80°, patamar e rampa de 20° atrás. Pule mantendo W voltado à face íngreme ou suba direto com W. Caminhe pela rampa pelo lado norte. |
| Parkour | Subida de borda em um bloco de 0,9 m, seguida por dois saltos curtos sobre chão de recuperação. |
| Escadas | Seis espelhos de 0,25 m e patamar para subir e descer andando. |
| Pulo duplo | Pistas niveladas com vãos de 2 m e 3,5 m. Use Espaço no vão curto e pressione Espaço novamente no ar no longo. A rampa de entrada dá acesso ao início de cada pista. |

As duas peças de escalada são editáveis no ProBuilder. Cada peça usa um único MeshCollider estático. Para reproduções e resultados em Play Mode, veja [[07 - Escalada e estamina]].

## Relações

- O projeto e seu estado: [[01 - Visão geral do projeto]].
- Ações de input disponíveis, sem lógica de jogo associada confirmada: [[03 - Input e controles]].
- Configurações de renderização: [[04 - Base técnica]].
- Índice: [[00 - MOC]].
- `Cat NPC Demo` adiciona quatro gatos com mochilas, passeio autônomo e visitas às duas escadas. Configuração, testes e limitações em [[08 - Gatos NPCs]].

## Próximas informações a registrar

Quando cenas de jogo forem criadas, documente sua finalidade, objetos principais, dependências entre cenas e como testá-las. Atualize também a lista de cenas habilitadas no build quando ela mudar.

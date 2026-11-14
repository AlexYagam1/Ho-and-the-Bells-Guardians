# Player e câmera

## Estado confirmado — 13/11/2026

Base jogável desktop de primeira pessoa em `Assets/Prefabs/Player/AdventurePlayer.prefab`, integrada à [[02 - Cena atual]]. Input direto pelo novo Input System, sem chamadas ao input legado. Ver [[03 - Input e controles]] e [[04 - Base técnica]].

## Arquitetura e parâmetros

- `AdventurePlayerInput` lê dispositivos em Update (ordem -200), normaliza diagonais e gerencia cursor/foco. `HasControl`, `Move`, `Look`, `Sprint` e `JumpPressed` expõem intenção; `SetControlEnabled(bool)` permite bloqueio externo.
- `AdventurePlayerLook` (ordem -150) aplica yaw na raiz e pitch no `Eye Target`. Sensibilidade 0.12 grau/pixel, pitch limitado a ±80 graus. Delta do mouse não é multiplicado por deltaTime.
- `AdventurePlayerMotor` (ordem -100) usa `CharacterController.Move`, caminhada 4 m/s, corrida 6.5 m/s, salto 1.2 m, gravidade 20 m/s² e queda limitada a 50 m/s. `Simulate(Vector2, bool, bool, float)` separa intenção da origem do input; `SetControlEnabled(bool)` delega o bloqueio ao input.
- CharacterController: altura 1.8 m, raio 0.3 m, centro Y 0.9, stepOffset 0.3, slopeLimit 45 graus e skinWidth 0.03. Sem Rigidbody.
- `Eye Target` em Y 1.65 e `Body Target` em Y 0.9. O segundo é um ponto de extensão; não há terceira pessoa jogável.
- `First Person Cinemachine` usa CinemachineCamera, HardLockToTarget e RotateWithFollowTarget, sem damping. Brain na câmera de saída atualiza em LateUpdate; FOV 70, near clip 0.05 e far clip 300.
- Prefab não inclui a câmera de saída: requer uma câmera com CinemachineBrain na cena. Não há corpo visível nem animações.

## Visual

`Assets/Settings/AdventureVibrantProfile.asset`: Neutral, saturação +15, contraste +10 e exposição 0. Bloom, motion blur, profundidade de campo, aberração cromática, granulação e vinheta explicitamente desligados. Câmera com pós-processamento ativo, máscara Default e SMAA High; HDR ativo. Perfil original preservado.

Direção artística definida pelo autor: cartoon vibrante, inspirado em BOTW, sem bloom. O perfil e os materiais da demonstração são uma base; cel shading, contornos e arte final não foram implementados.

## Ferramentas e reprodução

- `Assets/Editor/AdventurePlayerSetup.cs`: constrói e salva prefab, área e perfil pelo Editor. Menu `Tools/Ho and the Bells/Create Adventure Player Demo`. Recusa executar se a demonstração já existir, evitando duplicação.
- `Assets/Editor/AdventurePlayerValidation.cs`: validação no Editor em Play Mode. `Run()` instancia uma cópia temporária, usa a geometria da demonstração e a remove ao terminar.
- `RunInput()` usa dispositivos virtuais e restaura os dispositivos anteriores no finally. Exige Play Mode com Game View em foco e estado de foco do Input System atualizado. Para execução por CLI, focar Game View e chamar `InputSystem.Update()` antes do teste na mesma avaliação.
- `Tools/Validate-UnityAssets.ps1`: alternativa local de checagem de GUIDs, metas e referências de scripts, pois o comando Python da skill não está disponível no PATH. Não substitui a validação de cena no Editor.

## Validação realizada

- Compilação dos scripts sem erros; corrigida a chamada de busca obsoleta identificada durante os testes.
- Testes com passos de simulação de 1/30, 1/60 e 1/120 segundo: caminhada, corrida, diagonal normalizada, altura do salto, rejeição de salto aéreo, aterrissagem e sensibilidade por deslocamento do mouse passaram. São testes de timestep, não benchmark de FPS renderizado.
- Paredes, teto baixo, degraus, rampa, queda do patamar e limites de pitch passaram.
- Teclado/mouse virtuais: WASD, sprint, salto por borda (sem repetição ao segurar), Escape, recaptura sem salto de visão, perda de foco, bloqueio externo e ausência de dispositivos passaram. Os dispositivos foram restaurados ao terminar.
- Play Mode avançando confirmado por frameCount e playerLoopTicking. Câmera alinhada ao Eye Target com erro de posição/rotação zero na amostra; um Camera e um AudioListener.
- Captura em `Screenshots/adventure-player.png` inspecionada: área visível, cores vibrantes e sem bloom. O Console não apresentou erros de gameplay.
- Cena reaberta do disco: prefab conectado, alvos atribuídos e nenhum MonoBehaviour ausente.
- `Tools/Validate-UnityAssets.ps1 -ChangedOnly`: 13 assets novos/alterados verificados, sem problemas de metas, GUIDs duplicados ou referências de script; 37 GUIDs próprios inventariados.
- A checagem completa de arquivos encontrou quatro GUIDs de scripts não resolvidos no `DefaultVolumeProfile.asset` antigo: `60f3b30c03e6ba64d9a27dc9dba8f28d`, `0fd9ee276a1023e439cf7a9c393195fa`, `5a00a63fdd6bd2a45ab1f2d869305ffd` e `74955a4b0b4243bc87231e8b59ed9140`. Todos já constam no HEAD anterior à tarefa. O asset original foi preservado; essa dívida preexistente permanece pendente. A cena e o novo perfil não têm essas referências.
- Avaliação subjetiva prolongada de conforto e desempenho em hardware-alvo permanece pendente; não foi medido jitter sob carga ou FPS sustentado de 120.

## Pendente

Terceira pessoa, transições, puzzles, interação, inventário, gamepad, remapeamento, personagem animado e shader toon. A estrutura atual permite evolução, mas não implementa esses sistemas.

## Relações

- [[00 - MOC]]
- [[01 - Visão geral do projeto]]
- [[02 - Cena atual]]
- [[03 - Input e controles]]
- [[04 - Base técnica]]

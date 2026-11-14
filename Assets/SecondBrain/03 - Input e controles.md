# Input e controles

## Asset configurado

`Assets/InputSystem_Actions.inputactions` contém dois mapas: `Player` e `UI`. O arquivo está associado como asset global de ações nas configurações de build do Editor.

### Mapa `Player`

Ações configuradas: `Move`, `Look`, `Attack`, `Interact`, `Crouch`, `Jump`, `Previous`, `Next` e `Sprint`.

Bindings observados incluem teclado e mouse, gamepad, joystick, touchscreen e controles XR. Exemplos: WASD/setas e stick esquerdo para movimento; mouse delta e stick direito para olhar; espaço e botão sul do gamepad para pular; Shift esquerdo e pressionar o stick esquerdo para sprint. `Interact` usa interação `Hold`.

Os nomes descrevem a configuração de entrada; não confirmam que essas ações já sejam consumidas por código ou que estejam implementadas no jogo.

### Mapa `UI`

Inclui `Navigate`, `Submit`, `Cancel`, `Point`, `Click`, `RightClick`, `MiddleClick`, `ScrollWheel`, `TrackedDevicePosition` e `TrackedDeviceOrientation`.

## Controles implementados do player

- `AdventurePlayerInput` lê `Keyboard.current` e `Mouse.current` diretamente. O asset de ações acima continua preservado, mas não dirige o player atual.
- WASD: andar; Shift esquerdo: correr; Espaço: pular somente no chão; movimento do mouse: olhar.
- Escape libera o cursor e limpa os comandos. Clique esquerdo na Game View recaptura, descartando o delta do mouse desse frame.
- Perda de foco ou ausência de teclado/mouse libera o cursor e limpa o input. O retorno do foco exige clique para recapturar.
- `SetControlEnabled(false)` bloqueia comandos para futuros menus/puzzles; clicar não contorna esse bloqueio. `true` tenta recapturar se houver foco e dispositivos.
- Gravidade continua durante bloqueio de comandos; não é um sistema global de pausa.
- Gamepad, remapeamento, interação, agachamento e alternância de câmera ainda não estão implementados.
- Implementação e parâmetros: [[06 - Player e câmera]].

## Relações

- Projeto e lacunas de implementação: [[01 - Visão geral do projeto]].
- Cena atualmente configurada: [[02 - Cena atual]].
- Pacote Input System: [[04 - Base técnica]].
- Índice: [[00 - MOC]].

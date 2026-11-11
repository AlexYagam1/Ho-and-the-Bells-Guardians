# Input e controles

## Asset configurado

`Assets/InputSystem_Actions.inputactions` contém dois mapas: `Player` e `UI`. O arquivo está associado como asset global de ações nas configurações de build do Editor.

### Mapa `Player`

Ações configuradas: `Move`, `Look`, `Attack`, `Interact`, `Crouch`, `Jump`, `Previous`, `Next` e `Sprint`.

Bindings observados incluem teclado e mouse, gamepad, joystick, touchscreen e controles XR. Exemplos: WASD/setas e stick esquerdo para movimento; mouse delta e stick direito para olhar; espaço e botão sul do gamepad para pular; Shift esquerdo e pressionar o stick esquerdo para sprint. `Interact` usa interação `Hold`.

Os nomes descrevem a configuração de entrada; não confirmam que essas ações já sejam consumidas por código ou que estejam implementadas no jogo.

### Mapa `UI`

Inclui `Navigate`, `Submit`, `Cancel`, `Point`, `Click`, `RightClick`, `MiddleClick`, `ScrollWheel`, `TrackedDevicePosition` e `TrackedDeviceOrientation`.

## Relações

- Projeto e lacunas de implementação: [[01 - Visão geral do projeto]].
- Cena atualmente configurada: [[02 - Cena atual]].
- Pacote Input System: [[04 - Base técnica]].
- Índice: [[00 - MOC]].

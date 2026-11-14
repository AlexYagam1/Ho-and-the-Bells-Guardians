# Escalada e estamina

## Estado confirmado — 14/11/2026

Implementado no prefab `AdventurePlayer` e na `SampleScene` via Unity CLI/Pipeline. Preservadas as medidas ajustadas pelo autor: CharacterController com altura 1.42, raio 0.3, centro Y 0.9 e Eye Target Y 1.32. As sondagens calculam os pés pela cápsula, não pelo Transform.

## Estados e controles

- `AdventurePlayerMotor.MovementState`: Locomotion, Climbing e Mantling. O motor é o único responsável pelo deslocamento com CharacterController.Move; expõe `State`, `StateChanged`, `IsGrounded`, `AirJumpAvailable` e `ExhaustionLocked`.
- `Simulate(move, sprint, jumpPressed, deltaTime, dropPressed = false, controlEnabled = true)` mantém a assinatura antiga compatível por parâmetros opcionais. Input runtime usa `Keyboard.current`/`Mouse.current` e Ctrl esquerdo para soltar.
- Qualquer collider sólido estático em `solidMask` pode ser escalado sem `ClimbableSurface`, se sua face estiver até 15 graus da vertical e voltada ao player. Paredes com Rigidbody e tetos não são aceitos. Objetos animados por Transform não são suportados nesta entrega. `ClimbableSurface` continua disponível, mas não é requisito do player.
- W contra uma face elegível inicia escalada. W/S sobe/desce a 1.2/1.5 m/s; A/D move a 1 m/s. Direção diagonal normalizada. Movimento usa normal da parede/eixo vertical, independentemente do olhar. Sem input direcional, mantém contato.
- Ctrl solta; Espaço custa 15 para saltar para fora (impulso horizontal 3 m/s amortecido em 0.2 s e impulso vertical do salto normal). Sem saldo, apenas solta. O salto aéreo disponível é preservado.
- Após soltar ou saltar, exige liberação de W e cooldown de 0.25 s. Perder contato causa queda; mudança de normal acima de 30 graus cancela em vez de contornar quinas de 90 graus.
- Escape, perda de foco e bloqueio externo cancelam escalada/mantle e limpam feedback; gravidade continua. `timeScale = 0` suspende simulação, consumo e progressão visual.

## Subida de quina e pulo duplo

- `PlayerTraversalProbe` detecta parede, topo e apoio sob quatro pontos da cápsula. Funciona em sólidos mesmo sem marcador.
- Alcance até dois raios a partir do eixo da cápsula, acrescido de skin; altura de topo até 85% da altura efetiva. Degraus menores ficam com stepOffset.
- Exige topo dentro do slopeLimit, apoio suficiente, cápsula livre no destino e percurso livre. Usa queries com triggers ignorados e filtra o próprio player.
- Segurar W perto de uma borda válida inicia Mantle sem custo de estamina. Se o último passo pago zerar a reserva, o motor ainda verifica e inicia o Mantle se o topo já for alcançável; sem topo válido, desprende e cai. Não permite continuar escalando normalmente sem reserva.
- Mantle leva 0.3 s: eleva e depois avança, com easing, verificando cada trecho. Bloqueio cancela; ao concluir, assenta a cápsula no topo e recupera o salto extra. Uma transição iniciada termina mesmo se a reserva se esgotar.
- Um salto no chão e um extra no ar; sair andando de uma borda também deixa um salto aéreo disponível. O salto extra redefine velocidade vertical para altura 1.2 m, sem acumular velocidade. Só aterrissagem ou fim de mantle recarregam; contato com parede não recarrega.

## Reserva e API para itens futuros

`PlayerStamina` expõe Current, Maximum, Normalized, evento Changed(current, maximum) e Exhausted. Valores iniciais:

| Comportamento | Custo/recuperação |
|---|---|
| Capacidade | 100 |
| Subir, inclusive diagonal ascendente | 25/s |
| Segurar, mover lateralmente, descer | 10/s |
| Salto da parede | 15 |
| Recuperar no chão | 40/s após 0.3 s |

- Não recupera no ar/agarrado. Corrida e saltos comuns não gastam. Ao zerar, cai no mesmo passo e bloqueia reengate até o chão.
- `SetMaximumStamina(float)`: exige valor positivo finito; aumentar não repõe reserva; reduzir limita Current ao novo máximo.
- `Restore(float)`, `Consume(float)` e `TrySpend(float)` aceitam quantidades finitas não negativas. Restore é explícito; TrySpend não consome saldo insuficiente.
- Capacidade pode ultrapassar 200. Não há sistema de itens implementado.

## Aura — sem barra

- `PlayerStaminaFeedback` controla `StaminaAuraGraphic` (uGUI) e CinemachineBasicMultiChannelPerlin. O Canvas está no prefab, em Screen Space Overlay; sem GraphicRaycaster e sem interceptar cliques. Perfil URP original preservado, sem bloom.
- Cobertura acompanha Current/200, limitada a 1: 100 cobre borda inferior e laterais até metade; 150 até três quartos; 200+ fecha todo o contorno. Mudanças de reserva interpolam em 0.25 s. O limite visual 200 é configurável e não limita a reserva.
- Com reserva incompleta, a cobertura mínima é 4% e fica ancorada ao rodapé, inclusive quando Current chega a zero; a aura cresce para cima durante a recuperação. O centro é transparente; término superior e bordas têm queda suave.
- Cor por Current absoluto: 200+ branco; 150 e 100 verde; 50 laranja; 15 ou menos vermelho; interpolação entre esses pontos. 250 continua branco até baixar de 200, independentemente da capacidade.
- Entrada ao agarrar em 0.15 s; opacidade inicial 0.42. A aura permanece enquanto a reserva for menor que Maximum, inclusive durante atraso e recuperação no chão ou durante queda; ao encher, dissipa em 0.2 s. Abaixo de 50, tremor aumenta com o quadrado da fadiga enquanto escala e dissipa em 0.2 s ao soltar. Abaixo/igual a 15, pulsação de opacidade a 1.5 Hz (intensidade 0.55).
- Bloqueio de controle limpa imediatamente a aura e o tremor.
- `StaminaCameraNoise.asset` tem somente canais rotacionais, amplitudes X/Y/Z de 0.4/0.4/0.2; o gerador usa metade dessas amplitudes, limitando a magnitude combinada a 0.3 grau com ganho 1. Não desloca corpo/collider nem o Eye Target. Intensidades de tremor e pulsação podem ser zeradas no Inspector.

## Ferramentas e validação anterior — 13/11/2026

- `AdventureTraversalSetup.Configure()` atualiza o prefab preservando dimensões e cria `Traversal Test Area` apenas se ausente. Menu: Tools/Ho and the Bells/Configure Climbing and Stamina.
- `AdventureTraversalValidation.Run()` cria fixtures temporárias em Play Mode, executa grupos de validação e remove os objetos ao terminar. Verifica movimento/pulo duplo e consumo a 30/60/120 Hz, salto da parede, bloqueio por esgotamento, mantle válido/alto/estreito/bloqueado e com estamina esgotada, parede sem marcador escalável, aura durante recuperação, pulsação, cancelamento e capacidades 100/150/200/250.
- Menu `Tools/Ho and the Bells/Run Player and Traversal Validation` executa também `AdventurePlayerValidation.Run()` e imprime os dois relatórios no Console.
- A suíte anterior `AdventurePlayerValidation.Run()` foi adaptada ao pulo duplo e às dimensões atuais; passou em caminhada, corrida, diagonais, colisões, teto, degraus, rampa, queda e mouse-look. Rotas ajustadas para não cruzar novos obstáculos.
- `RunInput()` passou com teclado/mouse virtuais, Escape, foco, recaptura, bloqueio externo e dispositivos ausentes.
- Tremor amostrado em 60 frames: correção angular entre 0.0508 e 0.1133 graus, com deslocamento do corpo igual a zero.
- Prévias runtime por `PrepareAuraPreview(maximum, reserve)`, restauradas ao sair do Play Mode. Capturas inspecionadas: `Screenshots/stamina-green-100.png` (16:9), `stamina-orange-150.png` (16:10), `stamina-red-200.png` (ultrawide) e `stamina-white-250.png` (16:9). Centro transparente; cobertura agora acompanha a reserva e a cor permanece por limites absolutos.
- Verificação final: cena reaberta sem alterações pendentes, dimensões preservadas, um componente de cada sistema, uma câmera e zero scripts ausentes. Console atual sem erros/avisos. `Validate-UnityAssets.ps1 -ChangedOnly` passou nos 14 assets novos/alterados, com 46 GUIDs próprios inventariados e nenhuma referência inválida introduzida.
- Não é benchmark de FPS nem avaliação subjetiva prolongada de conforto. Superfícies móveis, animações de patas, terceira pessoa e itens continuam pendentes.

## Validação desta alteração — 14/11/2026

- Unity **6000.6.5f1**, na `SampleScene`, em Play Mode: os 23 cenários de `AdventurePlayerValidation.Run()` e os 11 grupos de `AdventureTraversalValidation.Run()` passaram, incluindo parede estática sem marcador, mantle ao esgotar estamina, faixa inferior em zero e persistência da aura durante a recuperação.
- Console sem erros ou avisos após a execução. Play Mode encerrado e a cena voltou ao estado de edição.
- `git diff --check` passou.

## Relações

- [[00 - MOC]]
- [[01 - Visão geral do projeto]]
- [[02 - Cena atual]]
- [[03 - Input e controles]]
- [[04 - Base técnica]]
- [[06 - Player e câmera]]

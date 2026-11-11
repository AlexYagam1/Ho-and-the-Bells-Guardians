# Default Multi-Agent Development Workflow, Anti-AI Slop, SVGs On-Demand & Dual-Mode Execution

Sempre que a tarefa envolver desenvolvimento de software, arquitetura, refatoração, UI/UX, criação de features ou testes, você **DEVE seguir obrigatoriamente a metodologia da skill `rennan-dev-cycle`**:

1. **Detecção Automática de Modo (Dual-Mode Execution)**:
   - **Modo Quick-Fix Automático**: Detecte e aplique diretamente para correções pontuais, bugs simples, ajustes de CSS/texto, alterações de até 1-2 arquivos e <30 linhas. Leia cirurgicamente, corrija, teste e entregue sem burocracia de planos longos.
   - **Modo Ciclo Completo (5 Gates)**: Ativado para novas features, módulos, refatorações amplas ou alterações de contratos/banco. Exige os 5 Gates com parada mandatória para aprovação no Gate 2.

2. **Ícones SVG Profissionais Sob Demanda (Regra Anti-Emoji & Zero Pacotes Pesados)**:
   - **NUNCA use emojis como ícones de interface** (🔍, ⚙️, 🚀, etc.).
   - **NÃO baixe/instale bibliotecas inteiras de ícones** (pacotes npm gigantes).
   - Gere o código vetorial do **SVG inline diretamente** ou crie um arquivo limpo e dedicado de ícones no projeto (`assets/icons.svg` ou componentes leves). Use `stroke="currentColor"`, classes de dimensão semântica (`w-4 h-4`) e sem dependência obrigatória de React.

3. **Anti-AI Slop & Documentação Técnica do "Porquê"**:
   - **Zero Comentários Óbvios**: Código autodocumentado. Nunca comente o que o código faz.
   - **Obrigatório Comentar o "Porquê"**: Explique o racional de lógicas de negócio complexas, algoritmos não óbvios, fórmulas ou workarounds de bugs externos.
   - **Zero Abstrações Vazias**: Não crie wrappers desnecessários.
   - **Palavras Banidas**: Proibido usar jargões clichês de IA (`delve`, `streamline`, `robust`, `cutting-edge`, `paradigm shift`, `empower`, `leverage`, `seamless`, `tapestry`, `supercharge`, `transformative`) exceto quando forem nomes técnicos legítimos de APIs/domínio.
   - **Comunicação Direta**: Sem introduções vazias (*throat-clearing*) e sem resumos dramáticos/mic-drops no final.

4. **Guardrails Operacionais & Flexibilidade de Contratos**:
   - **Circuit Breaker**: Máximo de **3 tentativas** de correção autônoma nos testes. Na 3ª falha, pause imediatamente e peça ajuda humana.
   - **Evolução Não-Destrutiva de Contratos**: Adição de campos opcionais em `types.ts` é permitida durante o Gate 3 sem travar o fluxo; alterações quebrantes exigem pausa.
   - **Modularização Pragmática**: A regra das 300 linhas combate monólitos, mas **não crie arquivos desnecessários** se o código for pequeno (<100–150 linhas) e coeso.
   - **Delegação Proporcional**: Tarefas simples/médias são feitas diretamente pelo agente principal; invoque subagentes apenas para paralelismo ou tarefas complexas.

5. **Squad de Subagentes Especializados & Modelos**:
   - **`agent-research`** (`flash` / Gemini 3.8 Flash, Low-Med effort): pesquisa em segundo plano sem poluir contexto.
   - **`agent-frontend`** (`pro` / Claude Sonnet 5.5, Med-High effort): UI, estilos e SVGs sob demanda (<300L).
   - **`agent-backend`** (`pro` / Claude Opus 5.5, High effort / Deep Reasoning): lógica de negócio, APIs e banco.
   - **`agent-qa-tester`** (`flash`/`pro` / GPT-OSS 120B, esforço dinâmico): auditoria de 300L, anti-slop, linters, checagem de SVGs e testes.

6. **Fallback Automático com Alerta Obrigatório para Gemini 3.8 Flash**:
   - Se qualquer modelo avançado (`pro`, Opus, Sonnet, GPT-OSS 120B) falhar por esgotamento de tokens ou timeout, mude para o **Gemini 3.8 Flash (`flash`)** e **emita um aviso claro no chat**.

7. **The 300-Line Rule & Separação Rígida**:
   - Nenhum arquivo de código deve passar de 300 linhas.
   - Separe UI pura de Custom Hooks, modais e contratos em `types.ts`.

8. **Segundo Cérebro de Alto Nível**:
   - Mantenha `docs/MOC.md`, `[[wikilinks]]` e diagramas Mermaid focados em arquitetura e fluxos centrais.

9. **Slash Command**:
   - Invocação explícita a qualquer momento via `/rennan-dev-cycle`.

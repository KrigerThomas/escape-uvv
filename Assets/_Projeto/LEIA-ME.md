# Escape UVV — guia do projeto para o grupo

Unity **6000.0.8f1** (a mesma do laboratório 12), template URP 3D.
Tudo o que é nosso fica em `Assets/_Projeto`. As outras pastas de `Assets` são packs baixados (não edite dentro deles).

## Como abrir e jogar
1. Abra a cena `Assets/_Projeto/Scenes/Nivel1`.
2. Aperte Play.
3. Comandos: WASD andar, Shift correr, Espaço pular, mouse gira a câmera, Esc solta o mouse.
4. Atalhos de teste: **R** reinicia o nível, **N** pula para o próximo.

## Pastas
| Pasta | O que tem |
|---|---|
| `Scenes` | Uma cena por nível (`Nivel1`, `Nivel2`...). `_Backup` guarda versões antigas. |
| `Scripts` | Todos os scripts C#. Um arquivo por classe, todos comentados. |
| `Prefabs/Base` | Peças usadas em todos os níveis: Jogador, câmera, HUD, EventSystem. |
| `Prefabs/Itens` | Os 5 itens perdidos do Nível 1. |
| `Prefabs/Efeitos` | Partícula de coleta. |
| `Materiais` | Materiais e texturas feitos pelo grupo. |
| `Models` | Personagem (Remy, Mixamo). |
| `Animations/Calouro` | Animações e o Animator do personagem. |
| `Audio` | Músicas e efeitos sonoros. |

## Como o Nível 1 funciona
1. **SpawnItens** sorteia 5 dos 8 pontos (`PontosSpawn`) e cria um item em cada.
2. Cada item tem **ItemColetavel**: gira, flutua e, ao encostar no jogador, avisa o **GameManager**.
3. O **GameManager** soma 1 no contador. Com 5 de 5, manda a **PortaDeslizante** abrir.
4. Ao entrar na área **SaidaNivel**, o jogo carrega a próxima cena.
5. O **UIManager** cuida dos textos da tela e do botão Reiniciar.

## Scripts
| Script | Onde está na cena | Função |
|---|---|---|
| `GameManager` | `_GameManager` | Regras do nível: contador, porta, troca de cena. |
| `UIManager` | `Canvas_HUD` | Textos da tela, avisos, introdução, botão Reiniciar. |
| `SpawnItens` | `Gameplay/ItensSorteados` | Sorteio dos itens entre os pontos. |
| `ItemColetavel` | Prefabs de itens | Rotação, coleta, som e partícula. |
| `PortaDeslizante` | `Porta_Saida` | Porta que desliza com Translate. |
| `SaidaNivel` | `Gameplay/Saida_Trigger` | Área que leva ao próximo nível. |
| `CadeiraGiratoria` | `Cadeira_1` a `6` | Cadeira gira com AddTorque ao encostar. |
| `EfeitoFaisca` | `Efeitos_Ambiente/Faiscas_Multiplug` | Faíscas, luz e som do curto-circuito. |
| `PlayerController` | `Jogador` | Movimento, pulo e animação. |
| `CameraFollow` | `Main Camera` | Câmera em terceira pessoa com colisão. |
| `AtalhosTeste` | `_GameManager` | Teclas R e N. |
| `NotaDoDesenvolvedor` | Vários objetos | Só documentação: mostra uma explicação no Inspector. |

**Dica:** clique em um objeto da Hierarchy. Se ele tiver o componente *Nota Do Desenvolvedor*, a explicação aparece no Inspector.

## Tarefas comuns
- **Mudar um texto da tela:** `_GameManager` → campos *Texto Objetivo* / *Texto Porta Liberada*. A introdução fica em `Canvas_HUD > Painel_Intro > Texto_Intro`.
- **Mover um ponto de item:** selecione `P1`...`P8` em `Gameplay/PontosSpawn` e arraste.
- **Trocar o modelo de um item:** abra o prefab em `Prefabs/Itens` e troque os filhos visuais. Não mexa no componente ItemColetavel nem no collider.
- **Adicionar decoração:** arraste um prefab de `Assets/school/Prefabs/props` para dentro de `Nivel1_Cenario/Mobiliario`.
- **Trocar um som:** coloque o arquivo em `Audio` e arraste para o campo de som do objeto (ex.: *Som Abrir* na `Porta_Saida`).

## Regras para não dar conflito no Git
- Só **uma pessoa por vez** edita a mesma cena. Combine no grupo antes.
- Para montar algo grande, monte em um prefab separado e depois uma pessoa coloca na cena.
- Sempre dê *Pull* antes de começar e *Commit + Push* ao terminar.
- Arquivos acima de 100 MB não sobem para o GitHub. Sons e músicas devem ser curtos (.wav ou .ogg para efeitos, .mp3/.ogg para música).

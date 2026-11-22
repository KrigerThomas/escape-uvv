# Escape UVV

Jogo 3D em terceira pessoa feito na Unity para a disciplina de **Desenvolvimento de Games** da Universidade Vila Velha (UVV), 2026/2.

O calouro pegou no sono no InovaLab e acordou com o campus fechado. Para sair, ele passa por cinco lugares da UVV, cada um com uma tarefa.

**Grupo:** Brenno Gomes Breda, Daphne Rocha Amigo, Giovanna Rocha Santana e Thomás Kriger de Souza
**Professor:** Saulo Pereira Ribeiro

## Níveis

| Nível | Lugar | O que fazer |
|---|---|---|
| 1 | InovaLab | Achar os 5 itens perdidos (sorteados a cada partida) e sair pela porta |
| 2 | Pátio à noite | Atravessar o labirinto de sebes e achar 5 itens para abrir o portão |
| 3 | Corredor | Acertar 3 perguntas do Quadro do Abrantes para abrir a porta corta-fogo |
| 4 | Biblioteca | Guardar a mochila no armário, passar pela catraca, achar 3 livros e sair no apagão |
| 5 | Sala 12 | Pegar o celular, achar a chave do bloco e fugir da professora que faz a ronda |

## Como abrir

1. Instale a **Unity 6000.0.80f1** pelo Unity Hub (a mesma versão do laboratório 12).
2. Clone o repositório pelo **GitHub Desktop**. Ele baixa os arquivos grandes (áudios, modelos, texturas) pelo Git LFS automaticamente. Se usar outro programa, instale o [Git LFS](https://git-lfs.com) antes de clonar.
3. No Unity Hub, clique em **Add** e escolha a pasta do projeto.
4. Abra a cena `Assets/_Projeto/Scenes/Nivel1` e aperte **Play**.

## Controles

| Tecla | Ação |
|---|---|
| W, A, S, D ou setas | Andar |
| Shift | Correr |
| Espaço | Pular |
| Mouse | Girar a câmera |
| E | Interagir |
| F | Lanterna do celular |
| Tab | Ver o objetivo |
| Esc | Pausa (continuar, reiniciar o nível ou sair) |

## Organização

```
Assets/
├── _Projeto/      ← tudo que o grupo fez: cenas, scripts, prefabs, materiais, áudio, interface
│   ├── Scenes/    ← Nivel1 a Nivel5 e FimDeJogo
│   ├── Scripts/   ← Base, Interacao, Itens, Cenario, Efeitos, Som, Personagens, Biblioteca
│   └── Editor/    ← janelas do menu Escape UVV (Textos do nível e Sons do nível)
├── _Pacotes/      ← pacotes de terceiros (School Assets, POLYGON city, animais, plantas...)
├── TextMesh Pro/
└── Settings/      ← configurações do URP
```

Todas as cenas seguem a mesma estrutura: `Main Camera`, `Jogador`, `_GameManager`, `Canvas_HUD`, `EventSystem`, `NivelX_Cenario` e `Pos_Processamento`.

Para mudar textos e sons sem mexer em código, use o menu **Escape UVV > Textos do nível** e **Escape UVV > Sons do nível**.

## Onde cada requisito aparece

| Requisito | Onde está no jogo |
|---|---|
| Colisão e trigger | `OnTriggerEnter` nos itens, portas e saídas; `OnCollisionEnter` nas latinhas do pátio (`LataChutavel`) |
| Translate | Portas de correr (`PortaDeslizante`) |
| AddForce | Pulo do jogador (`PlayerController`) e chute das latinhas |
| Rotação | Câmera, portas giratórias, catraca, itens girando, gatos e girassóis |
| Torque | `AddTorque` nas latinhas chutadas no Nível 2 |
| Vetor de prefabs e posição sorteada | `SpawnItens` (Níveis 1 e 2) e `SorteioLivros` (Nível 4) |
| Spawn por função | `SpawnItens.Instantiate` e o efeito de coleta |
| Partículas criadas pelo grupo | Faíscas e fumaça (N1), vagalumes (N2), chuva (N3 a N5), brilho dos segredos e efeito de coleta |
| Som | Ambiente por nível, passos por piso, portas, interface, perseguição |
| Animação | Parado, andando, correndo, pulando e caindo (Mixamo); gato, esquilo e professora |
| Materiais com cor, textura e normal map | Mais de 20 materiais em `_Projeto/Materiais` |
| UI com botão de reiniciar | Pausa (Reiniciar nível) e tela final (Jogar de novo) |
| Terreno com 10 camadas | Nível 2 (`Terreno/Camadas`) |
| Tag | Player, Chao, Porta, ItemPerdido |
| NavMesh e NPC | Professora do Nível 5 |
| Iluminação pontual, área, baked e mixed | Nível 1: luminárias Mixed, luzes de área e lua Baked, lightmap e light probes |

## Tecnologias

- Unity 6 (6000.0.80f1) com Universal Render Pipeline (URP)
- C# com documentação XML em cada função
- TextMeshPro, Particle System, Animator (Mixamo), AI Navigation e Volume (pós-processamento)

## Créditos de terceiros

| Asset | Origem | Uso |
|---|---|---|
| School Assets | Unity Asset Store | Salas, corredores e móveis |
| POLYGON city pack | Unity Asset Store | Pátio e prédios |
| Gato Misty (Ladymito) | Unity Asset Store | Nível 2 |
| Realistic Cat Demo | Unity Asset Store | Gato cinza do Nível 2 |
| Furry Squirrel | Unity Asset Store | Esquilo do Nível 2 |
| Living Birds | Unity Asset Store | Pássaros do Nível 2 |
| Ornamental Flower Set e Patchmesh | Unity Asset Store | Flores e canteiros do Nível 2 |
| PBR Lamps Pack, OldExtinguisher, Modular Sofa, QA Books, Free Rug Pack | Unity Asset Store | Luminárias, extintores, sofás, livros e tapetes |
| Texturas do terreno | ambientCG (CC0) | Nível 2 |
| Arduino, mouse, fone, celular, pen drive, chave, poste e latinha | Sketchfab | Itens e cenário |
| Elizabeth Warren caricature | Unity Asset Store | Modelo da professora do Nível 5 |
| Personagem Remy e animações | Mixamo (Adobe) | Jogador |
| Fontes Barlow e Kalam | Google Fonts (OFL) | Interface |
| Sons e músicas | Pixabay e outras bibliotecas gratuitas | Todos os níveis |

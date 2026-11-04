# Escape UVV

Jogo 3D em terceira pessoa feito na Unity para a disciplina de **Desenvolvimento de Games** da Universidade Vila Velha (UVV), 2026/2.

Contexto: Um calouro ficou preso no campus depois do fechamento e precisa passar por cinco lugares da UVV para conseguir sair, onde cada lugar é um nível curto, com uma única tarefa.

**Grupo:** Brenno Gomes Breda, Daphne Rocha Amigo, Giovanna Rocha Santana e Thomás Kriger de Souza
**Professor:** Saulo Pereira Ribeiro

## Níveis

| Nível | Local | Objetivo |
|---|---|---|
| 1 | Laboratório (InovaLab) | Encontrar 5 itens perdidos e sair pela porta |
| 2 | Pátio à noite | Atravessar o pátio até o portão |
| 3 | Corredor | Responder a pergunta do Quadro do Abrantes |
| 4 | Biblioteca | Encontrar os livros perdidos |
| 5 | Sala de aula | Fugir do professor e chegar à porta |

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
| E | Interagir (ex.: fazer carinho no gato cinza do Nível 2) |
| Esc | Soltar o mouse para clicar nos botões |
| R | Reiniciar o nível (atalho de teste) |
| N | Pular para o próximo nível (atalho de teste) |

## Organização

Todo o conteúdo feito pelo grupo fica em `Assets/_Projeto` (cenas, scripts, prefabs, materiais, áudio e interface).

```
Assets/
├── _Projeto/          ← tudo do grupo (cenas, scripts, prefabs, áudio, UI)
├── school/            ← pack "School Assets" (Unity Asset Store)
├── POLYGON city pack/
├── Ladymito/          ← modelo do gato Misty (Nível 2)
├── living birds/      ← pássaros animados (Nível 2)
├── (pacotes de plantas, esquilo e gato cinza, Nível 2)
├── TextMesh Pro/      ← fontes da interface
└── Settings/          ← configurações do URP
```

## Tecnologias

- Unity 6 (6000.0.80f1) com Universal Render Pipeline (URP)
- C# (scripts comentados com documentação XML)
- TextMeshPro, Particle System, Animator (Mixamo) e AI Navigation

## Créditos de terceiros

| Asset | Origem | Uso |
|---|---|---|
| School Assets | Unity Asset Store | Cenários internos |
| POLYGON city pack | Unity Asset Store | Pátio e prédios |
| Gato Misty (Ladymito) | Unity Asset Store | Nível 2 |
| Gato cinza | Unity Asset Store | Nível 2 (carinho com a tecla E) |
| Esquilo | Unity Asset Store | Nível 2 (foge do jogador) |
| Living Birds | Unity Asset Store | Nível 2 (pássaros que pousam, cantam e voam) |
| Pacote de plantas 1 | Unity Asset Store | Nível 2 (flores e canteiros) |
| Pacote de plantas 2 | Unity Asset Store | Nível 2 (vasos e floreiras) |
| Personagem Remy e animações | Mixamo (Adobe) | Jogador |
| Sons e músicas | Bibliotecas gratuitas (origem e licença de cada arquivo na planilha de assets do grupo) | Todos os níveis |

> Alguns sons do Nível 2 (chiado do esquilo, ronronar e chiado do gato, folhas) são provisórios, gerados por código (arquivos `*_provisorio.wav` em `Assets/_Projeto/Audio`).

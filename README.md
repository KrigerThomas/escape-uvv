# Escape UVV

Jogo 3D em terceira pessoa feito na Unity para a disciplina de **Desenvolvimento de Games** da Universidade Vila Velha (UVV), 2026/2.

Um calouro fica preso no campus depois do fechamento e precisa passar por cinco lugares da UVV para conseguir sair. Cada lugar é um nível curto, com uma única tarefa.

**Grupo:** Brenno Gomes Breda, Daphne Rocha Amigo, Giovanna Rocha Santana e Thomás Kriger de Souza
**Professor:** Saulo Pereira Ribeiro

## Níveis

| Nível | Local | Objetivo |
|---|---|---|---|
| 1 | Laboratório (InovaLab) | Encontrar 5 itens perdidos e sair pela porta |
| 2 | Pátio à noite | Atravessar o pátio até o portão |
| 3 | Corredor | Responder a pergunta do Quadro do Abrantes |
| 4 | Biblioteca | Encontrar os livros perdidos |
| 5 | Sala de aula | Fugir do professor e chegar à porta |

## Como abrir

1. Instale a **Unity 6000.0.8f1** pelo Unity Hub (a mesma versão do laboratório 12).
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
| Esc | Soltar o mouse para clicar nos botões |
| R | Reiniciar o nível (atalho de teste) |
| N | Pular para o próximo nível (atalho de teste) |

## Organização

Todo o conteúdo feito pelo grupo fica em `Assets/_Projeto` (cenas, scripts, prefabs, materiais, áudio e interface).

```
Assets/
├── _Projeto/        ← tudo do grupo (cenas, scripts, prefabs, áudio, UI)
├── school/          ← pack "School Assets" (Unity Asset Store)
├── POLYGON city pack/
├── Ladymito/        ← modelo do gato (Nível 2)
├── TextMesh Pro/    ← fontes da interface
└── Settings/        ← configurações do URP
```

## Tecnologias

- Unity 6 (6000.0.8f1) com Universal Render Pipeline (URP)
- C# (scripts comentados com documentação XML)
- TextMeshPro, Particle System, Animator (Mixamo) e AI Navigation

## Créditos de terceiros

| Asset | Origem |
|---|---|
| School Assets | Unity Asset Store |
| POLYGON city pack | Unity Asset Store |
| Gato (Ladymito) | Unity Asset Store |
| Personagem Remy e animações | Mixamo (Adobe) |
| Sons e músicas | Bibliotecas gratuitas (origem e licença de cada arquivo na planilha de assets do grupo) |

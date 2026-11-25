# Escape UVV

Jogo de aventura em terceira pessoa feito em Unity para a disciplina de Desenvolvimento de Games da Universidade Vila Velha (2026/2).

Você é um calouro que dormiu estudando no InovaLab e acordou com a UVV fechada. Para ir embora, tem que passar por cinco lugares do campus: achar suas coisas no laboratório, atravessar o labirinto do pátio, responder o quadro do corredor, pegar livros na biblioteca durante um apagão e sair da Sala 12 sem ser pego pela professora.

| | |
|---|---|
| ![Menu](Docs/Imagens/menu.jpg) | ![InovaLab](Docs/Imagens/nivel1.jpg) |
| ![Pátio](Docs/Imagens/nivel2.jpg) | ![Corredor](Docs/Imagens/nivel3.jpg) |
| ![Biblioteca](Docs/Imagens/nivel4.jpg) | ![Sala 12](Docs/Imagens/nivel5.jpg) |

## Gameplay

Vídeo da gameplay no YouTube: https://youtu.be/5AvyqnO0iDg

## Jogar

Baixe o zip da [versão mais recente](https://github.com/KrigerThomas/escape-uvv/releases/latest), extraia e abra `Escape UVV.exe`. Roda no Windows e não precisa instalar nada.

A mesma build também fica na pasta `Jogo` do repositório.

## Controles

| Tecla | Ação |
|---|---|
| W A S D | Andar |
| Shift | Correr |
| Espaço | Pular |
| Mouse | Câmera |
| E | Interagir |
| F | Lanterna (só nos dois últimos níveis) |
| Tab | Mostrar o objetivo |
| L | Lista de livros (biblioteca) |
| Esc | Pausa |

Volume, sensibilidade do mouse e tela cheia ficam em **Opções**, no menu inicial.

## Abrindo o projeto na Unity

Precisa da Unity **6000.0.80f1** e do [Git LFS](https://git-lfs.com) (o GitHub Desktop já vem com ele).

1. Clone o repositório.
2. No Unity Hub, clique em **Add** e selecione a pasta.
3. Abra `Assets/_Projeto/Scenes/Menu` e dê Play.

Para gerar o executável: **File > Build Profiles > Windows > Build**. As cenas já estão na ordem.

O que fizemos fica em `Assets/_Projeto` (cenas, scripts, prefabs, modelos, áudio e interface). Pacotes baixados da Asset Store e de outros sites ficam em `Assets/_Pacotes`.

## Documentos

- [GDD](Docs/GDD_Escape_UVV.pdf)
- [Requisitos do trabalho](Docs/Requisitos.md)
- [Créditos dos assets](Docs/Creditos.md)

## Equipe

Brenno Gomes Breda, Daphne Rocha Amigo, Giovanna Rocha Santana e Thomás Kriger de Souza.

Professor: Saulo Pereira Ribeiro.

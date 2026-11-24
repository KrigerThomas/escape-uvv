<p align="center">
  <img src="Docs/Imagens/menu.jpg" alt="Escape UVV" width="100%">
</p>

<h1 align="center">Escape UVV</h1>

<p align="center">
  Aventura de suspense em terceira pessoa ambientada no campus da Universidade Vila Velha.<br>
  Feita em Unity 6 com URP.
</p>

---

Era para ser só mais uma noite estudando no InovaLab. O calouro pegou no sono e, quando acordou, o campus já tinha fechado. As coisas dele estão espalhadas pelo prédio, o portão está trancado e tem alguém fazendo ronda.

Para sair, ele precisa atravessar cinco lugares da UVV. Em cada um, o caminho de volta para casa depende de explorar, prestar atenção e não fazer barulho demais.

## Os lugares

<table>
  <tr>
    <td width="50%"><img src="Docs/Imagens/nivel1.jpg" alt="InovaLab"></td>
    <td width="50%"><img src="Docs/Imagens/nivel2.jpg" alt="O Pátio"></td>
  </tr>
  <tr>
    <td><b>O InovaLab.</b> O laboratório onde tudo começa. Os objetos do calouro aparecem em lugares diferentes a cada partida.</td>
    <td><b>O Pátio.</b> Um labirinto de sebes ao redor de uma fonte, com gatos, um esquilo que não para quieto e uma tempestade chegando.</td>
  </tr>
  <tr>
    <td><img src="Docs/Imagens/nivel3.jpg" alt="O Corredor"></td>
    <td><img src="Docs/Imagens/nivel4.jpg" alt="A Biblioteca"></td>
  </tr>
  <tr>
    <td><b>O Corredor.</b> A porta corta-fogo só abre para quem acertar as perguntas deixadas no Quadro do Abrantes.</td>
    <td><b>A Biblioteca.</b> Catraca, armário, terminal de consulta e três livros para achar antes da energia cair.</td>
  </tr>
  <tr>
    <td><img src="Docs/Imagens/nivel5.jpg" alt="Sala 12"></td>
    <td valign="top"><b>Sala 12.</b> O celular ficou carregando no fundo da sala, a chave do bloco está na mesa da professora e ela ainda está no prédio. Dá para se esconder nos armários do corredor, mas a lanterna chama atenção.</td>
  </tr>
</table>

## Destaques

- Cinco níveis com objetivos próprios, ligados por aberturas curtas e uma tela final com créditos
- Itens e livros sorteados a cada partida
- Professora com navegação por NavMesh, ronda, perseguição e esconderijos
- Apagão com lâmpada estourando, relâmpagos e chuva que soa abafada dentro dos prédios
- Passos que mudam de som conforme o chão: piso, pedra ou grama
- Easter eggs espalhados pelo pátio
- Menu com opções de volume, sensibilidade do mouse e tela cheia

## Controles

| Tecla | Ação |
|---|---|
| W A S D | Andar |
| Shift | Correr |
| Espaço | Pular |
| Mouse | Olhar em volta |
| E | Interagir |
| F | Lanterna do celular |
| Tab | Ver o objetivo |
| Esc | Pausa |

## Rodando o projeto

1. Instale a **Unity 6000.0.80f1** pelo Unity Hub.
2. Clone o repositório com o **GitHub Desktop**, que já baixa os arquivos grandes pelo Git LFS. Em outros programas, instale o [Git LFS](https://git-lfs.com) antes.
3. No Unity Hub, clique em **Add** e escolha a pasta do projeto.
4. Abra a cena `Assets/_Projeto/Scenes/Menu` e aperte **Play**.

Para gerar o executável: **File > Build Profiles > Windows > Build**. As cenas já estão na ordem certa.

## Estrutura

```
Assets/
├── _Projeto/        conteúdo do jogo
│   ├── Scenes/      Menu, Nivel1 a Nivel5 e FimDeJogo
│   ├── Scripts/     código em C#, separado por sistema
│   ├── Prefabs/     objetos prontos para usar nas cenas
│   ├── Models/      modelos 3D, separados por lugar
│   ├── Materiais/   Geral e uma pasta por nível
│   ├── Audio/       Geral, Menu e uma pasta por nível
│   ├── Terreno/     camadas e texturas do terreno do pátio
│   └── UI/          fontes, imagens e o HUD
└── _Pacotes/        pacotes de terceiros
Docs/                GDD, requisitos da disciplina, créditos e imagens
```

Textos e sons de cada nível podem ser ajustados sem mexer no código pelo menu **Escape UVV** da Unity.

## Documentação

- [Game Design Document](Docs/GDD_Escape_UVV.pdf)
- [Requisitos da disciplina](Docs/Requisitos.md)
- [Créditos](Docs/Creditos.md)

## Equipe

**Brenno Gomes Breda**, **Daphne Rocha Amigo**, **Giovanna Rocha Santana** e **Thomás Kriger de Souza**

Desenvolvido para a disciplina de Desenvolvimento de Games da Universidade Vila Velha, 2026/2, com orientação do professor Saulo Pereira Ribeiro.

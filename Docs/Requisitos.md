# Requisitos da disciplina

Onde cada item pedido no trabalho de Desenvolvimento de Games (UVV, 2026/2) aparece no Escape UVV.

## Mecânicas e programação

| Requisito | Onde está no jogo |
|---|---|
| Colisão e trigger | `OnTriggerEnter` e `OnTriggerStay` nos itens, segredos, portas e saídas; `OnCollisionEnter` no livro que cai da estante (`LivroCaindo`, Nível 4) |
| Movimentação com Translate | Portas de correr (`PortaDeslizante`) |
| Movimentação com AddForce | Pulo do jogador (`PlayerController`) e livro empurrado para fora da estante (Nível 4) |
| Rotação | Câmera orbital, portas giratórias, catraca, itens girando, gatos e girassóis que seguem o jogador |
| Torque | `AddTorque` no livro que despenca girando da estante a cada livro pego (Nível 4) |
| Vetor de prefabs e posição sorteada | `SpawnItens` (Níveis 1 e 2) e `SorteioLivros` (Nível 4) |
| Spawn de objetos por função | `SpawnItens.SortearItens` com `Instantiate`, efeito de coleta e livro que cai |
| Tag | Player, Chao, Porta, ItemPerdido e as tags dos pássaros |
| Scripts documentados | Toda função tem comentário XML (`summary`, `param`, `returns`) e comentários no corpo |

## Conteúdo

| Requisito | Onde está no jogo |
|---|---|
| Cinco níveis | InovaLab, Pátio, Corredor, Biblioteca e Sala 12, mais o menu e a tela final |
| Pelo menos 70 assets por nível | Cada nível tem de 230 a 670 objetos renderizados, de categorias diferentes |
| Partículas criadas pelo grupo | Faíscas e fumaça (N1), vagalumes (N2), chuva (N3 a N5), estouro da lâmpada no apagão (N4), brilho dos itens e efeito de coleta |
| Explosão e fumaça | Estouro da lâmpada no apagão (N4) e fumaça do rack de servidores (N1) |
| Som | Música no menu e na tela final, ambiente por nível, passos que mudam com o chão, portas, interface, animais e perseguição |
| Animação | Jogador parado, andando, correndo, pulando e caindo (Mixamo); gato, esquilo e professora |
| Materiais com cor, textura e normal map | Mais de 20 materiais em `Assets/_Projeto/Materiais` |
| UI com texto e botão de reiniciar | Menu com Opções e Créditos, HUD, pausa com Reiniciar nível e tela final com Jogar de novo |
| Terreno com 10 camadas | Pátio do Nível 2 (`Assets/_Projeto/Terreno/Camadas`), com relevo, lago, caminhos, cascalho, grama e árvores |
| Texturas do terreno com diffuse, normal e height | As 10 camadas do Nível 2 |
| NavMesh e NPC | Professora do Nível 5, com ronda, perseguição e NavMesh Link na porta de saída |
| Iluminação pontual, de área, baked e mixed | Nível 1: luminárias Mixed, luzes de área e lua Baked, lightmap e light probes; luzes pontuais em todos os níveis |

## Tema

| Tema | Elementos |
|---|---|
| Tecnologia | Computadores e rack de servidores do InovaLab, multiplug com faíscas, itens perdidos (pen drive, mouse, Arduino, celular e fone), terminal de consulta e catraca com reconhecimento facial da biblioteca, lanterna do celular |
| Ecorrenovação | Horta e composteira do pátio, fonte, plantas e animais do parque, lixeiras separadas por cor no corredor |
| Skills | Quadro do Abrantes com perguntas de Unity e da disciplina, livros de Cálculo, Álgebra Linear e Estruturas de Dados, apostila de Cálculo perdida no pátio |

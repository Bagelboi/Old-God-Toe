# Old-God-Toe

> Jogo da velha em 3D, com um clima sombrio.

O jogo foi feito para testar os recursos da Godot Engine e o suporte a Mono (C#), e também para conseguir terminar um jogo com 90% das features que eu tinha planejado. O gameplay é um jogo da velha em 3D. Tem tutorial em slides, multiplayer, uma IA básica, personalização de perfil e configurações de partida.

🎮 **Jogue / baixe:** [daniellpk.itch.io/old-god-tac-toe](https://daniellpk.itch.io/old-god-tac-toe)

**Gênero:** Estratégia · Jogo de tabuleiro
**Plataformas:** Windows (32/64-bit), Linux (x86_64 / ARM64), Android

## Features

- **Jogo da velha em 3D**: tabuleiro cúbico de tamanho configurável (3×3×3 por padrão). A vitória vale em linhas, colunas e em todas as 13 direções diagonais do cubo.
- **Modos de jogo**: local (no mesmo dispositivo) ou online.
- **Multiplayer online**: usa ENet e as RPCs da Godot. Dá para escolher a porta, e o UPnP abre a porta no roteador sozinho.
- **IA básica**: avalia cada casa pelo número de marcadores que faltam para vencer e pelas interseções entre linhas.
- **Tutorial**: um slideshow que ensina as regras.
- **Perfil do jogador**: aparência customizável (sala de perfil e alfaiataria) e vários slots de save, guardados em `user://data.sav` com criptografia.
- **Configurações**: volume, cores da caixa e do skybox, modo de cores e opção de esconder os jogadores (salvas em `user://config.cfg`).
- **Controles por toque**: joystick virtual no Android.

## Controles (desktop)

| Ação | Tecla |
|---|---|
| Mover | `W` `A` `S` `D` |
| Subir / descer | `Espaço` / `Ctrl` |
| Olhar | Setas |
| Selecionar casa | Clique esquerdo |
| Confirmar jogada | `Enter` |
| Alternar jogada / ciclar | `X` / `Z` · `C` |
| Teleportar | `Q` |
| Segurar seleção | `Tab` |
| Sair / menu | `Esc` |

## Stack

- **Engine:** [Godot 4.2](https://godotengine.org/) (.NET / Mono), renderer *Mobile*
- **Linguagem:** C# (.NET 6, .NET 7 no Android), com GDScript no addon
- **Pacotes NuGet:** [CsvHelper](https://joshclose.github.io/CsvHelper/) 32.0.3
- **Addon:** [Virtual Joystick](https://github.com/MarcoFazioRandom/Virtual-Joystick-Godot) de Marco Fazio
- **Rede:** `ENetMultiplayerPeer` + `Upnp` da Godot

## Estrutura

```
scenes/              Cenas (.tscn) e scripts de tela: menu, tutorial, sala de perfil
  objects/           Tabuleiro, caixa, jogador, menus in-game
scripts/
  objects/           Gameboard, Player, PlayerAI, Box, menus
  resources/         PlayerData, PlayerAppearance (save e aparência)
  singletons/        OldConst: enums, utilidades de índice e vetor, save/load
  utils/             UpnpHelper, TouchScreenControls
resources/           Assets 2D, 3D, áudio e UI
addons/              virtual_joystick
design/              Documento de design
```

## Como rodar

1. Instale a **Godot 4.2 .NET** e o **.NET SDK 6+** (para exportar para Android, o 7+).
2. Abra o `project.godot` no editor.
3. Compile o projeto C# (botão *Build*) e rode. A cena principal é `scenes/mainmenu.tscn`.

## Créditos

- **DanielLpk**: programação, modelagem, texturas e edição de som
- **Áudio**: tirado do YouTube e de outros jogos
- **Virtual Joystick**: Marco Fazio
- Feito com a [Godot Engine](https://godotengine.org/)

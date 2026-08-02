# YTDown

Aplicativo desktop para Windows que baixa videos e audios do YouTube de forma
simples.

> **Estado atual:** em desenvolvimento. O aplicativo ja consulta um video, baixa
> em MP4 na qualidade escolhida ou extrai apenas o audio em MP3, com progresso,
> cancelamento, historico e configuracoes. Ainda nao ha fila de downloads.

---

## Como executar

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e Windows.

```powershell
# 1. Baixar yt-dlp e FFmpeg (nao versionados por serem grandes e mudarem sempre)
./scripts/bootstrap-tools.ps1

# 2. Executar
dotnet run --project src/YTDown.UI
```

Cole o endereco de um video do YouTube e clique em **Buscar**. O video
reconhecido aparece na tela, junto das qualidades que ele oferece; escolha e
clique em **Baixar** para salvar na sua pasta Downloads.

Enderecos aceitos incluem `youtube.com/watch?v=...`, `youtu.be/...`,
`/shorts/...`, `/live/...`, com ou sem `https://`. Parametros de playlist e de
tempo sao ignorados: apenas o video colado e considerado.

A lista de qualidades mostra o que aquele video realmente oferece. Marque
**Baixar somente o audio** para receber um MP3 no lugar do video.

O video sai em MP4 com H.264, o formato que abre em qualquer player, celular ou
aplicativo de mensagens. Como o YouTube nao oferece H.264 acima de 1080p, essa e
a qualidade maxima, mesmo em videos publicados em 4K.

**Historico**, no canto superior direito, lista os ultimos cinquenta downloads e
abre a pasta de qualquer um deles. Limpar a lista nao apaga arquivo nenhum.

**Configuracoes**, ao lado, guarda duas escolhas: em que pasta salvar e ate que
qualidade baixar. O limite de qualidade nao impede nada — um video que so exista
em 480p continua sendo baixado.

---

## Testes

```powershell
dotnet test                                        # tudo
dotnet test --filter Category!=Integration         # sem rede
```

Os testes de integracao executam o yt-dlp de verdade e exigem conexao. Sao
quatro downloads reais por execucao: rodar em laco faz o YouTube bloquear
temporariamente o seu endereco de rede. No dia a dia, prefira o filtro acima.

---

## Gerar uma versao para entregar

```powershell
./scripts/publish.ps1
```

Produz tres coisas em `dist/`:

| Arquivo | Para que serve |
|---|---|
| `YTDown-<versao>-setup.exe` | instalador, 88 MB |
| `YTDown-<versao>-win-x64.zip` | para quem prefere nao instalar, 113 MB |
| `YTDown-<versao>-win-x64/` | a pasta publicada, origem dos dois acima |

O .NET vai dentro do pacote: nada precisa estar instalado na maquina de destino.

O instalador **nao pede administrador** — instala para o usuario atual, em
`%LOCALAPPDATA%\Programs\YTDown`. Desinstalar remove o aplicativo mas preserva
`%LOCALAPPDATA%\YTDown`, onde ficam o historico e as configuracoes.

O instalador exige o [Inno Setup](https://jrsoftware.org/isdl.php) 6 ou 7; sem
ele, o script gera apenas a pasta e o zip. O executavel nao e assinado, entao o
Windows avisa que o programa e de origem desconhecida e a pessoa precisa
escolher executar mesmo assim.

---

## Estrutura

```
src/YTDown.Domain           regras puras, sem dependencias
src/YTDown.Application      servicos, contratos e DTOs
src/YTDown.Infrastructure   yt-dlp, FFmpeg, processos
src/YTDown.UI               WPF, MVVM
```

Arquitetura, decisoes e roadmap estao em [CLAUDE.md](CLAUDE.md).

---

## Ferramentas externas

O YTDown usa [yt-dlp](https://github.com/yt-dlp/yt-dlp) e
[FFmpeg](https://ffmpeg.org/). As versoes e os hashes ficam fixados em
`tools/tools.lock.json`; `scripts/bootstrap-tools.ps1` baixa e verifica cada uma.

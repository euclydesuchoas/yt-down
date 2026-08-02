# YTDown

Aplicativo desktop para Windows que baixa videos e audios do YouTube de forma
simples.

> **Estado atual:** em desenvolvimento. O aplicativo ja consulta um video, baixa
> em MP4 na qualidade escolhida ou extrai apenas o audio em MP3, com progresso e
> cancelamento. Ainda nao ha historico nem configuracoes.

---

## Como executar

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e Windows.

```powershell
# 1. Baixar yt-dlp e FFmpeg (nao versionados por serem grandes e mudarem sempre)
./scripts/bootstrap-tools.ps1

# 2. Executar
dotnet run --project src/YTDown.UI
```

Cole o endereco de um video do YouTube. **Buscar** mostra qual video foi
reconhecido; **Baixar** salva o arquivo na sua pasta Downloads.

Enderecos aceitos incluem `youtube.com/watch?v=...`, `youtu.be/...`,
`/shorts/...`, `/live/...`, com ou sem `https://`. Parametros de playlist e de
tempo sao ignorados: apenas o video colado e considerado.

Depois de **Buscar**, a lista de qualidades mostra o que aquele video realmente
oferece. Marque **Baixar somente o audio** para receber um MP3 no lugar do video.

O video sai em MP4 com H.264, o formato que abre em qualquer player, celular ou
aplicativo de mensagens. Como o YouTube nao oferece H.264 acima de 1080p, essa e
a qualidade maxima, mesmo em videos publicados em 4K.

---

## Testes

```powershell
dotnet test                                        # tudo
dotnet test --filter Category!=Integration         # sem rede
```

Os testes de integracao executam o yt-dlp de verdade e exigem conexao.

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

# YTDown

Aplicativo desktop para Windows que baixa vídeos e áudios do YouTube de forma
simples.

> **Estado atual:** em desenvolvimento. O aplicativo já consulta um vídeo, baixa
> em MP4 na qualidade escolhida ou extrai apenas o áudio em MP3, com progresso,
> cancelamento, histórico e configurações. Ainda não há fila de downloads.

---

## Como executar

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e Windows.

```powershell
# 1. Baixar yt-dlp e FFmpeg (não versionados por serem grandes e mudarem sempre)
./scripts/bootstrap-tools.ps1

# 2. Executar
dotnet run --project src/YTDown.UI
```

Cole o endereço de um vídeo do YouTube e clique em **Buscar**. O vídeo
reconhecido aparece na tela, junto das qualidades que ele oferece; escolha e
clique em **Baixar** para salvar na sua pasta Downloads.

Endereços aceitos incluem `youtube.com/watch?v=...`, `youtu.be/...`,
`/shorts/...`, `/live/...`, com ou sem `https://`. Parâmetros de playlist e de
tempo são ignorados: apenas o vídeo colado é considerado.

A lista de qualidades mostra o que aquele vídeo realmente oferece. Marque
**Baixar somente o áudio** para receber um MP3 no lugar do vídeo.

**Nome** vem preenchido com o título do vídeo e pode ser trocado — clicar
seleciona tudo, então basta digitar por cima. A extensão fica ao lado e não é
digitada. Se já houver um arquivo com esse nome na pasta, o novo sai como
`Nome (2)`, sem apagar o anterior.

**Salvar em** escolhe a pasta deste download, sem mexer nas configurações. A
lista traz a pasta padrão e as usadas recentemente; **Escolher...** abre o
seletor do Windows. A escolha vale para os próximos downloads até fechar o
aplicativo.

O vídeo sai em MP4 com H.264, o formato que abre em qualquer player, celular ou
aplicativo de mensagens. Como o YouTube não oferece H.264 acima de 1080p, essa é
a qualidade máxima, mesmo em vídeos publicados em 4K.

**Histórico**, no canto superior direito, lista os últimos cinquenta downloads e
abre a pasta de qualquer um deles. Limpar a lista não apaga arquivo nenhum.

**Configurações**, ao lado, guarda duas escolhas: em que pasta salvar e até que
qualidade baixar. O limite de qualidade não impede nada — um vídeo que só exista
em 480p continua sendo baixado.

---

## Testes

```powershell
dotnet test                                        # tudo
dotnet test --filter Category!=Integration         # sem rede
```

Os testes de integração executam o yt-dlp de verdade e exigem conexão. São
quatro downloads reais por execução: rodar em laço faz o YouTube bloquear
temporariamente o seu endereço de rede. No dia a dia, prefira o filtro acima.

---

## Gerar uma versão para entregar

```powershell
./scripts/publish.ps1
```

Produz três coisas em `dist/`:

| Arquivo | Para que serve |
|---|---|
| `YTDown-<versão>-setup.exe` | instalador, 88 MB |
| `YTDown-<versão>-win-x64.zip` | para quem prefere não instalar, 113 MB |
| `YTDown-<versão>-win-x64/` | a pasta publicada, origem dos dois acima |

O .NET vai dentro do pacote: nada precisa estar instalado na máquina de destino.

O instalador **não pede administrador** — instala para o usuário atual, em
`%LOCALAPPDATA%\Programs\YTDown`. Desinstalar remove o aplicativo mas preserva
`%LOCALAPPDATA%\YTDown`, onde ficam o histórico e as configurações.

O instalador exige o [Inno Setup](https://jrsoftware.org/isdl.php) 6 ou 7; sem
ele, o script gera apenas a pasta e o zip. O executável não é assinado, então o
Windows avisa que o programa é de origem desconhecida e a pessoa precisa
escolher executar mesmo assim.

---

## Estrutura

```
src/YTDown.Domain           regras puras, sem dependencias
src/YTDown.Application      serviços, contratos e DTOs
src/YTDown.Infrastructure   yt-dlp, FFmpeg, processos
src/YTDown.UI               WPF, MVVM
```

Arquitetura, decisões e roadmap estão em [CLAUDE.md](CLAUDE.md).

---

## Ferramentas externas

O YTDown usa [yt-dlp](https://github.com/yt-dlp/yt-dlp) e
[FFmpeg](https://ffmpeg.org/). As versões e os hashes ficam fixados em
`tools/tools.lock.json`; `scripts/bootstrap-tools.ps1` baixa e verifica cada uma.

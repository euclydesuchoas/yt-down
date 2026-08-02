# CLAUDE.md

Memoria tecnica do projeto. Este documento deve ser suficiente para entender o
YTDown sem ler todo o codigo, e e a **documentacao viva**: onde ele divergir de
[`docs/especificacao.md`](docs/especificacao.md), que e o brief original
preservado sem manutencao, este documento prevalece.

---

## Visao geral

**YTDown** e um aplicativo desktop para Windows que baixa videos e audios do
YouTube. O publico-alvo e o usuario comum, sem conhecimento tecnico, e a
prioridade e simplicidade de uso.

O aplicativo nao reimplementa nada que o **yt-dlp** e o **FFmpeg** ja resolvem:
ele os orquestra e apresenta o resultado de forma compreensivel.

**Estado atual:** consulta de metadados e download funcionando de ponta a ponta,
com progresso, cancelamento e abertura da pasta. Sem selecao de qualidade,
historico ou configuracoes.

---

## Arquitetura

MVVM + Clean Architecture, quatro camadas.

```
UI ─────────────► Application ─────────► Domain
 │                     ▲
 └──► Infrastructure ──┘
              │
              └────────────────────────► Domain
```

| Camada | Responsabilidade | Nunca faz |
|---|---|---|
| `YTDown.Domain` | Entidades, value objects, excecoes de dominio | Depender de qualquer coisa |
| `YTDown.Application` | Servicos, contratos, DTOs, `Result` | Conhecer WPF, yt-dlp ou Infrastructure |
| `YTDown.Infrastructure` | Processos, ferramentas externas, sistema de arquivos | Conhecer a UI |
| `YTDown.UI` | Views, ViewModels, converters, composition root | Regra de negocio ou `Process` |

As interfaces de integracao (`IVideoMetadataProvider`) sao **declaradas na
Application** e **implementadas na Infrastructure**. E isso que mantem a
Application sem qualquer conhecimento de yt-dlp.

Sete testes em `tests/YTDown.ArchitectureTests` fixam essas regras, incluindo um
controle positivo que garante que a deteccao de dependencias esta funcionando.

---

## Estrutura de pastas

```
src/
  YTDown.Domain/          Exceptions/ ValueObjects/
  YTDown.Application/     Common/ DTOs/ DependencyInjection/ Interfaces/ Services/
  YTDown.Infrastructure/  DependencyInjection/ Processes/ Tools/ YouTube/
  YTDown.UI/              Converters/ Resources/ ViewModels/ Views/
tests/
  YTDown.UnitTests/         espelha a estrutura de src/
  YTDown.IntegrationTests/  exercita o yt-dlp real
  YTDown.ArchitectureTests/ regras de dependencia
docs/
  especificacao.md        brief original, historico, nao mantido
scripts/
  bootstrap-tools.ps1     baixa yt-dlp e FFmpeg
tools/
  tools.lock.json         versoes fixadas + SHA256
  yt-dlp.exe, ffmpeg.exe  nao versionados
```

---

## Fluxo da aplicacao

Consulta de um video:

1. `MainViewModel.SearchAsync` recebe o texto colado pelo usuario
2. `VideoInfoService` tenta criar um `VideoUrl`; entrada invalida falha aqui,
   sem iniciar processo externo
3. `YtDlpMetadataProvider` localiza o executavel via `IToolLocator`
4. `ProcessRunner` executa `yt-dlp --dump-single-json --no-playlist <url>`
5. `YtDlpVideoInfoParser` le a resposta, ou `YtDlpErrorClassifier` classifica o
   erro pela mensagem do stderr
6. O ViewModel exibe o video ou traduz o `ErrorCode` em frase pelo
   `ErrorMessages`

Download de um video:

1. `MainViewModel.DownloadAsync` cria o `Progress<T>` na linha da interface, de
   modo que cada atualizacao volte para ela sozinha
2. `DownloadService` valida a URL e pergunta o destino ao
   `IDownloadLocationProvider`
3. `YtDlpVideoDownloader` cria a pasta de trabalho, monta os argumentos e
   acompanha a saida linha a linha
4. `YtDlpProgressParser` le cada linha; `DownloadProgressAggregator` transforma
   o progresso de cada stream em um unico percentual crescente
5. A pasta de trabalho e removida em `finally`; ao cancelar, nada sobra
6. O ViewModel exibe o arquivo e habilita "Abrir pasta", que passa pelo
   `IFileExplorer` porque a apresentacao nao pode iniciar processos

---

## Tecnologias

.NET 10, WPF, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.DependencyInjection 10.0.10.

Testes: xUnit 2.9.3, FluentAssertions 7.2.2, Moq 4.20.72, NetArchTest.eNhancedEdition 1.4.5.

Versoes centralizadas em `Directory.Packages.props` (Central Package Management).

---

## Decisoes arquiteturais

### Result em vez de excecao para falhas esperadas

Video removido, video privado e queda de rede sao desfechos normais deste
aplicativo. Trafegam como `Result<T>` com um `ErrorCode` tipado. Excecao fica
reservada a defeito de programacao.

A saida bruta do yt-dlp nunca chega a tela: fica em `Result.Diagnostics`, apenas
para depuracao.

### VideoUrl normaliza, o resto do sistema nao pensa em formato

O usuario cola a mesma referencia de muitas formas: barra de enderecos,
compartilhar, aplicativo movel, YouTube Music, Shorts, live, embed. `VideoUrl`
reduz tudo ao identificador e descarta `list`, `t`, `si`, `index` e `pp`.

**Playlist e ignorada em silencio.** Quem cola a URL da barra de enderecos
durante uma playlist quer aquele video, nao os duzentos seguintes.

**Identificador solto e recusado.** Qualquer palavra de 11 caracteres validos
(`hello_world`, por exemplo) passaria na verificacao, e o erro so apareceria
depois como uma falha confusa do yt-dlp.

### O projeto de testes unitarios referencia a Infrastructure

O codigo mais fragil do projeto e a leitura da saida do yt-dlp, e ela vive na
Infrastructure. Deixa-la fora dos testes unitarios seria testar tudo menos o que
costuma quebrar. O parser e o classificador sao classes sem estado, testadas
contra respostas reais gravadas em `tests/YTDown.UnitTests/Infrastructure/YouTube/Fixtures/`.

### Binarios externos fora do Git

`yt-dlp.exe` e `ffmpeg.exe` somam 120 MB e mudam com frequencia. Cada
atualizacao versionada seria peso permanente e irremovivel no historico. Ficam
em `.gitignore`, com versoes e SHA256 fixados em `tools/tools.lock.json` e
baixados por `scripts/bootstrap-tools.ps1`.

O `YTDown.UI.csproj` referencia as ferramentas com `Condition="Exists(...)"`,
para que um clone limpo compile mesmo sem elas.

### IToolLocator existe desde o inicio

A implementacao atual so procura em `tools/` ao lado do executavel. A abstracao
existe porque o local definitivo ainda vai mudar: as ferramentas precisarao
viver em pasta gravavel para se atualizarem sozinhas, o que nao acontece com o
aplicativo instalado em Arquivos de Programas.

### Como o yt-dlp e conduzido durante o download

Tres detalhes desta integracao custaram tempo e nao devem ser desfeitos:

- **O caminho final e pedido em JSON**, com `--print "after_move:FINAL|%(filepath)j"`.
  Ao escrever em um pipe, o yt-dlp **descarta silenciosamente tudo o que nao for
  ASCII**: o video de referencia, de titulo japones, chegava como ` EDED.mp4`,
  um arquivo que nao existe em disco, embora o arquivo real estivesse correto.
  Em JSON os caracteres viajam como escapes. Definir `PYTHONIOENCODING` nao
  resolve sozinho, mas fica, por ser a outra metade da leitura em UTF-8.
- **`--print` implica `--quiet`**, o que silencia o progresso por completo.
  `--progress` o traz de volta.
- **Os arquivos intermediarios vao para uma pasta propria**, via
  `--paths temp:`, criada dentro do destino para que a mudanca final ocorra no
  mesmo volume. A limpeza e apagar a pasta inteira, em `finally`, sem depender
  de adivinhar nomes de `.part`.

O formato do progresso e imposto por nos com `--progress-template`, e nao lido
do texto que a ferramenta mostra ao usuario, que muda entre versoes. Video e
audio se distinguem porque o yt-dlp informa o codec de video como `none`
enquanto baixa o audio.

### Faixas do progresso

Video ocupa 0 a 90%, audio de 90 a 97%, e a juncao fica em 97% ate concluir.
As faixas vem da proporcao real medida: no video de referencia o audio e pouco
mais de 7% dos bytes. O percentual nunca retrocede, porque os dois streams sao
baixados em sequencia e cada um comeca do zero.

### FluentAssertions fixado em 7.x

A partir da 8.0.0 o pacote exige licenca comercial para uso nao open source. A
7.2.2 e a ultima sob Apache 2.0. **Nao atualizar sem decisao explicita.**

`NetArchTest.eNhancedEdition` substitui o `NetArchTest.Rules` original, sem
manutencao ativa. A API do fork difere: nao existe `HaveDependencyOn` no
singular, apenas `HaveDependencyOnAny` e `HaveDependencyOnAll`, e o resultado
expoe `FailingTypes` (com `FullName` e `Explanation`), nao `FailingTypeNames`.

---

## Convencoes

- **Identificadores em ingles; comentarios, documentacao e mensagens ao usuario
  em portugues.**
- Nomes de teste seguem `Metodo_Cenario_ResultadoEsperado`.
- Comentario explica **por que**, nunca **o que**. Codigo que precisa de
  comentario para dizer o que faz deve ser reescrito.
- `async`/`await` com `CancellationToken` em toda operacao que cruza processo ou
  rede.
- Injecao de dependencia sempre; cada camada expoe seu proprio
  `Add<Camada>()`.
- Conventional Commits, um assunto por commit.

---

## Ferramentas externas

| Ferramenta | Versao fixada | Uso |
|---|---|---|
| yt-dlp | 2026.07.04 | metadados e download |
| FFmpeg | 8.1.2-essentials | juncao e conversao |

Video de referencia para testes: `https://www.youtube.com/watch?v=UKcJqQqiXq0`
(titulo em japones, o que tambem exercita a codificacao UTF-8 de ponta a ponta).

---

## Limitacoes conhecidas

- **Teto de 1080p**, consequencia de preferir H.264 para nao reconverter. As
  qualidades oferecidas sao as reais do video, mas apenas as que existem em
  H.264: um video em 4K aparece com 1080p no maximo.
- **Nenhum runtime JavaScript embarcado.** O yt-dlp avisa que a extracao sem um
  runtime esta depreciada e que alguns formatos podem faltar. Embarcar um
  runtime significa mais uma dependencia grande no instalador. Decisao adiada
  conscientemente, para a fatia das ferramentas.

  **Ja houve sintoma:** um teste de integracao falhou uma vez com
  `ERROR: unable to download video data: HTTP Error 403: Forbidden` e passou na
  execucao seguinte, sem qualquer alteracao. O 403 nesse ponto costuma indicar
  URL cuja assinatura precisaria ser decodificada por JavaScript. Como e
  intermitente, nao ha diagnostico conclusivo: se voltar com frequencia, esta e
  a primeira hipotese a investigar.
- O yt-dlp esta congelado na versao fixada. Quando o YouTube o quebrar, o
  aplicativo para de funcionar e o usuario nao tera como resolver.
- Uma playlist colada e reduzida ao video atual, sem qualquer aviso na tela.
- Apenas um download por vez, sem fila.
- Sem persistencia: nada de historico ou configuracoes.
- Sem tratador global de excecoes na UI.

---

## Roadmap tecnico

| Fatia | Conteudo |
|---|---|
| 1 (feita) | Consulta de metadados de ponta a ponta |
| 2 (feita) | Download com progresso agregado, cancelamento e limpeza |
| 3 (feita) | Selecao de qualidade e download somente de audio em MP3 |
| 4 | Ferramentas em `%LOCALAPPDATA%`, `yt-dlp -U` e runtime JavaScript |
| 5+ | Historico, configuracoes, fila, tema, distribuicao |

### Decisoes ja tomadas

- **Compatibilidade antes de qualidade.** Preferir H.264 com AAC permite juntar
  sem reconverter. A alternativa seria entregar 4K em VP9/AV1, mas ou o arquivo
  sai em MKV, que pode nao abrir onde o usuario espera, ou o FFmpeg reconverte,
  gastando minutos de CPU a 100% com a barra parada. Para o publico deste
  aplicativo, um MP4 previsivel vale mais que resolucao maior.
- **Pasta Downloads, sem perguntar.** Um seletor de pasta a cada download e
  atrito exatamente onde o publico-alvo trava. Vira configuracao na fatia 5.
- **Um download por vez.** Downloads simultaneos dividem a banda, embaralham o
  progresso e tornam cancelamento e limpeza bem mais dificeis.

### Decisao adiada

- **Runtime JavaScript.** Ver limitacoes conhecidas. Nao ha urgencia enquanto os
  formatos continuarem disponiveis, mas o aviso do yt-dlp e explicito quanto a
  depreciacao.

---

## Pontos de atencao

### Smart App Control bloqueia a execucao

O **Smart App Control** do Windows 11 barra binarios sem assinatura e sem
reputacao, que e exatamente o que um build local de .NET produz. O sintoma:

```
System.IO.FileLoadException: An Application Control policy has blocked this file. (0x800711C7)
```

O build fica verde; falha apenas a **execucao** dos testes e do aplicativo. A
checagem consulta reputacao online, entao o mesmo arquivo pode passar numa hora
e ser barrado noutra.

```
HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy
VerifiedAndReputablePolicyState   0 = desligado   1 = ligado   2 = avaliacao
```

Desligar pela interface do Windows e, oficialmente, irreversivel. Nesta maquina
o recurso esta em **0**.

### Outros

- Assinatura de commit: este repositorio usa a conta pessoal
  fixada no config **local**. A maquina tem outras
  contas, de empresa, que nao devem ser usadas aqui.
- Os testes de integracao exigem rede e as ferramentas baixadas. Estao marcados
  com a categoria `Integration` para poderem ser excluidos.

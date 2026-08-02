# CLAUDE.md

Memoria tecnica do projeto. Este documento deve ser suficiente para entender o
YTDown sem ler todo o codigo.

---

## Visao geral

**YTDown** e um aplicativo desktop para Windows que baixa videos e audios do
YouTube. O publico-alvo e o usuario comum, sem conhecimento tecnico, e a
prioridade e simplicidade de uso.

O aplicativo nao reimplementa nada que o **yt-dlp** e o **FFmpeg** ja resolvem:
ele os orquestra e apresenta o resultado de forma compreensivel.

**Estado atual:** consulta de metadados funcionando de ponta a ponta. Download
ainda nao implementado.

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

- Somente consulta de metadados. Nao ha download.
- O yt-dlp esta congelado na versao fixada. Quando o YouTube o quebrar, o
  aplicativo para de funcionar e o usuario nao tera como resolver.
- Uma playlist colada e reduzida ao video atual, sem qualquer aviso na tela.
- Sem persistencia: nada de historico ou configuracoes.
- Sem tratador global de excecoes na UI.

---

## Roadmap tecnico

| Fatia | Conteudo |
|---|---|
| 1 (feita) | Consulta de metadados de ponta a ponta |
| 2 | Download com progresso agregado e cancelamento com limpeza de `.part` |
| 3 | Selecao de qualidade e formato |
| 4 | Ferramentas em `%LOCALAPPDATA%` e `yt-dlp -U` |
| 5+ | Historico, configuracoes, tema, distribuicao |

### Decisoes adiadas, e por que

- **Codec, remux ou reconversao.** Se o usuario pede "1080p MP4" e o YouTube
  entrega VP9/AV1, o FFmpeg reconverte: minutos de CPU a 100% com a barra de
  progresso parada, e o usuario conclui que travou. As saidas sao restringir a
  `avc1` (teto de 1080p), entregar `.mkv` quando nao der remux, ou aceitar a
  reconversao com aviso honesto. So decidivel vendo os formatos reais. Fatia 3.
- **Progresso.** O yt-dlp baixa video e audio como dois streams sequenciais (a
  barra vai a 100% duas vezes) e depois entra em `Merging formats` sem progresso
  algum. Exige `--progress-template` e um modelo agregado. Fatia 2.
- **Limpeza no cancelamento.** `ProcessRunner` ja encerra a arvore de processos,
  mas ninguem remove `.part`, `.ytdl` e fragmentos orfaos. Fatia 2.

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

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

**Estado atual:** consulta, escolha de qualidade, download em MP4 ou MP3,
progresso, cancelamento, abertura da pasta, ferramentas que se instalam e se
atualizam sozinhas, historico dos ultimos downloads e configuracoes de destino e
qualidade. Sem fila.

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
  YTDown.Infrastructure/  DependencyInjection/ FileSystem/ Processes/ Tools/ YouTube/
  YTDown.UI/              Converters/ Resources/ ViewModels/ Views/
tests/
  YTDown.UnitTests/         espelha a estrutura de src/
  YTDown.IntegrationTests/  exercita o yt-dlp real
  YTDown.ArchitectureTests/ regras de dependencia
docs/
  especificacao.md        brief original, historico, nao mantido
scripts/
  bootstrap-tools.ps1     baixa yt-dlp e FFmpeg
  publish.ps1             gera pasta, zip e instalador em dist/
installer/
  YTDown.iss              script do Inno Setup, compilado pelo publish.ps1
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
   `IDownloadLocationProvider`, que consulta as configuracoes
3. `YtDlpVideoDownloader` cria a pasta de trabalho, monta os argumentos e
   acompanha a saida linha a linha
4. `YtDlpProgressParser` le cada linha; `DownloadProgressAggregator` transforma
   o progresso de cada stream em um unico percentual crescente
5. A pasta de trabalho e removida em `finally`; ao cancelar, nada sobra
6. Dando certo, o `DownloadService` registra o download pelo
   `DownloadHistoryService`, que grava em `history.json`
7. O ViewModel exibe o arquivo e habilita "Abrir pasta", que passa pelo
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

### As duas ferramentas vivem em lugares diferentes

O yt-dlp precisa **se sobrescrever** para se atualizar, e nao consegue quando o
aplicativo esta em Arquivos de Programas. Por isso ele e copiado para
`%LOCALAPPDATA%\YTDown\tools` na primeira execucao, e e essa copia que o
`ManagedToolLocator` prefere.

O FFmpeg **nunca se atualiza sozinho** e fica onde esta, junto do aplicativo:
copiar cem megabytes na primeira execucao seria uma espera visivel sem ganho.

O locator cai para a copia que acompanha a instalacao quando a do perfil ainda
nao existe. E isso que permite a preparacao rodar em paralelo com a tela, sem
que um download logo apos a abertura falhe.

A recopia e decidida comparando a versao declarada em `tools/tools.lock.json`,
que acompanha a instalacao, com um marcador gravado no perfil. Comparar com o
arquivo em disco seria errado: ele costuma estar **mais novo**, por ter se
atualizado sozinho, e sobrescreve-lo seria retroceder.

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

### O historico e um JSON, e guarda pouco

Fica em `%LOCALAPPDATA%\YTDown\history.json`, ao lado da pasta `tools`. Sao
poucas dezenas de registros lidos de uma vez so: um banco embarcado traria
esquema, migracao e mais uma dependencia grande no instalador para um problema
que este aplicativo nao tem. Em texto, o arquivo ainda pode ser conferido a mao.

**Guarda so o que se sabe quando o download termina:** endereco normalizado,
caminho, nome, tamanho, tipo e horario. Titulo e canal ficam de fora porque so
existem se o usuario tiver buscado o video antes, e baixar sem buscar e um
caminho valido. O nome do arquivo ja e o titulo, escrito pelo yt-dlp.

A escrita e feita em arquivo ao lado, seguida de troca, para que uma queda no
meio nao deixe o historico pela metade. Na leitura, JSON invalido devolve lista
vazia: perder a lista e aceitavel, deixar o aplicativo sem abrir nao e. Falha de
acesso ao arquivo tambem nao derruba um download que ja terminou — mas qualquer
excecao que **nao** seja `IOException` ou `UnauthorizedAccessException` continua
subindo, porque ai o defeito e nosso.

O limite e de cinquenta registros. Passando disso, a lista deixa de responder a
pergunta que motiva abri-la e vira algo que ninguem le.

### As configuracoes ficam em memoria depois da primeira leitura

Em `%LOCALAPPDATA%\YTDown\settings.json`, pelo mesmo `JsonFile` do historico.
Guardam a pasta de destino e o teto de qualidade.

Sao lidas do disco **uma vez**. Todo download consulta o destino, e reler a cada
consulta seria pagar por algo que nunca muda sozinha: quem grava e o proprio
aplicativo, que atualiza a copia em memoria ao salvar.

**Nulo e resposta, e nao ausencia:** significa a pasta Downloads e a melhor
qualidade disponivel. Por isso `SettingsDto` tem os dois campos anulaveis e um
`Default` estatico, em vez de valores fixos escritos no DTO.

**O teto de qualidade e limite, nao exigencia.** As qualidades que o video
oferece continuam todas na lista; o teto so decide qual ja vem marcada. Um video
que so exista em 480p continua sendo baixado com o teto em 1080p. Esconder o que
existe transformaria uma preferencia em impedimento.

Falha ao gravar nao impede o usuario de fechar a tela: a escolha vale na mesma
hora e dura ate o aplicativo fechar. Recusar a mudanca por causa de um arquivo
que nao pode ser escrito seria pior que perde-la na proxima abertura.

### O pacote leva o .NET junto, e nao e um arquivo unico

`scripts/publish.ps1` publica **self-contained** para `win-x64`. Pedir que o
publico-alvo instale um runtime antes de baixar um video seria perder a pessoa
na primeira tela. Custa 255 MB em pasta, 113 MB no zip.

**Sem `PublishSingleFile`.** Um executavel unico seria mais bonito de entregar,
mas o WPF precisa extrair bibliotecas nativas para uma pasta temporaria na
primeira execucao — mais lentidao e mais um motivo para o antivirus reclamar. O
destino final e um instalador, onde uma pasta com muitos arquivos e o normal.

**Sem trimming.** O WPF carrega XAML por reflexao, e o recorte remove o que ele
so procura em tempo de execucao. Falharia na abertura, nao no build.

**Com `PublishReadyToRun`.** Troca tamanho por tempo ate a janela aparecer, que
e por onde este publico julga qualidade. Os megabytes a mais pesam pouco ao lado
dos 97 MB do FFmpeg.

O script confere que `tools/` saiu com o yt-dlp, o FFmpeg e o `tools.lock.json`.
Sem isso, a falta so apareceria na maquina de quem recebeu o pacote.

### O instalador nao pede administrador

`installer/YTDown.iss`, compilado pelo Inno Setup 7 a partir do `publish.ps1`,
que passa a versao e a pasta publicada. Saem 88 MB, menos que os 113 MB do zip,
por causa da compressao solida.

**`PrivilegesRequired=lowest`,** com destino em `%LOCALAPPDATA%\Programs\YTDown`.
O aplicativo so escreve em `%LOCALAPPDATA%`, entao pedir UAC cobraria uma
permissao que nada aqui usa — e o publico-alvo costuma nao te-la.

**O desinstalador nao apaga `%LOCALAPPDATA%\YTDown`.** Aquela pasta guarda o
historico, as configuracoes e o yt-dlp que ja se atualizou sozinho: e do
usuario, nao da instalacao. Nao ha `[UninstallDelete]` apontando para la, de
proposito. Ciclo verificado: instalar, executar, desinstalar, e os arquivos do
usuario continuam intactos.

**`AppId` e um GUID fixo** e nunca deve mudar: e por ele que uma instalacao nova
reconhece a anterior e a substitui em vez de duplicar no Painel de Controle.

**`UseSetupLdr=x64`** apresenta o instalador como executavel nativo de 64 bits e
liga ASLR de alta entropia, o que ajuda com politicas que barram binarios sem
reputacao. E marcado como experimental pelo Inno Setup, mas o ciclo completo foi
verificado nesta maquina.

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
- **O historico nao sabe se o arquivo ainda existe.** Registro apagado ou movido
  continua na lista, e "Abrir pasta" leva a pasta sem selecionar nada. Conferir
  a existencia de cinquenta arquivos a cada abertura custaria mais do que
  resolve, e um registro que some sozinho seria pior de entender.
- **Pasta de destino que sumiu volta a Downloads em silencio.** Pendrive
  removido ou unidade de rede fora do ar levam o arquivo para Downloads sem
  aviso na tela. Falhar o download seria pior, mas o usuario pode estranhar.
- O limite de cinquenta registros do historico continua fixo no codigo. E um
  botao de desenvolvedor numa tela feita para quem nao e.
- **Nada e assinado.** Nem o aplicativo, nem o instalador. O Windows avisa que o
  programa e de origem desconhecida, e o Smart App Control pode barra-lo por
  completo. Assinar exige um certificado pago, e a reputacao so se constroi com
  downloads: e o unico problema aqui sem solucao tecnica.
- **Gerar o instalador exige o Inno Setup** instalado na maquina que publica.
  Sem ele o script gera apenas a pasta e o zip, e avisa.
- **Sem icone proprio.** O executavel usa o icone padrao do .NET.
- Sem tratador global de excecoes na UI.

---

## Roadmap tecnico

| Fatia | Conteudo |
|---|---|
| 1 (feita) | Consulta de metadados de ponta a ponta |
| 2 (feita) | Download com progresso agregado, cancelamento e limpeza |
| 3 (feita) | Selecao de qualidade e download somente de audio em MP3 |
| 4 (feita) | Ferramentas em `%LOCALAPPDATA%` e atualizacao automatica do yt-dlp |
| 5 | Runtime JavaScript, se o 403 e a verificacao anti-robo reincidirem |
| 6 (feita) | Historico dos ultimos downloads, em janela propria |
| 7 (feita) | Configuracoes de destino e de qualidade padrao |
| 8 (feita) | Pacote self-contained, zip e instalador |
| 9+ | Fila, tema, assinatura de codigo |

### Decisoes ja tomadas

- **Compatibilidade antes de qualidade.** Preferir H.264 com AAC permite juntar
  sem reconverter. A alternativa seria entregar 4K em VP9/AV1, mas ou o arquivo
  sai em MKV, que pode nao abrir onde o usuario espera, ou o FFmpeg reconverte,
  gastando minutos de CPU a 100% com a barra parada. Para o publico deste
  aplicativo, um MP4 previsivel vale mais que resolucao maior.
- **Pasta Downloads, sem perguntar.** Um seletor de pasta a cada download e
  atrito exatamente onde o publico-alvo trava. Quem quer outro destino escolhe
  uma vez nas configuracoes, e nao a cada download.
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
o recurso esta em **0**, e o dono consegue liga-lo e desliga-lo pelo registro.

**Renomear o assembly zera a reputacao.** Ao ganhar metadados de distribuicao, o
`AssemblyName` passou de `YTDown.UI` para `YTDown`. Para o Smart App Control o
resultado nao e um arquivo conhecido que mudou, e sim um binario inedito, sem
assinatura e sem historico: `YTDown.dll` foi bloqueado logo depois, uma vez, e o
aplicativo morreu antes de a janela existir. Esperar isso a cada mudanca de nome
ou de versao, aqui e na maquina de quem receber o instalador.

Como a checagem consulta reputacao online, o sintoma e **intermitente e nao
reproduzivel**: nao vale procurar defeito no codigo antes de descartar esta
hipotese. Nao ha solucao tecnica sem assinar o executavel.

### O YouTube bloqueia depois de muitos downloads seguidos

Rodar os testes de integracao repetidas vezes em pouco tempo faz o YouTube
recusar o endereco de rede inteiro:

```
ERROR: [youtube] <id>: Sign in to confirm you're not a bot.
Use --cookies-from-browser or --cookies for the authentication.
```

Nao e defeito do aplicativo: acontece igual chamando o yt-dlp direto pela linha
de comando. Passa sozinho depois de um tempo. A suite de integracao faz quatro
downloads reais por execucao, entao **nao a rode em laco**; use
`--filter Category!=Integration` durante o desenvolvimento normal.

Esse erro e classificado como `ErrorCode.BotCheckRequired`, e a mensagem ao
usuario pede espera em vez de nova tentativa, porque insistir prolonga o
bloqueio.

### Outros

- Assinatura de commit: este repositorio usa a conta pessoal
  fixada no config **local**. A maquina tem outras
  contas, de empresa, que nao devem ser usadas aqui.
- Os testes de integracao exigem rede e as ferramentas baixadas. Estao marcados
  com a categoria `Integration` para poderem ser excluidos.

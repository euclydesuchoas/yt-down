# CLAUDE.md

Memória técnica do projeto. Este documento deve ser suficiente para entender o
YTDown sem ler todo o código, e é a **documentação viva**: onde ele divergir de
[`docs/especificacao.md`](docs/especificacao.md), que é o brief original
preservado sem manutenção, este documento prevalece.

---

## Visão geral

**YTDown** é um aplicativo desktop para Windows que baixa vídeos e áudios do
YouTube. O público-alvo é o usuário comum, sem conhecimento técnico, e a
prioridade é simplicidade de uso.

O aplicativo não reimplementa nada que o **yt-dlp** e o **FFmpeg** já resolvem:
ele os orquestra e apresenta o resultado de forma compreensível.

**Estado atual:** consulta, escolha de qualidade, download em MP4 ou MP3,
progresso, cancelamento, abertura da pasta, ferramentas que se instalam e se
atualizam sozinhas, histórico dos últimos downloads e configurações de destino e
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
| `YTDown.Domain` | Entidades, value objects, exceções de domínio | Depender de qualquer coisa |
| `YTDown.Application` | Serviços, contratos, DTOs, `Result` | Conhecer WPF, yt-dlp ou Infrastructure |
| `YTDown.Infrastructure` | Processos, ferramentas externas, sistema de arquivos | Conhecer a UI |
| `YTDown.UI` | Views, ViewModels, converters, composition root | Regra de negócio ou `Process` |

As interfaces de integração (`IVideoMetadataProvider`) são **declaradas na
Application** e **implementadas na Infrastructure**. É isso que mantém a
Application sem qualquer conhecimento de yt-dlp.

Sete testes em `tests/YTDown.ArchitectureTests` fixam essas regras, incluindo um
controle positivo que garante que a detecção de dependências está funcionando.

---

## Estrutura de pastas

```
src/
  YTDown.Domain/          Exceptions/ ValueObjects/   (VideoUrl, OutputFileName)
  YTDown.Application/     Common/ DTOs/ DependencyInjection/ Interfaces/ Services/
  YTDown.Infrastructure/  DependencyInjection/ FileSystem/ Processes/ Tools/ YouTube/
  YTDown.UI/              Converters/ Resources/ ViewModels/ Views/
tests/
  YTDown.UnitTests/         espelha a estrutura de src/
  YTDown.IntegrationTests/  exercita o yt-dlp real
  YTDown.ArchitectureTests/ regras de dependência
docs/
  especificacao.md        brief original, histórico, não mantido
scripts/
  bootstrap-tools.ps1     baixa yt-dlp e FFmpeg
  build-icon.ps1          desenha assets/ytdown.ico
  publish.ps1             gera pasta, zip e instalador em dist/
installer/
  YTDown.iss              script do Inno Setup, compilado pelo publish.ps1
assets/
  ytdown.ico              ícone do aplicativo e do instalador
tools/
  tools.lock.json         versões fixadas + SHA256
  yt-dlp.exe, ffmpeg.exe  não versionados
```

---

## Fluxo da aplicação

Consulta de um vídeo:

1. `MainViewModel.SearchAsync` recebe o texto colado pelo usuário
2. `VideoInfoService` tenta criar um `VideoUrl`; entrada inválida falha aqui,
   sem iniciar processo externo
3. `YtDlpMetadataProvider` localiza o executável via `IToolLocator`
4. `ProcessRunner` executa `yt-dlp --dump-single-json --no-playlist <url>`
5. `YtDlpVideoInfoParser` lê a resposta, ou `YtDlpErrorClassifier` classifica o
   erro pela mensagem do stderr
6. O ViewModel exibe o vídeo ou traduz o `ErrorCode` em frase pelo
   `ErrorMessages`

Download de um vídeo, que **só fica disponível depois de uma busca
bem-sucedida**:

1. `MainViewModel.DownloadAsync` cria o `Progress<T>` na linha da interface, de
   modo que cada atualização volte para ela sozinha
2. `DownloadService` valida a URL e pergunta o destino ao
   `IDownloadLocationProvider`, que consulta as configurações
3. `YtDlpVideoDownloader` cria a pasta de trabalho, monta os argumentos e
   acompanha a saída linha a linha
4. `YtDlpProgressParser` lê cada linha; `DownloadProgressAggregator` transforma
   o progresso de cada stream em um único percentual crescente
5. A pasta de trabalho é removida em `finally`; ao cancelar, nada sobra
6. Dando certo, o `DownloadService` registra o download pelo
   `DownloadHistoryService`, que grava em `history.json`
7. O ViewModel exibe o arquivo e habilita "Abrir pasta", que passa pelo
   `IFileExplorer` porque a apresentação não pode iniciar processos

---

## Tecnologias

.NET 10, WPF, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.DependencyInjection 10.0.10.

Testes: xUnit 2.9.3, FluentAssertions 7.2.2, Moq 4.20.72, NetArchTest.eNhancedEdition 1.4.5.

Versões centralizadas em `Directory.Packages.props` (Central Package Management).

---

## Decisões arquiteturais

### Result em vez de exceção para falhas esperadas

Vídeo removido, vídeo privado e queda de rede são desfechos normais deste
aplicativo. Trafegam como `Result<T>` com um `ErrorCode` tipado. Exceção fica
reservada a defeito de programação.

A saída bruta do yt-dlp nunca chega à tela: fica em `Result.Diagnostics`, apenas
para depuração.

### VideoUrl normaliza, o resto do sistema não pensa em formato

O usuário cola a mesma referência de muitas formas: barra de endereços,
compartilhar, aplicativo móvel, YouTube Music, Shorts, live, embed. `VideoUrl`
reduz tudo ao identificador e descarta `list`, `t`, `si`, `index` e `pp`.

**Playlist é ignorada em silêncio.** Quem cola a URL da barra de endereços
durante uma playlist quer aquele vídeo, não os duzentos seguintes.

**Identificador solto é recusado.** Qualquer palavra de 11 caracteres válidos
(`hello_world`, por exemplo) passaria na verificação, e o erro só apareceria
depois como uma falha confusa do yt-dlp.

### O projeto de testes unitários referencia a Infrastructure

O código mais frágil do projeto é a leitura da saída do yt-dlp, e ela vive na
Infrastructure. Deixá-la fora dos testes unitários seria testar tudo menos o que
costuma quebrar. O parser e o classificador são classes sem estado, testadas
contra respostas reais gravadas em `tests/YTDown.UnitTests/Infrastructure/YouTube/Fixtures/`.

### Binários externos fora do Git

`yt-dlp.exe` e `ffmpeg.exe` somam 120 MB e mudam com frequência. Cada
atualização versionada seria peso permanente e irremovível no histórico. Ficam
em `.gitignore`, com versões e SHA256 fixados em `tools/tools.lock.json` e
baixados por `scripts/bootstrap-tools.ps1`.

O `YTDown.UI.csproj` referencia as ferramentas com `Condition="Exists(...)"`,
para que um clone limpo compile mesmo sem elas.

### As duas ferramentas vivem em lugares diferentes

O yt-dlp precisa **se sobrescrever** para se atualizar, e não consegue quando o
aplicativo está em Arquivos de Programas. Por isso ele é copiado para
`%LOCALAPPDATA%\YTDown\tools` na primeira execução, e é essa cópia que o
`ManagedToolLocator` prefere.

O FFmpeg **nunca se atualiza sozinho** e fica onde está, junto do aplicativo:
copiar cem megabytes na primeira execução seria uma espera visível sem ganho.

O locator cai para a cópia que acompanha a instalação quando a do perfil ainda
não existe. É isso que permite a preparação rodar em paralelo com a tela, sem
que um download logo após a abertura falhe.

A recópia é decidida comparando a versão declarada em `tools/tools.lock.json`,
que acompanha a instalação, com um marcador gravado no perfil. Comparar com o
arquivo em disco seria errado: ele costuma estar **mais novo**, por ter se
atualizado sozinho, e sobrescrevê-lo seria retroceder.

### Como o yt-dlp é conduzido durante o download

Três detalhes desta integração custaram tempo e não devem ser desfeitos:

- **O caminho final é pedido em JSON**, com `--print "after_move:FINAL|%(filepath)j"`.
  Ao escrever em um pipe, o yt-dlp **descarta silenciosamente tudo o que não for
  ASCII**: o vídeo de referência, de título japonês, chegava como ` EDED.mp4`,
  um arquivo que não existe em disco, embora o arquivo real estivesse correto.
  Em JSON os caracteres viajam como escapes. Definir `PYTHONIOENCODING` não
  resolve sozinho, mas fica, por ser a outra metade da leitura em UTF-8.
- **`--print` implica `--quiet`**, o que silencia o progresso por completo.
  `--progress` o traz de volta.
- **Os arquivos intermediários vão para uma pasta própria**, via
  `--paths temp:`, criada dentro do destino para que a mudança final ocorra no
  mesmo volume. A limpeza é apagar a pasta inteira, em `finally`, sem depender
  de adivinhar nomes de `.part`.

O formato do progresso é imposto por nós com `--progress-template`, e não lido
do texto que a ferramenta mostra ao usuário, que muda entre versões. Vídeo e
áudio se distinguem porque o yt-dlp informa o codec de vídeo como `none`
enquanto baixa o áudio.

### Faixas do progresso

Vídeo ocupa 0 a 90%, áudio de 90 a 97%, e a junção fica em 97% até concluir.
As faixas vêm da proporção real medida: no vídeo de referência o áudio é pouco
mais de 7% dos bytes. O percentual nunca retrocede, porque os dois streams são
baixados em sequência e cada um começa do zero.

### O histórico é um JSON, e guarda pouco

Fica em `%LOCALAPPDATA%\YTDown\history.json`, ao lado da pasta `tools`. São
poucas dezenas de registros lidos de uma vez só: um banco embarcado traria
esquema, migração e mais uma dependência grande no instalador para um problema
que este aplicativo não tem. Em texto, o arquivo ainda pode ser conferido à mão.

**Guarda só o que se sabe quando o download termina:** endereço normalizado,
caminho, nome, tamanho, tipo e horário. Título e canal ficam de fora porque só
existem se o usuário tiver buscado o vídeo antes, e baixar sem buscar é um
caminho válido. O nome do arquivo já é o título, escrito pelo yt-dlp.

A escrita é feita em arquivo ao lado, seguida de troca, para que uma queda no
meio não deixe o histórico pela metade. Na leitura, JSON invalido devolve lista
vazia: perder a lista é aceitável, deixar o aplicativo sem abrir não é. Falha de
acesso ao arquivo também não derruba um download que já terminou — mas qualquer
exceção que **não** seja `IOException` ou `UnauthorizedAccessException` continua
subindo, porque aí o defeito é nosso.

O limite é de cinquenta registros. Passando disso, a lista deixa de responder a
pergunta que motiva abri-la e vira algo que ninguém lê.

### Buscar é um passo obrigatório, e não um atalho

`DownloadCommand` exige `Video is not null`. Antes dava para baixar direto, o
que parecia economizar um clique mas escondia duas coisas: **qual qualidade** o
usuário ia receber, porque a lista só existe após a consulta e o download
aplicava o teto das configurações em silêncio; e **se o endereço era mesmo o
vídeo pretendido**.

O clique a mais custa pouco: a consulta de metadados acontece de qualquer forma
durante o download. O que muda é o usuário ver o resultado dela antes.

**Mexer no endereço descarta o vídeo encontrado** (`OnUrlChanged`). Sem isso
daria para buscar um vídeo, colar outro endereço e baixar o segundo com o
primeiro ainda na tela.

A ordem das linhas da `MainWindow` conta essa história: endereço, vídeo
encontrado, escolhas sobre ele, resultado. O botão Baixar vive na linha das
escolhas, junto das decisões que executa.

### O yt-dlp mente quando o arquivo já existe

**Medido, não suposto.** Pedindo um vídeo com o nome de um arquivo que já está na
pasta, o yt-dlp **pula o download, termina com código zero e imprime o caminho
final** como se tivesse baixado. O aplicativo mostraria "Download concluído", o
histórico registraria, e o arquivo seria o antigo. Ele também não acrescenta
sufixo sozinho.

Por isso `YtDlpOutputTemplate` escolhe um nome livre **antes** de chamar a
ferramenta: `Musica`, `Musica (2)`, `Musica (3)`, como faz o navegador.
Sobrescrever resolveria a mentira, mas apagaria em silêncio um arquivo do
usuário.

Isso já valia para nomes vindos do título — dois vídeos de título igual, ou que
truncam igual em cem caracteres — só que era raro. Com nome escolhido à mão,
"Música" e "Ao Vivo" viram rotina.

**O `%` abre um campo no template de saída.** Um nome com "100%" seria lido como
instrução, e "%(title)s" viraria o título do vídeo. Duplicar (`%%`) devolve o
caractere literal; verificado com `--simulate --print filename`.

### A pasta tem dois níveis: padrão nas configurações, exceção no download

O destino sai das configurações, mas cada download pode apontar outro pelo campo
`DestinationDirectory` de `DownloadOptionsDto`. Quem organiza arquivos por
assunto — um álbum por cantor, por exemplo — troca de pasta o tempo todo, e
mandar essa pessoa às configurações a cada download seria atrito no lugar errado.

**A escolha explícita não vira outra coisa em silêncio.** Pasta que sumiu falha
com `ErrorCode.DestinationUnavailable`. O silêncio se justifica para o destino
padrão, configurado uma vez e esquecido, mas não para uma pasta que o usuário
acabou de apontar: o arquivo iria parar longe dali e ele só descobriria ao
procurar.

**As pastas recentes saem do histórico**, que já guarda o caminho completo de
cada arquivo. Guardar essa lista em separado seria manter uma segunda verdade
sobre o mesmo fato. A exceção é a pasta recém-apontada no seletor, que entra na
lista antes de existir no histórico — este só registra downloads concluídos, e
sem isso ela sumiria no instante seguinte ao de ser escolhida.

**A escolha dura a sessão**, e não um download só. Separar doze músicas em uma
pasta custaria doze idas ao seletor.

### As configurações ficam em memória depois da primeira leitura

Em `%LOCALAPPDATA%\YTDown\settings.json`, pelo mesmo `JsonFile` do histórico.
Guardam a pasta de destino e o teto de qualidade.

São lidas do disco **uma vez**. Todo download consulta o destino, e reler a cada
consulta seria pagar por algo que nunca muda sozinha: quem grava é o próprio
aplicativo, que atualiza a cópia em memória ao salvar.

**Nulo é resposta, e não ausência:** significa a pasta Downloads e a melhor
qualidade disponível. Por isso `SettingsDto` tem os dois campos anuláveis e um
`Default` estático, em vez de valores fixos escritos no DTO.

**O teto de qualidade é limite, não exigência.** As qualidades que o vídeo
oferece continuam todas na lista; o teto só decide qual já vem marcada. Um vídeo
que só exista em 480p continua sendo baixado com o teto em 1080p. Esconder o que
existe transformaria uma preferência em impedimento.

Falha ao gravar não impede o usuário de fechar a tela: a escolha vale na mesma
hora e dura até o aplicativo fechar. Recusar a mudança por causa de um arquivo
que não pode ser escrito seria pior que perdê-la na próxima abertura.

### O pacote leva o .NET junto, e não é um arquivo único

`scripts/publish.ps1` publica **self-contained** para `win-x64`. Pedir que o
público-alvo instale um runtime antes de baixar um vídeo seria perder a pessoa
na primeira tela. Custa 255 MB em pasta, 113 MB no zip.

**Sem `PublishSingleFile`.** Um executável único seria mais bonito de entregar,
mas o WPF precisa extrair bibliotecas nativas para uma pasta temporaria na
primeira execução — mais lentidão e mais um motivo para o antivírus reclamar. O
destino final é um instalador, onde uma pasta com muitos arquivos é o normal.

**Sem trimming.** O WPF carrega XAML por reflexão, e o recorte remove o que ele
só procura em tempo de execução. Falharia na abertura, não no build.

**Com `PublishReadyToRun`.** Troca tamanho por tempo até a janela aparecer, que
é por onde este público julga qualidade. Os megabytes a mais pesam pouco ao lado
dos 97 MB do FFmpeg.

O script confere que `tools/` saiu com o yt-dlp, o FFmpeg e o `tools.lock.json`.
Sem isso, a falta só apareceria na máquina de quem recebeu o pacote.

### O ícone é desenhado por código

`scripts/build-icon.ps1` gera `assets/ytdown.ico`. Desenhar por código em vez de
editar em uma ferramenta gráfica segue a mesma lógica das ferramentas externas:
o resultado pode ser refeito e ajustado sem depender de nada instalado, e o
binário versionado deixa de ser um arquivo sem procedência. O script reproduz o
`.ico` byte a byte.

**Cada tamanho é desenhado no seu próprio tamanho**, e não reduzido do maior, que
é o que deixa ícone borrado nas dimensões pequenas. São sete: 16, 24, 32, 48, 64,
128 e 256. Até 48 vão como DIB, que qualquer contexto do shell lê; acima disso
como PNG, para o arquivo não inchar.

Um `ApplicationIcon` no `csproj` resolve executável, janela e barra de tarefas de
uma vez, porque o WPF usa o ícone do próprio executável. O instalador precisa do
seu, pelo `SetupIconFile`: sem ele o `setup.exe` sai com o ícone genérico do Inno
Setup, e ele é o primeiro arquivo que a pessoa vê.

### O instalador não pede administrador

`installer/YTDown.iss`, compilado pelo Inno Setup 7 a partir do `publish.ps1`,
que passa a versão e a pasta publicada. Saem 88 MB, menos que os 113 MB do zip,
por causa da compressão sólida.

**`PrivilegesRequired=lowest`,** com destino em `%LOCALAPPDATA%\Programs\YTDown`.
O aplicativo só escreve em `%LOCALAPPDATA%`, então pedir UAC cobraria uma
permissão que nada aqui usa — e o público-alvo costuma não tê-la.

**O desinstalador não apaga `%LOCALAPPDATA%\YTDown`.** Aquela pasta guarda o
histórico, as configurações e o yt-dlp que já se atualizou sozinho: é do
usuário, não da instalação. Não há `[UninstallDelete]` apontando para lá, de
propósito. Ciclo verificado: instalar, executar, desinstalar, e os arquivos do
usuário continuam intactos.

**`AppId` é um GUID fixo** e nunca deve mudar: é por ele que uma instalação nova
reconhece a anterior e a substitui em vez de duplicar no Painel de Controle.

**`UseSetupLdr=x64`** apresenta o instalador como executável nativo de 64 bits e
liga ASLR de alta entropia, o que ajuda com políticas que barram binários sem
reputação. É marcado como experimental pelo Inno Setup, mas o ciclo completo foi
verificado nesta máquina.

### FluentAssertions fixado em 7.x

A partir da 8.0.0 o pacote exige licença comercial para uso não open source. A
7.2.2 é a última sob Apache 2.0. **Não atualizar sem decisão explícita.**

`NetArchTest.eNhancedEdition` substitui o `NetArchTest.Rules` original, sem
manutenção ativa. A API do fork difere: não existe `HaveDependencyOn` no
singular, apenas `HaveDependencyOnAny` e `HaveDependencyOnAll`, e o resultado
expõe `FailingTypes` (com `FullName` e `Explanation`), não `FailingTypeNames`.

---

## Convenções

- **Identificadores em inglês; comentários, documentação e mensagens ao usuário
  em português, com acentuação.** Português sem acento é português errado, e o
  repositório é público. Nomes de arquivo usados como dado de teste
  (`video.mp4`, `musica.mp3`) continuam em ASCII de propósito: são fixtures, não
  texto.
- **Os `.ps1` e o `.iss` levam BOM UTF-8.** Sem ele o Windows PowerShell 5.1 e o
  Inno Setup leem o arquivo na code page ANSI, e o acento chega mutilado na tela
  e no instalador. Os `.json` ficam **sem** BOM, que leitor estrito recusa; o
  C# e o XAML dispensam, porque já assumem UTF-8.
- Nomes de teste seguem `Metodo_Cenario_ResultadoEsperado`.
- Comentário explica **por que**, nunca **o que**. Código que precisa de
  comentário para dizer o que faz deve ser reescrito.
- `async`/`await` com `CancellationToken` em toda operação que cruza processo ou
  rede.
- Injeção de dependência sempre; cada camada expõe seu próprio
  `Add<Camada>()`.
- Conventional Commits, um assunto por commit.

---

## Ferramentas externas

| Ferramenta | Versão fixada | Uso |
|---|---|---|
| yt-dlp | 2026.07.04 | metadados e download |
| FFmpeg | 8.1.2-essentials | junção e conversão |

Vídeo de referência para testes: `https://www.youtube.com/watch?v=UKcJqQqiXq0`
(título em japonês, o que também exercita a codificação UTF-8 de ponta a ponta).

---

## Limitações conhecidas

- **Teto de 1080p**, consequência de preferir H.264 para não reconverter. As
  qualidades oferecidas são as reais do vídeo, mas apenas as que existem em
  H.264: um vídeo em 4K aparece com 1080p no máximo.
- **Nenhum runtime JavaScript embarcado.** O yt-dlp avisa que a extração sem um
  runtime está depreciada e que alguns formatos podem faltar. Embarcar um
  runtime significa mais uma dependência grande no instalador. Decisão adiada
  conscientemente, para a fatia das ferramentas.

  **Já houve sintoma:** um teste de integração falhou uma vez com
  `ERROR: unable to download video data: HTTP Error 403: Forbidden` e passou na
  execução seguinte, sem qualquer alteração. O 403 nesse ponto costuma indicar
  URL cuja assinatura precisaria ser decodificada por JavaScript. Como é
  intermitente, não há diagnóstico conclusivo: se voltar com frequência, esta é
  a primeira hipótese a investigar.
- O yt-dlp está congelado na versão fixada. Quando o YouTube o quebrar, o
  aplicativo para de funcionar e o usuário não terá como resolver.
- Uma playlist colada é reduzida ao vídeo atual, sem qualquer aviso na tela.
- Apenas um download por vez, sem fila.
- **O histórico não sabe se o arquivo ainda existe.** Registro apagado ou movido
  continua na lista, e "Abrir pasta" leva à pasta sem selecionar nada. Conferir
  a existência de cinquenta arquivos a cada abertura custaria mais do que
  resolve, e um registro que some sozinho seria pior de entender.
- **Pasta de destino que sumiu volta a Downloads em silêncio.** Pendrive
  removido ou unidade de rede fora do ar levam o arquivo para Downloads sem
  aviso na tela. Falhar o download seria pior, mas o usuário pode estranhar.
- O limite de cinquenta registros do histórico continua fixo no código. É um
  botão de desenvolvedor numa tela feita para quem não é.
- **Nada é assinado.** Nem o aplicativo, nem o instalador. O Windows avisa que o
  programa é de origem desconhecida, e o Smart App Control pode barrá-lo por
  completo. Assinar exige um certificado pago, e a reputação só se constrói com
  downloads: é o único problema aqui sem solução técnica.
- **Gerar o instalador exige o Inno Setup** instalado na máquina que publica.
  Sem ele o script gera apenas a pasta e o zip, e avisa.
- Sem tratador global de exceções na UI.

---

## Roadmap técnico

| Fatia | Conteúdo |
|---|---|
| 1 (feita) | Consulta de metadados de ponta a ponta |
| 2 (feita) | Download com progresso agregado, cancelamento e limpeza |
| 3 (feita) | Seleção de qualidade e download somente de áudio em MP3 |
| 4 (feita) | Ferramentas em `%LOCALAPPDATA%` e atualização automática do yt-dlp |
| 5 | Runtime JavaScript, se o 403 e a verificação anti-robô reincidirem |
| 6 (feita) | Histórico dos últimos downloads, em janela própria |
| 7 (feita) | Configurações de destino e de qualidade padrão |
| 8 (feita) | Pacote self-contained, zip e instalador |
| 9+ | Fila, tema, assinatura de código |

### Decisões já tomadas

- **Compatibilidade antes de qualidade.** Preferir H.264 com AAC permite juntar
  sem reconverter. A alternativa seria entregar 4K em VP9/AV1, mas ou o arquivo
  sai em MKV, que pode não abrir onde o usuário espera, ou o FFmpeg reconverte,
  gastando minutos de CPU a 100% com a barra parada. Para o público deste
  aplicativo, um MP4 previsível vale mais que resolução maior.
- **Pasta Downloads, sem perguntar.** Um seletor de pasta a cada download é
  atrito exatamente onde o público-alvo trava. Quem quer outro destino escolhe
  uma vez nas configurações, e não a cada download.
- **Um download por vez.** Downloads simultâneos dividem a banda, embaralham o
  progresso e tornam cancelamento e limpeza bem mais difíceis.

### Decisão adiada

- **Runtime JavaScript.** Ver limitações conhecidas. Não há urgência enquanto os
  formatos continuarem disponíveis, mas o aviso do yt-dlp é explícito quanto à
  depreciação.

---

## Pontos de atenção

### Smart App Control bloqueia a execução

O **Smart App Control** do Windows 11 barra binários sem assinatura e sem
reputação, que é exatamente o que um build local de .NET produz. O sintoma:

```
System.IO.FileLoadException: An Application Control policy has blocked this file. (0x800711C7)
```

O build fica verde; falha apenas a **execução** dos testes e do aplicativo. A
checagem consulta reputação online, então o mesmo arquivo pode passar numa hora
e ser barrado noutra.

```
HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy
VerifiedAndReputablePolicyState   0 = desligado   1 = ligado   2 = avaliação
```

Desligar pela interface do Windows é, oficialmente, irreversível. Nesta máquina
o recurso está em **0**, e o dono consegue ligá-lo e desligá-lo pelo registro.

**Renomear o assembly zera a reputação.** Ao ganhar metadados de distribuição, o
`AssemblyName` passou de `YTDown.UI` para `YTDown`. Para o Smart App Control o
resultado não é um arquivo conhecido que mudou, e sim um binário inédito, sem
assinatura e sem histórico: `YTDown.dll` foi bloqueado logo depois, uma vez, e o
aplicativo morreu antes de a janela existir. Esperar isso a cada mudança de nome
ou de versão, aqui e na máquina de quem receber o instalador.

Como a checagem consulta reputação online, o sintoma é **intermitente e não
reproduzível**: não vale procurar defeito no código antes de descartar esta
hipótese. Não há solução técnica sem assinar o executável.

### O YouTube bloqueia depois de muitos downloads seguidos

Rodar os testes de integração repetidas vezes em pouco tempo faz o YouTube
recusar o endereço de rede inteiro:

```
ERROR: [youtube] <id>: Sign in to confirm you're not a bot.
Use --cookies-from-browser or --cookies for the authentication.
```

Não é defeito do aplicativo: acontece igual chamando o yt-dlp direto pela linha
de comando. Passa sozinho depois de um tempo. A suite de integração faz quatro
downloads reais por execução, então **não a rode em laço**; use
`--filter Category!=Integration` durante o desenvolvimento normal.

Esse erro é classificado como `ErrorCode.BotCheckRequired`, e a mensagem ao
usuário pede espera em vez de nova tentativa, porque insistir prolonga o
bloqueio.

### Outros

- Assinatura de commit: este repositório usa a conta pessoal
  fixada no config **local**. A máquina tem outras
  contas, de empresa, que não devem ser usadas aqui.
- Os testes de integração exigem rede e as ferramentas baixadas. Estão marcados
  com a categoria `Integration` para poderem ser excluídos.

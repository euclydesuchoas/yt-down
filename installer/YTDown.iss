; Instalador do YTDown, para Inno Setup 7.
;
; Nao e compilado a mao: scripts/publish.ps1 publica o aplicativo e chama o
; ISCC passando a versao e a pasta publicada. Compilar direto daqui exige
; informar as duas coisas:
;
;   ISCC.exe /DAppVersion=0.1.0 /DPublishedDirectory=..\dist\YTDown-0.1.0-win-x64 YTDown.iss

#ifndef AppVersion
  #error Defina AppVersion. Use scripts/publish.ps1 -Installer.
#endif

#ifndef PublishedDirectory
  #error Defina PublishedDirectory. Use scripts/publish.ps1 -Installer.
#endif

#ifndef OutputDirectory
  #define OutputDirectory "..\dist"
#endif

[Setup]
; Identidade do aplicativo perante o Windows. Nunca deve mudar: e por ela que
; uma instalacao nova reconhece a anterior e a substitui em vez de duplicar.
AppId={{1460EFC7-6901-49D7-8ED3-94B1AE465A91}
AppName=YTDown
AppVersion={#AppVersion}
AppVerName=YTDown {#AppVersion}
AppPublisher=Euclydes Uchoas
VersionInfoVersion={#AppVersion}

; Instalacao por usuario, sem UAC. O aplicativo so escreve em %LOCALAPPDATA%,
; entao pedir administrador cobraria do usuario uma permissao que nada aqui usa.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\YTDown
DefaultGroupName=YTDown

; O aplicativo publicado e win-x64, e as duas ferramentas externas tambem.
ArchitecturesAllowed=x64compatible

; O carregador de 64 bits apresenta o instalador como executavel nativo e
; habilita ASLR de alta entropia, o que ajuda com politicas que barram binarios
; sem reputacao.
UseSetupLdr=x64

; O pacote passa de 250 MB, quase todo em executaveis. Compressao solida no
; maximo demora para compilar, mas isso acontece uma vez por publicacao.
Compression=lzma2/max
SolidCompression=yes

OutputDir={#OutputDirectory}
OutputBaseFilename=YTDown-{#AppVersion}-setup

; O publico-alvo nao tem o que decidir aqui: sem componentes, sem escolha de
; pasta do menu Iniciar, sem tela de boas-vindas para clicar em Avancar.
DisableProgramGroupPage=yes
DisableWelcomePage=yes
ShowLanguageDialog=no
WizardStyle=modern
UninstallDisplayName=YTDown
UninstallDisplayIcon={app}\YTDown.exe

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
; A pasta publicada inteira, incluindo tools\ com o yt-dlp e o FFmpeg. Sem eles
; o aplicativo abre e falha no primeiro download.
Source: "{#PublishedDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\YTDown"; Filename: "{app}\YTDown.exe"
Name: "{autodesktop}\YTDown"; Filename: "{app}\YTDown.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\YTDown.exe"; Description: "{cm:LaunchProgram,YTDown}"; Flags: nowait postinstall skipifsilent

; Nada de [UninstallDelete] apontando para %LOCALAPPDATA%\YTDown. Aquela pasta
; guarda o historico, as configuracoes e o yt-dlp que ja se atualizou sozinho:
; e do usuario, nao da instalacao. Desinstalar e reinstalar preserva tudo.

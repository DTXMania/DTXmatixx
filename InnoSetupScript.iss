; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppVersion "063"

#define MyAppName "DTXMania2"
#define MyAppFolderName "DTXMania2"
#define MyAppPublisher "FROM / Kenji Yamazaki"
#define MyAppExeName "DTXMania2.exe"
#define MyAppBin "publish"
#define MyEditorName "SSTFEditor"
#define MyEditorExeName "SSTFEditor.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C47F4435-C3B5-43A2-8E5A-607C2FE6CB39}

; このインストーラを64bitインストールモードで起動。
 ArchitecturesInstallIn64BitMode=x64
; このスクリプトの基本となるフォルダ。このスクリプトファイルからの相対パス。
SourceDir=.
; ビルドしたインストーラ(exe)の出力先。SourceDir からの相対パス。
OutputDir=Archives
; setup ファイル名。
OutputBaseFilename=dtxmania2_{#MyAppVersion}_setup
; アプリ名。
AppName={#MyAppName}
; アプリのバージョン。（コントロールパネル用）
AppVersion={#MyAppVersion}
; アプリ名＋バージョン。
AppVerName={#MyAppName} Release {#MyAppVersion}
; アプリの配布元。(コントロールパネル用）
AppPublisher={#MyAppPublisher}
; アプリの配布元WebサイトのURL。（コントロールパネル用）
;AppPublisherURL=
; 使用許諾
;LicenseFile=Licence.txt
; パスワード
;Password=
; インストールの実行前に表示する情報。
;InfoBeforeFile=
; ユーザ情報の入力画面を出す？
UserInfoPage=no
; インストール先の選択画面を出さない？
DisableDirPage=no
; 既定のインストール先フォルダパス。
DefaultDirName={commonpf}\{#MyAppFolderName}
; スタートメニューのプログラムグループ名の設定画面を出さない？
DisableProgramGroupPage=no
; 既定のスタートメニューのグループ名。
DefaultGroupName={#MyAppFolderName}
; 「インストールの準備ができました」画面を出さない？
DisableReadyPage=no
; インストールの実行後に表示する情報。
;InfoAfterFile=
; 「セットアップが終了しました」画面を出さない？
DisableFinishedPage=no
; 圧縮形式。
Compression=lzma2/max
SolidCompression=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
; デスクトップにショートカットを作成するタスクの定義。後述する[Icons]セクションで使う。
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; {app} は exe のおかれるフォルダ。（例："C:\Program Files\<アプリ名>"）
Source: "{#MyAppBin}\*.*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

; {userappdata} → インストールユーザの AppData フォルダ。（例："C:\users\<ユーザ名>\AppData\Roaming\<アプリ名>"）
;Source: "{#MyAppBin}\appdata_default\*"; DestDir: "{userappdata}\{#MyAppFolderName}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[UninstallDelete]
; アンインストール時は、インストール時にコピーしたファイルだけが削除され、その後に作られたファイルは保持される。
; それらを削除したい場合はここに記載する（注意：ワイルドカードは使わないこと。危ないので）。

[Icons]
; プログラムグループへのショートカット。
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{#MyEditorName}"; Filename: "{app}\{#MyEditorExeName}"
; デスクトップへのショートカット（インストール時にユーザから指定された場合のみ）。
Name: "{commondesktop}\{#MyAppFolderName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{commondesktop}\{#MyEditorName}"; Filename: "{app}\{#MyEditorExeName}"; Tasks: desktopicon

[Run]
; インストール終了後にアプリを起動する場合のタスクの定義。
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

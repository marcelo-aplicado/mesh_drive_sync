# Mesh Drive Sync

Cliente de sincronização bidirecional para compartilhamentos WebDAV do MeshCentral no Windows.

O **Mesh Drive Sync** sincroniza arquivos e pastas entre o Mesh Drive e uma pasta local, oferecendo uma experiência semelhante a OneDrive, Google Drive e Dropbox, mas utilizando a infraestrutura WebDAV do MeshCentral.

## Principais recursos

### Sincronização bidirecional

- Upload automático de arquivos criados ou modificados localmente.
- Download automático de arquivos criados ou modificados no WebDAV.
- Sincronização acionada por eventos do sistema de arquivos.
- Debounce configurável para aguardar a estabilização antes do envio.
- Verificação periódica de segurança para alterações remotas e eventos locais eventualmente perdidos.
- Sincronização manual pela interface ou pelo ícone da bandeja.
- Botão para parar e reiniciar o serviço de sincronização sem fechar o aplicativo.

### Arquivos e diretórios

- Sincronização recursiva de arquivos e subpastas.
- Suporte a arquivos armazenados diretamente na raiz do compartilhamento.
- Criação automática de pastas remotas por WebDAV `MKCOL`.
- Criação local de pastas existentes no servidor.
- Suporte a pastas vazias.
- Opção para sincronizar todo o compartilhamento ou somente as pastas selecionadas.

### Proteção contra conflitos

O cliente utiliza:

- hash SHA-256 para identificar alterações locais;
- ETag WebDAV para identificar alterações remotas;
- cópia local de segurança quando o mesmo arquivo for modificado dos dois lados.

Exemplo de arquivo preservado em conflito:

```text
Relatorio.docx.conflito-20260909-143210
```

### Lixeira remota

Arquivos e pastas removidos localmente não são excluídos definitivamente do servidor. O conteúdo é movido para a pasta `.Trash`, preservando a estrutura original.

Exemplo:

```text
Documentos\Projetos\Relatorio.docx
```

é movido para:

```text
.Trash\Documentos\Projetos\Relatorio.docx
```

Se já existir outro item com o mesmo nome na lixeira, a versão anterior é renomeada:

```text
Relatorio (1).docx
Relatorio (2).docx
Relatorio (3).docx
```

O item excluído mais recentemente mantém o nome original.

### Múltiplos domínios e perfis

É possível cadastrar e executar mais de um domínio Mesh simultaneamente, por exemplo:

```text
mesh.aplicado.com.br
mesh.crsbrands.com.br
```

Ao cadastrar o domínio:

```text
mesh.aplicado.com.br
```

o aplicativo configura automaticamente o endereço WebDAV:

```text
https://mesh.aplicado.com.br/drive/
```

Cada domínio possui separadamente:

- configuração;
- credencial;
- pasta local;
- estado da sincronização;
- arquivo de log;
- inicialização automática;
- instância de execução.

A pasta local padrão segue o formato:

```text
%USERPROFILE%\Mesh Drive\<domínio>
```

Exemplo:

```text
C:\Users\usuario\Mesh Drive\mesh.aplicado.com.br
```

### Credenciais

A senha é armazenada no Gerenciador de Credenciais do Windows, em vez de ser gravada diretamente no arquivo JSON de configuração.

### Inicialização automática

A opção **Iniciar com o Windows** registra o perfil atual em:

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

Como o registro é feito em `HKEY_CURRENT_USER`, não são necessários privilégios administrativos.

Cada domínio recebe sua própria entrada e é iniciado em segundo plano com um comando semelhante a:

```powershell
MeshDriveSync.exe --profile="mesh.aplicado.com.br" --background
```

### Interface

- Tema escuro.
- Dashboard com indicadores de uploads, downloads, conflitos e erros.
- Histórico de atividade recente.
- Tela de configuração por domínio.
- Seletor de domínios.
- Ícone na janela, barra de tarefas e bandeja do Windows.
- Menu da bandeja com acesso à pasta local, sincronização manual, parada do serviço e abertura de outro domínio.

## Requisitos para compilação

### Sistema operacional

Ambiente de compilação recomendado:

- Windows 10;
- Windows 11;
- Windows Server com suporte ao .NET 8 Desktop Runtime/SDK.

### .NET SDK 8

Instale o **.NET SDK 8.0**.

Download oficial:

- [.NET 8 Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)

Verifique a instalação:

```powershell
dotnet --version
```

O comando deve retornar uma versão `8.x` compatível.

### PowerShell

O projeto utiliza um script PowerShell para restaurar, compilar e publicar o aplicativo.

Na sessão atual, libere a execução de scripts:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

Essa alteração vale somente para a janela atual do PowerShell.

### ImageMagick, opcional

O ImageMagick é utilizado para converter `Aplicado_Favicon.svg` em um arquivo `.ico` multirresolução para o Windows.

Instalação pelo WinGet:

```powershell
winget install -e --id ImageMagick.ImageMagick
```

Depois da instalação, feche e abra novamente o PowerShell e verifique:

```powershell
magick -version
```

Sem o ImageMagick, o projeto ainda pode ser compilado, mas a preparação automática do ícone pode não ser executada.

### Inno Setup, opcional

O Inno Setup 6 é necessário somente para gerar o instalador.

Download oficial:

- [Inno Setup](https://jrsoftware.org/isdl.php)

O script de build procura o compilador em:

```text
C:\Program Files (x86)\Inno Setup 6\ISCC.exe
```

## Como compilar

Abra o PowerShell na pasta raiz do projeto:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```

O script executa:

```text
dotnet restore
dotnet build
dotnet publish
```

## Arquivos gerados

### Executável portátil

O executável publicado é criado em:

```text
dist\MeshDriveSync.exe
```

A publicação utiliza:

```text
SelfContained = true
PublishSingleFile = true
RuntimeIdentifier = win-x64
```

Por isso, para uso normal, basta distribuir o arquivo:

```text
MeshDriveSync.exe
```

O usuário final não precisa instalar o .NET separadamente.

### Instalador

Quando o Inno Setup estiver instalado, o build também gera:

```text
installer\MeshDriveSync-Setup-2.1.0.exe
```

O instalador é opcional. O executável portátil pode ser copiado para uma pasta fixa e executado diretamente.

Pasta sugerida para instalação manual:

```text
C:\Aplicativos\MeshDriveSync
```

Evite executar permanentemente a aplicação a partir de `Downloads`, `Temp` ou outra pasta que possa ser removida, pois a inicialização automática registra o caminho atual do executável.

## Primeira configuração

1. Execute `MeshDriveSync.exe`.
2. Clique em **Novo domínio**.
3. Informe somente o domínio, por exemplo:

   ```text
   mesh.aplicado.com.br
   ```

4. Abra o domínio criado.
5. Informe o usuário e a senha.
6. Clique em **Conectar e listar**.
7. Selecione as pastas desejadas ou marque **Sincronizar tudo**.
8. Revise a pasta local.
9. Ajuste, se necessário:
   - **Aguardar após alteração**, padrão de 10 segundos;
   - **Verificação de segurança**, padrão de 600 segundos.
10. Marque **Iniciar com o Windows**, se desejado.
11. Clique em **Salvar e iniciar**.

## Execução de múltiplos domínios

Abra novamente o mesmo executável e selecione outro domínio.

Também é possível iniciar diretamente um perfil:

```powershell
.\MeshDriveSync.exe --profile="mesh.aplicado.com.br"
```

Para iniciar em segundo plano:

```powershell
.\MeshDriveSync.exe --profile="mesh.aplicado.com.br" --background
```

Uma instância é permitida para cada domínio.

## Arquivos de configuração e logs

As informações de cada domínio são armazenadas em:

```text
%LOCALAPPDATA%\MeshDriveSync\profiles
```

Estrutura aproximada:

```text
profiles\
├── mesh.aplicado.com.br.settings.json
├── mesh.aplicado.com.br.state.json
├── mesh.aplicado.com.br.log
├── mesh.crsbrands.com.br.settings.json
├── mesh.crsbrands.com.br.state.json
└── mesh.crsbrands.com.br.log
```

## Distribuição

Para distribuição simples:

1. Compile o projeto.
2. Copie `dist\MeshDriveSync.exe` para uma pasta fixa da estação.
3. Execute o aplicativo.
4. Configure os domínios.
5. Marque **Iniciar com o Windows**, se necessário.

O instalador é útil quando for necessário:

- padronizar o diretório de instalação;
- criar atalhos;
- disponibilizar desinstalação;
- facilitar a distribuição para diversos usuários.

## Recomendações de teste

Antes de utilizar em dados de produção, valide com uma pasta de teste:

- criação de arquivo local;
- alteração de arquivo local;
- download de arquivo remoto;
- criação de pasta vazia;
- exclusão de arquivo para `.Trash`;
- exclusão de pasta para `.Trash`;
- conflito de edição;
- sincronização de arquivos na raiz;
- execução simultânea de dois domínios;
- inicialização automática após novo logon.

## Licença

Defina neste repositório a licença aplicável ao projeto. Para uso restrito ou interno, documente explicitamente as condições de uso, cópia, modificação e distribuição.

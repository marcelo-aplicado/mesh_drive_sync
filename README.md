# Mesh Drive Sync

O **Mesh Drive Sync** é um aplicativo para Windows que centraliza o mapeamento persistente e a sincronização de compartilhamentos WebDAV disponibilizados pelo plugin Mesh Drive para MeshCentral.

A versão **3.0.8** permite administrar diversos mapeamentos, domínios e credenciais em uma única aplicação, com apenas um processo e um ícone na bandeja do sistema.

## Recursos da versão 3.0.8

### Múltiplos mapeamentos

- Cadastro de vários perfis no mesmo aplicativo.
- Suporte a diferentes domínios MeshCentral.
- Credenciais independentes por domínio e usuário.
- Seleção da pasta remota sem necessidade de conhecer manualmente o caminho `/drive`.
- Listagem das pastas disponíveis no Mesh Drive.
- Seleção apenas entre letras de unidade disponíveis.
- Nome do perfil utilizado como identificação do mapeamento no Windows.

### Mapeamento persistente

O aplicativo monitora os perfis configurados e tenta restabelecer os mapeamentos WebDAV quando necessário, evitando depender somente da persistência nativa do Windows.

Cada perfil pode ser configurado para:

- iniciar automaticamente com o Windows;
- reconectar quando a unidade ficar indisponível;
- usar uma letra de unidade específica;
- apontar para a raiz do Mesh Drive ou para uma pasta remota específica.

### Inicialização automática

A versão 3.0.8 mantém apenas uma entrada de inicialização no Registro do Windows:

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

A entrada `MeshDriveSync` é criada somente quando existe pelo menos um perfil ativo marcado com a opção **Iniciar este mapeamento automaticamente com o Windows**.

Quando nenhum perfil utiliza inicialização automática, a entrada é removida. Entradas antigas no formato `MeshDriveSync-*`, usadas por versões anteriores, também são eliminadas automaticamente.

### Controle por permissão

O aplicativo verifica o tipo de acesso disponível para cada perfil:

- **Leitura e gravação:** permite mapeamento e sincronização.
- **Somente leitura:** permite somente o mapeamento.
- **Sem acesso:** impede o uso do perfil.
- **Não verificada:** a permissão será validada ao conectar.

Quando o compartilhamento é somente leitura, a sincronização não é oferecida. Isso evita tentativas de upload, alteração ou exclusão em pastas nas quais o usuário possui apenas permissão de consulta.

### Sincronização

Para perfis com permissão de leitura e gravação, o Mesh Drive Sync pode manter uma cópia local do compartilhamento.

Principais recursos:

- sincronização bidirecional;
- criação e atualização de arquivos;
- criação de pastas;
- monitoramento de alterações locais;
- verificação periódica de alterações remotas;
- tratamento de conflitos;
- uso de hash SHA-256 e ETag;
- movimentação de exclusões para a pasta remota `.Trash`;
- proteção contra arquivos temporários e arquivos internos do sincronizador.

### Organização da pasta local

A pasta local segue o padrão:

```text
<Pasta selecionada>\Mesh Drive\<Domínio>\<Perfil>
```

Exemplo:

```text
C:\Users\usuario\Mesh Drive\mesh.crsbrands.com.br\KeePassXC
```

A pré-visualização do caminho é atualizada automaticamente quando o domínio ou o nome do perfil é alterado.

### Credenciais

As senhas são armazenadas no **Gerenciador de Credenciais do Windows**.

O arquivo de configuração mantém apenas informações não sensíveis, como domínio, usuário, letra da unidade e preferências do perfil.

A configuração principal é armazenada em:

```text
%LOCALAPPDATA%\MeshDriveSync\config.json
```

Os estados e logs de cada perfil ficam em:

```text
%LOCALAPPDATA%\MeshDriveSync\profiles\<ID do perfil>
```

## Interface

A aplicação utiliza uma única janela de gerenciamento e um único ícone na bandeja do sistema.

Na tela principal é possível:

- adicionar, editar e remover perfis;
- conectar e desconectar mapeamentos;
- verificar permissões;
- abrir uma unidade no Explorador de Arquivos;
- visualizar domínio, pasta remota, letra, permissão e estado.

Na tela de cadastro é possível:

- informar nome, domínio, usuário e senha;
- listar as pastas remotas disponíveis;
- selecionar uma letra livre;
- escolher a pasta-base local;
- habilitar inicialização automática;
- habilitar reconexão automática;
- habilitar sincronização quando houver permissão de gravação.

## Requisitos

### Sistema operacional

- Windows 10 ou Windows 11 de 64 bits.
- Serviço **WebClient** disponível e em execução para o mapeamento WebDAV.

### Compilação

- .NET SDK 8.
- PowerShell.
- ImageMagick opcional, utilizado para preparar o ícone.
- Inno Setup 6 opcional, utilizado para gerar o instalador.

## Como compilar

Abra o PowerShell na raiz do projeto e execute:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```

O script executa a restauração, a compilação e duas publicações para Windows x64.

## Executáveis gerados

### Versão portátil

```text
dist\MeshDriveSync_Portable.exe
```

- Inclui o runtime do .NET 8.
- Não exige instalação adicional do .NET.
- Indicada para uso portátil, testes e distribuição avulsa.

### Versão enxuta

```text
dist\MeshDriveSync.exe
```

- Exige o **Microsoft .NET 8 Desktop Runtime x64** instalado.
- Possui tamanho reduzido.
- É a edição utilizada pelo instalador.

O trimming permanece desabilitado para preservar a compatibilidade com Windows Forms, interoperabilidade COM, Gerenciador de Credenciais e chamadas nativas do Windows.

## Estrutura do projeto

```text
CredentialManager.cs
MainForm.cs
MeshDriveSync.csproj
MeshDriveSync.iss
Models.cs
NetworkDriveManager.cs
ProfileDialog.cs
Program.cs
SyncEngine.cs
WebDavClient.cs
build.ps1
prepare-icon.ps1
```

## Instalação e uso

1. Compile o projeto ou utilize um dos executáveis publicados.
2. Execute o Mesh Drive Sync.
3. Selecione **Adicionar**.
4. Informe um nome para o perfil.
5. Informe o domínio do MeshCentral, o usuário e a senha.
6. Use **Listar pastas** para selecionar a raiz ou uma pasta do Mesh Drive.
7. Escolha uma letra de unidade disponível.
8. Selecione a pasta-base local.
9. Escolha se o perfil deve iniciar com o Windows e reconectar automaticamente.
10. Salve o perfil e conecte o mapeamento.

Após a conexão, o aplicativo verifica a permissão efetiva. A sincronização somente fica disponível para compartilhamentos com acesso de leitura e gravação.

## Atualização a partir de versões anteriores

A versão 3.0.8 utiliza uma configuração central e uma única instância do aplicativo. Antes de substituir uma instalação antiga, recomenda-se preservar os dados existentes em:

```text
%LOCALAPPDATA%\MeshDriveSync
```

A aplicação remove automaticamente entradas antigas de inicialização no formato:

```text
MeshDriveSync-*
```

## Recomendações de teste

Antes de utilizar em produção, valide:

- criação de múltiplos perfis;
- mapeamentos em domínios diferentes;
- reconexão após novo logon;
- remoção da inicialização ao desmarcar ou excluir todos os perfis automáticos;
- acesso a compartilhamentos somente leitura;
- bloqueio da sincronização em modo somente leitura;
- upload e download em compartilhamentos graváveis;
- criação e alteração de arquivos pelo Explorador de Arquivos;
- conflitos de edição;
- funcionamento das versões portátil e enxuta.

## Versão estável

A versão **3.0.8** é a base estável oficial para as próximas melhorias do Mesh Drive Sync.

## Licença

Defina no repositório a licença aplicável ao projeto e as condições de uso, modificação e distribuição.

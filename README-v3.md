# Mesh Drive Sync 3.0.2

Gerenciador unificado de múltiplos mapeamentos WebDAV e sincronizações Mesh Drive.

## Cadastro

1. Informe um nome para o mapeamento.
2. Informe domínio, usuário e senha.
3. Use **Listar pastas** para escolher a raiz ou uma pasta publicada pelo Mesh Drive.
4. Selecione uma letra disponível.
5. Selecione a pasta local; o aplicativo acrescenta `Mesh Drive\Nome do perfil`.
6. Ao conectar, a permissão é verificada. Somente leitura mantém o mapeamento e bloqueia a sincronização.

## Build

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.ps1
```


### Saídas da compilação 3.0.8
- `dist\MeshDriveSync_Portable.exe`: edição autocontida, com runtime .NET 8 incluído.
- `dist\MeshDriveSync.exe`: edição enxuta, requer Microsoft .NET 8 Desktop Runtime x64.

### Inicialização automática
Existe somente uma entrada `MeshDriveSync` no Registro. Ela é criada quando pelo menos um perfil ativo está marcado para iniciar automaticamente com o Windows e é removida quando nenhum perfil possui essa opção. Entradas legadas `MeshDriveSync-*` também são removidas.

## 3.0.8
- Inicialização automática vinculada aos perfis ativos marcados para iniciar com o Windows.
- Sem perfis automáticos, a entrada MeshDriveSync é removida do Registro.
- Entradas antigas MeshDriveSync-* são removidas automaticamente.
- Build passa a gerar MeshDriveSync_Portable.exe, com runtime incluído.
- Build também gera MeshDriveSync.exe enxuto, dependente do .NET 8 Desktop Runtime x64.
- O instalador utiliza a edição enxuta MeshDriveSync.exe.

# 3.0.7

- Corrigida a sequência de escape inválida no texto do seletor de pasta local.
- Preservados o domínio inicial vazio, a janela compacta e o caminho Pasta selecionada\\Mesh Drive\\Domínio\\Perfil.

# 3.0.6

- Altura da janela de cadastro reduzida.
- Domínio inicial deixado em branco.
- Pasta local padronizada como Pasta selecionada\Mesh Drive\Domínio\Perfil.
- Pré-visualização da pasta local atualizada ao alterar domínio ou nome do perfil.
- Pasta-base selecionada armazenada separadamente para evitar caminhos duplicados.

# 3.0.5

- Corrigido o corte dos textos das opções na janela de cadastro.
- Checkboxes agora usam AutoSize e ocupam a largura disponível.
- Linhas das opções passam a ajustar automaticamente a altura.

# 3.0.4

- Ícone aplicado também à janela de cadastro e edição de mapeamentos.
- Removida a opção de trocar automaticamente para outra letra.
- Se a letra selecionada estiver ocupada, o mapeamento é ignorado e o motivo é registrado.
- Opção de inicialização renomeada para “Iniciar este mapeamento automaticamente com o Windows”.
- Opção de reconexão renomeada para “Restabelecer automaticamente o mapeamento se a conexão cair”.
- Opção de sincronização renomeada para “Manter uma cópia sincronizada desta pasta no computador”.

# 3.0.3

- Pacote completo consolidado.
- Restauradas as propriedades SelectedFolders e SyncAll exigidas pelo SyncEngine.
- Mantida a propriedade Ignore da sincronização.
- Preservadas todas as melhorias de usabilidade da 3.0.2.

# 3.0.2

- Ícone do executável reutilizado na janela, barra de tarefas e bandeja.
- Atalho do Menu Iniciar usa explicitamente o ícone do executável.
- Nome do perfil aplicado como rótulo do mapeamento no Explorador.
- Domínio padrão alterado para mesh.aplicado.com.br.
- Caminho /drive ocultado e fixado internamente.
- Listagem de pastas remotas restaurada no cadastro.
- Letras de unidade apresentadas somente quando disponíveis.
- Seletor de pasta local completa o caminho com Mesh Drive\Nome do perfil.
- Opções renomeadas com descrições claras.
- Sincronização oculta para acesso somente leitura ou sem acesso.

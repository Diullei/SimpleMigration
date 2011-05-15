# SimpleMigration

Como utilizar:

Após compilar o projeto configure a pasta na qual você deseja colocar os arquivos de migração segundo o esquema abaixo:

root\
 |
 +--sm.exe
 |
 +--sm.exe.config
 |
 +--mig\
     |
     +--(arquivos de migração)

Onde: 
	- "root" é a pasta escolhida para deploy do SimpleMigration
	- "sm.exe" e "sm.exe.config" são arquivos localizados na pasta bin\Debug ou bin\Release do projeto após a compilação
	- "mig" é a pasta onde ficarão guardados os arquivos de migração.
	- "(arquivos de migração)" são arquivos no formato NUMERO-TIPO.sql, sendo, NUMERO o número da versão de migração e TIPO o tipo da migração informando se é UP ou DOWN Exemplo: 20110101-UP.sql. Nota: Para todo arquivo UP será necessário existir um arquivo DOWN correspondente ao mesmo número de versão e vice versa.

Crie a tabela de versão no banco a ser versionado seguindo o script abaixo:

CREATE TABLE [SimpleMigration_VersionInfo](
	[version] [bigint] NOT NULL,
	CONSTRAINT [PK_SimpleMigration_VersionInfo] PRIMARY KEY CLUSTERED 
	(
		[version] ASC
	)
) ON [PRIMARY]

## NOTA

Este script foi feito para ser executado no SqlServer. Será necessário adaptar este script caso queira utilizar outro banco de dados.
você pode contribuir com este projeto e me enviar por email este script adaptado para outros SGBDs.

Altere o arquivo sm.exe.config ajustando a string de conexão para o seu banco de dados.

Feito isso estamos pronto para utilizar o SimpleMigration:

	- Utilize o comando sm ? para verificar quais os comandos disponíveis
	- Utilize o comando sm para migrar seu banco de dados para a ultima versão disponível caso o mesmo esteja desatualizado.
	- Utilize o comando sm version N, onde N é o número da versão a ser atualizada. Isso irá atualizar seu banco de dados para a versão com tanto que os scripts UP e DOWN estejan escritos corretamente.
	
OBS: Estou utilizando este programa em produção pois tive a necessidade de criar algo ao mesmo tempo simples e que atendesse as minhasw necessidades de versionamento de banco de dados. O mesmo ainda está em Beta e peço que me reportem qualquer bug encontrado para que eu possa corrigir.


Diullei Gomes [diullei@gmail.com]
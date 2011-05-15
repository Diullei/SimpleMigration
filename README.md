# SimpleMigration

Como utilizar:

Ap�s compilar o projeto configure a pasta na qual voc� deseja colocar os arquivos de migra��o segundo o esquema abaixo:

root\
 |
 +--sm.exe
 |
 +--sm.exe.config
 |
 +--mig\
     |
     +--(arquivos de migra��o)

Onde: 
	- "root" � a pasta escolhida para deploy do SimpleMigration
	- "sm.exe" e "sm.exe.config" s�o arquivos localizados na pasta bin\Debug ou bin\Release do projeto ap�s a compila��o
	- "mig" � a pasta onde ficar�o guardados os arquivos de migra��o.
	- "(arquivos de migra��o)" s�o arquivos no formato NUMERO-TIPO.sql, sendo, NUMERO o n�mero da vers�o de migra��o e TIPO o tipo da migra��o informando se � UP ou DOWN Exemplo: 20110101-UP.sql. Nota: Para todo arquivo UP ser� necess�rio existir um arquivo DOWN correspondente ao mesmo n�mero de vers�o e vice versa.

Crie a tabela de vers�o no banco a ser versionado seguindo o script abaixo:

CREATE TABLE [SimpleMigration_VersionInfo](
	[version] [bigint] NOT NULL,
	CONSTRAINT [PK_SimpleMigration_VersionInfo] PRIMARY KEY CLUSTERED 
	(
		[version] ASC
	)
) ON [PRIMARY]

## NOTA

Este script foi feito para ser executado no SqlServer. Ser� necess�rio adaptar este script caso queira utilizar outro banco de dados.
voc� pode contribuir com este projeto e me enviar por email este script adaptado para outros SGBDs.

Altere o arquivo sm.exe.config ajustando a string de conex�o para o seu banco de dados.

Feito isso estamos pronto para utilizar o SimpleMigration:

	- Utilize o comando sm ? para verificar quais os comandos dispon�veis
	- Utilize o comando sm para migrar seu banco de dados para a ultima vers�o dispon�vel caso o mesmo esteja desatualizado.
	- Utilize o comando sm version N, onde N � o n�mero da vers�o a ser atualizada. Isso ir� atualizar seu banco de dados para a vers�o com tanto que os scripts UP e DOWN estejan escritos corretamente.
	
OBS: Estou utilizando este programa em produ��o pois tive a necessidade de criar algo ao mesmo tempo simples e que atendesse as minhasw necessidades de versionamento de banco de dados. O mesmo ainda est� em Beta e pe�o que me reportem qualquer bug encontrado para que eu possa corrigir.


Diullei Gomes [diullei@gmail.com]
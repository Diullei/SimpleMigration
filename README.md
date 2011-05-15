# SimpleMigration

### How to use:

After compiling the project, set up the folder to put the migration files as outlined below:

```text
root\
 |
 +--sm.exe
 |
 +--sm.exe.config
 |
 +--mig\
     |
     +--(migration files)
```

As: 
	* "root" is the chosen folder to deploy the SimpleMigration;
	* "sm.exe" and "sm.exe.config" are files located in the bin\Debug or bin\Release project folder after compiling;
	* "mig" is the folder where the migration files will be stored;
	* "(migration files)" are files formated as NUMBER-TYPE.sql, where NUMBER is the migration version number and TYPE is "UP" or "DOWN". 
		+ Example: 20110101-UP.sql. Note: all version need to have a "UP" and "DOWN" file.

Create the version table in the database to be versioned with the script:

```sql
CREATE TABLE [SimpleMigration_VersionInfo](
	[version] [bigint] NOT NULL,
	CONSTRAINT [PK_SimpleMigration_VersionInfo] PRIMARY KEY CLUSTERED 
	(
		[version] ASC
	)
) ON [PRIMARY]
```

### NOTE

This script was made ??to run on SqlServer. You will need to adapt this script if you want to use in another database type.

Update sm.exe.config to use your Connection String.

That done we are ready to use SimpleMigration:

	* Use 'sm ?' command to help;
	* Use 'sm' command to migrate its database to the latest version if it is outdated.
	* Use 'sm version NUMBER' command, where NUMBER is the target version number.
	
OBS: this is a BETA program.


Diullei Gomes [diullei@gmail.com]
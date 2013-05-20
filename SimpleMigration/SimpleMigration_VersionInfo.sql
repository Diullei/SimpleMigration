CREATE TABLE [SimpleMigration_VersionInfo](
    [version] [bigint] NOT NULL,
	[tag] nvarchar(100) NULL,
    CONSTRAINT [PK_SimpleMigration_VersionInfo] PRIMARY KEY CLUSTERED 
    (
        [version] ASC
    )
) ON [PRIMARY]

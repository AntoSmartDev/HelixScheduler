IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [BusyEvents] (
        [Id] bigint NOT NULL IDENTITY,
        [Title] nvarchar(300) NULL,
        [StartUtc] datetime2 NOT NULL,
        [EndUtc] datetime2 NOT NULL,
        [EventType] nvarchar(50) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_BusyEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [DemoScenarioStates] (
        [Id] int NOT NULL IDENTITY,
        [BaseDateUtc] datetime2 NOT NULL,
        [SeedVersion] int NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_DemoScenarioStates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [ResourceProperties] (
        [Id] int NOT NULL IDENTITY,
        [ParentId] int NULL,
        [Key] nvarchar(100) NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [SortOrder] int NULL,
        CONSTRAINT [PK_ResourceProperties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ResourceProperties_ResourceProperties_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ResourceProperties] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [ResourceTypes] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [Label] nvarchar(200) NOT NULL,
        [SortOrder] int NULL,
        CONSTRAINT [PK_ResourceTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [Rules] (
        [Id] bigint NOT NULL IDENTITY,
        [Kind] tinyint NOT NULL,
        [IsExclude] bit NOT NULL,
        [Title] nvarchar(300) NULL,
        [FromDateUtc] date NULL,
        [ToDateUtc] date NULL,
        [SingleDateUtc] date NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [DaysOfWeekMask] int NULL,
        [DayOfMonth] int NULL,
        [IntervalDays] int NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Rules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [Resources] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(64) NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsSchedulable] bit NOT NULL,
        [Capacity] int NOT NULL DEFAULT 1,
        [TypeId] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Resources] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Resources_Capacity] CHECK ([Capacity] >= 1),
        CONSTRAINT [FK_Resources_ResourceTypes_TypeId] FOREIGN KEY ([TypeId]) REFERENCES [ResourceTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [ResourceTypeProperties] (
        [ResourceTypeId] int NOT NULL,
        [PropertyDefinitionId] int NOT NULL,
        CONSTRAINT [PK_ResourceTypeProperties] PRIMARY KEY ([ResourceTypeId], [PropertyDefinitionId]),
        CONSTRAINT [FK_ResourceTypeProperties_ResourceProperties_PropertyDefinitionId] FOREIGN KEY ([PropertyDefinitionId]) REFERENCES [ResourceProperties] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ResourceTypeProperties_ResourceTypes_ResourceTypeId] FOREIGN KEY ([ResourceTypeId]) REFERENCES [ResourceTypes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [BusyEventResources] (
        [BusyEventId] bigint NOT NULL,
        [ResourceId] int NOT NULL,
        CONSTRAINT [PK_BusyEventResources] PRIMARY KEY ([BusyEventId], [ResourceId]),
        CONSTRAINT [FK_BusyEventResources_BusyEvents_BusyEventId] FOREIGN KEY ([BusyEventId]) REFERENCES [BusyEvents] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BusyEventResources_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [ResourcePropertyLinks] (
        [ResourceId] int NOT NULL,
        [PropertyId] int NOT NULL,
        CONSTRAINT [PK_ResourcePropertyLinks] PRIMARY KEY ([ResourceId], [PropertyId]),
        CONSTRAINT [FK_ResourcePropertyLinks_ResourceProperties_PropertyId] FOREIGN KEY ([PropertyId]) REFERENCES [ResourceProperties] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ResourcePropertyLinks_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [ResourceRelations] (
        [ParentResourceId] int NOT NULL,
        [ChildResourceId] int NOT NULL,
        [RelationType] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_ResourceRelations] PRIMARY KEY ([ParentResourceId], [ChildResourceId], [RelationType]),
        CONSTRAINT [FK_ResourceRelations_Resources_ChildResourceId] FOREIGN KEY ([ChildResourceId]) REFERENCES [Resources] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ResourceRelations_Resources_ParentResourceId] FOREIGN KEY ([ParentResourceId]) REFERENCES [Resources] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE TABLE [RuleResources] (
        [RuleId] bigint NOT NULL,
        [ResourceId] int NOT NULL,
        CONSTRAINT [PK_RuleResources] PRIMARY KEY ([RuleId], [ResourceId]),
        CONSTRAINT [FK_RuleResources_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RuleResources_Rules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [Rules] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_BusyEventResources_ResourceId_BusyEventId] ON [BusyEventResources] ([ResourceId], [BusyEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_BusyEvents_StartUtc_EndUtc] ON [BusyEvents] ([StartUtc], [EndUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_ResourceProperties_ParentId] ON [ResourceProperties] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_ResourcePropertyLinks_PropertyId_ResourceId] ON [ResourcePropertyLinks] ([PropertyId], [ResourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_ResourceRelations_ChildResourceId] ON [ResourceRelations] ([ChildResourceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_Resources_Code] ON [Resources] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_Resources_IsSchedulable] ON [Resources] ([IsSchedulable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_Resources_TypeId] ON [Resources] ([TypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_ResourceTypeProperties_PropertyDefinitionId_ResourceTypeId] ON [ResourceTypeProperties] ([PropertyDefinitionId], [ResourceTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_ResourceTypes_Key] ON [ResourceTypes] ([Key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_RuleResources_ResourceId_RuleId] ON [RuleResources] ([ResourceId], [RuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    CREATE INDEX [IX_Rules_FromDateUtc_ToDateUtc_SingleDateUtc] ON [Rules] ([FromDateUtc], [ToDateUtc], [SingleDateUtc]) INCLUDE ([IsExclude]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104231821_Baseline'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104231821_Baseline', N'10.0.0');
END;

COMMIT;
GO


CREATE TABLE [dbo].[issue] (
    [id]       INT IDENTITY (1, 1) NOT NULL,
    [patronid] INT NULL,
    [itemid]   INT NOT NULL,
    PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [fk_issue_item] FOREIGN KEY ([itemid]) REFERENCES [dbo].[item] ([id]),
    CONSTRAINT [fk_issue_patron] FOREIGN KEY ([patronid]) REFERENCES [dbo].[patron] ([id])
);


GO
CREATE NONCLUSTERED INDEX [ix_issue_patron]
    ON [dbo].[issue]([patronid] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_issue_item]
    ON [dbo].[issue]([itemid] ASC);


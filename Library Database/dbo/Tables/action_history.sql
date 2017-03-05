CREATE TABLE [dbo].[action_history] (
    [id]              INT      IDENTITY (1, 1) NOT NULL,
    [patronid]        INT      NULL,
    [itemid]          INT      NOT NULL,
    [action_datetime] DATETIME NOT NULL,
    [action_type]     INT      NOT NULL,
    PRIMARY KEY NONCLUSTERED ([id] ASC),
    CONSTRAINT [fk_action_action] FOREIGN KEY ([action_type]) REFERENCES [dbo].[action_type] ([id])
);


GO
CREATE NONCLUSTERED INDEX [cix_action_action]
    ON [dbo].[action_history]([action_type] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_action_item]
    ON [dbo].[action_history]([itemid] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_action_patron]
    ON [dbo].[action_history]([patronid] ASC);


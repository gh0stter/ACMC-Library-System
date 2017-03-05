CREATE TABLE [dbo].[item] (
    [id]               INT            IDENTITY (1, 1) NOT NULL,
    [code]             NVARCHAR (30)  NULL,
    [barcode]          NVARCHAR (100) NULL,
    [title]            NVARCHAR (100) NULL,
    [keywords]         NVARCHAR (100) NULL,
    [publisher]        NVARCHAR (100) NULL,
    [description]      NVARCHAR (100) NULL,
    [moreinfo]         TEXT           NULL,
    [base_location]    INT            NULL,
    [current_location] INT            NULL,
    [category]         INT            NULL,
    [item_subclass]    INT            NULL,
    [author]           NVARCHAR (100) NULL,
    [isbn]             NVARCHAR (100) NULL,
    [pages]            INT            NULL,
    [minutes]          INT            NULL,
    [status]           INT            NULL,
    [patronid]         INT            NULL,
    [translator]       NVARCHAR (100) NULL,
    [language]         NVARCHAR (100) NULL,
    [donator]          NVARCHAR (100) NULL,
    [price]            NVARCHAR (100) NULL,
    [published_date]   NVARCHAR (100) NULL,
    [due_date]         DATETIME       NULL,
    PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [fk_item_category] FOREIGN KEY ([category]) REFERENCES [dbo].[item_category] ([id]),
    CONSTRAINT [fk_item_subclass] FOREIGN KEY ([item_subclass]) REFERENCES [dbo].[item_class] ([id]),
    CONSTRAINT [uk_barcode] UNIQUE NONCLUSTERED ([barcode] ASC)
);


GO
CREATE NONCLUSTERED INDEX [ix_item_barcode]
    ON [dbo].[item]([barcode] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_code]
    ON [dbo].[item]([code] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_title]
    ON [dbo].[item]([title] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_keywords]
    ON [dbo].[item]([keywords] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_current_location]
    ON [dbo].[item]([current_location] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_category]
    ON [dbo].[item]([category] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_isbn]
    ON [dbo].[item]([isbn] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_item_patron]
    ON [dbo].[item]([patronid] ASC);


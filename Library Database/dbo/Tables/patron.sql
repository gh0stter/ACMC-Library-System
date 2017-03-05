CREATE TABLE [dbo].[patron] (
    [id]                INT            IDENTITY (1, 1) NOT NULL,
    [barcode]           NVARCHAR (30)  NULL,
    [surname_ch]        NVARCHAR (100) NULL,
    [firstnames_ch]     NVARCHAR (100) NULL,
    [surname_en]        NVARCHAR (100) NULL,
    [firstnames_en]     NVARCHAR (100) NULL,
    [surname_pinyin]    NVARCHAR (30)  NULL,
    [firstnames_pinyin] NVARCHAR (30)  NULL,
    [picture]           IMAGE          NULL,
    [limit]             INT            NULL,
    [items_out]         INT            NULL,
    [address]           NVARCHAR (255) NULL,
    [phone]             NVARCHAR (30)  NULL,
    [email]             NVARCHAR (255) NULL,
    [guarantor]         INT            NULL,
    [created]           DATETIME       NULL,
    [expiry]            DATETIME       NULL,
    [fellowship]        INT            NULL,
    [balance]           MONEY          NULL,
    PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [fk_fellowship] FOREIGN KEY ([fellowship]) REFERENCES [dbo].[fellowship] ([id]),
    CONSTRAINT [fk_patron_guarantor] FOREIGN KEY ([guarantor]) REFERENCES [dbo].[patron] ([id])
);


GO
CREATE NONCLUSTERED INDEX [ix_patron_barcode]
    ON [dbo].[patron]([barcode] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_patron_surname_pinyin]
    ON [dbo].[patron]([surname_pinyin] ASC);


GO
CREATE NONCLUSTERED INDEX [ix_patron_surname_en]
    ON [dbo].[patron]([surname_en] ASC);


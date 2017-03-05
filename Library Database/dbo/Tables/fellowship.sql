CREATE TABLE [dbo].[fellowship] (
    [id]     INT            IDENTITY (1, 1) NOT NULL,
    [f_name] NVARCHAR (255) NOT NULL,
    CONSTRAINT [pk_fellowship] PRIMARY KEY CLUSTERED ([id] ASC)
);


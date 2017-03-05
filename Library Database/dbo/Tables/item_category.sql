CREATE TABLE [dbo].[item_category] (
    [id]            INT            IDENTITY (1, 1) NOT NULL,
    [category_name] NVARCHAR (100) NOT NULL,
    [cat_code]      VARCHAR (10)   NULL,
    PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [uk_itemCategoryName] UNIQUE NONCLUSTERED ([category_name] ASC)
);


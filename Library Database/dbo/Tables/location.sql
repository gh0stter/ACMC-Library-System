CREATE TABLE [dbo].[location] (
    [id]            INT            IDENTITY (1, 1) NOT NULL,
    [location_name] NVARCHAR (100) NOT NULL,
    PRIMARY KEY NONCLUSTERED ([id] ASC),
    CONSTRAINT [uk_locationName] UNIQUE NONCLUSTERED ([location_name] ASC)
);


GO
CREATE UNIQUE CLUSTERED INDEX [cix_location]
    ON [dbo].[location]([location_name] ASC);


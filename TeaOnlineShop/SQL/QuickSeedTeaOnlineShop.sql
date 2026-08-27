USE [TeaOnlineShop];
GO

/* -------------------------------------------------------------
   Optional non-sensitive catalogue seed for local assessment.
   Safe to run multiple times (uses IF NOT EXISTS checks).
   Identity accounts are never created by SQL. Use the secure environment-
   driven first-administrator bootstrap described in the root README.
-------------------------------------------------------------- */

/* Shipping settings */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Settings] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[Settings] ON;
    INSERT INTO [dbo].[Settings] ([Id], [Shipping]) VALUES (1, 5.0000);
    SET IDENTITY_INSERT [dbo].[Settings] OFF;
END
GO

/* Navigation menus */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Menus])
BEGIN
    SET IDENTITY_INSERT [dbo].[Menus] ON;
    INSERT INTO [dbo].[Menus] ([ID], [MenuTitle], [Link], [Type]) VALUES (1, N'Home', N'/', N'Top');
    INSERT INTO [dbo].[Menus] ([ID], [MenuTitle], [Link], [Type]) VALUES (2, N'Shop', N'/products', N'Top');
    INSERT INTO [dbo].[Menus] ([ID], [MenuTitle], [Link], [Type]) VALUES (3, N'Home', N'/', N'Bottom');
    INSERT INTO [dbo].[Menus] ([ID], [MenuTitle], [Link], [Type]) VALUES (4, N'About', N'/Home/About', N'Top');
    SET IDENTITY_INSERT [dbo].[Menus] OFF;
END
GO

/* Home page banners (image files already exist in wwwroot/images/banners) */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Banner])
BEGIN
    SET IDENTITY_INSERT [dbo].[Banner] ON;
    INSERT INTO [dbo].[Banner] ([ID], [Title], [SubTitle], [ImageName], [Priority], [Link], [Positon])
    VALUES (1, N'Tea For A Perfect Morning', N'Start your day with a cup of tea', N'dda6a53e-ca5b-4d81-b72e-ca2992c84665.jpg', 1, N'/Products', N'Slider');

    INSERT INTO [dbo].[Banner] ([ID], [Title], [SubTitle], [ImageName], [Priority], [Link], [Positon])
    VALUES (2, N'The Finest Tea', N'Hand picked from our estates', N'3364eac2-3caf-44b7-87ae-24c2f7b56f9d.jpg', 2, N'/Products', N'Slider');

    INSERT INTO [dbo].[Banner] ([ID], [Title], [SubTitle], [ImageName], [Priority], [Link], [Positon])
    VALUES (3, N'English Breakfast', N'High energetic black tea', N'9e4c6185-0764-40b0-841a-33b032b1e5c0.jpg', 1, N'/Products', N'Banner1');

    INSERT INTO [dbo].[Banner] ([ID], [Title], [SubTitle], [ImageName], [Priority], [Link], [Positon])
    VALUES (4, N'Ginger', N'Natural ingredients', N'44a29cdd-4e34-46e8-ac67-c191854dec83.jpg', 2, N'/Products', N'Banner1');

    INSERT INTO [dbo].[Banner] ([ID], [Title], [SubTitle], [ImageName], [Priority], [Link], [Positon])
    VALUES (5, N'Mango and Strawberry', N'Fresh and cool flavors', N'ef2a510d-47fe-4047-972c-2a72a00b27a0.jpg', 1, N'/Products', N'Banner2');

    INSERT INTO [dbo].[Banner] ([ID], [Title], [SubTitle], [ImageName], [Priority], [Link], [Positon])
    VALUES (6, N'Orange Flavour', N'Finest Ceylon teas', N'952c85c4-a7d7-4127-b333-a11d69eb25bb.jpg', 2, N'/Products', N'Banner2');
    SET IDENTITY_INSERT [dbo].[Banner] OFF;
END
GO

/* Products */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products])
BEGIN
    SET IDENTITY_INSERT [dbo].[Products] ON;
    INSERT INTO [dbo].[Products] ([Id], [Title], [Description], [FullDescription], [Price], [Discount], [ImageName], [Quantity], [Tags], [VideoUrl])
    VALUES
    (30, N'Premium Decaf Ceylon Black', N'Decaffeinated pure Ceylon black tea', N'Solvent-free decaf process with rich aroma and smooth finish.', 100.0000, 25.0000, N'ddb1aff5-2646-4a59-94e5-9773cd2df9b4.jpg', 12, N'premium, black', NULL),
    (31, N'Gourmet Ceylon Supreme Black Tea', N'Premium Ceylon black tea', N'Balanced strength with bright character and clean aftertaste.', 25.0000, 5.0000, N'76222b54-7c3a-4ddb-aaaf-d13340ebc194.jpg', 20, N'black, ceylon', NULL),
    (32, N'Classic Black Tea', N'Tasty and naturally sweet notes', N'Everyday black tea suitable for morning and evening brewing.', 75.0000, 15.0000, N'0626654b-9b82-46bc-9d84-20dec94ddd8d.jpg', 7, N'black', N'https://www.youtube.com/embed/3loq8EsQfg0');
    SET IDENTITY_INSERT [dbo].[Products] OFF;
END
GO

/* Product gallery for product details page */
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProductGallery])
BEGIN
    SET IDENTITY_INSERT [dbo].[ProductGallery] ON;
    INSERT INTO [dbo].[ProductGallery] ([Id], [ProductId], [ImageName]) VALUES (101, 30, N'a8b52e34-2403-43bd-aef2-e5b112467c8c.jpg');
    INSERT INTO [dbo].[ProductGallery] ([Id], [ProductId], [ImageName]) VALUES (102, 30, N'885dfd49-67dd-4962-b5eb-86d81eddff4a.jpg');
    INSERT INTO [dbo].[ProductGallery] ([Id], [ProductId], [ImageName]) VALUES (103, 31, N'f69319ca-2c96-4765-9202-d60051e1e1de.jpg');
    INSERT INTO [dbo].[ProductGallery] ([Id], [ProductId], [ImageName]) VALUES (104, 31, N'15689dc7-8e9e-4dcb-99be-fe125a37ed17.jpg');
    INSERT INTO [dbo].[ProductGallery] ([Id], [ProductId], [ImageName]) VALUES (105, 32, N'40af9fde-58e6-40b6-82ac-176b4f794c6c.jpg');
    INSERT INTO [dbo].[ProductGallery] ([Id], [ProductId], [ImageName]) VALUES (106, 32, N'96de8395-fe24-4f0b-884c-8ca09e46663e.jpg');
    SET IDENTITY_INSERT [dbo].[ProductGallery] OFF;
END
GO

/* Quick verification */
SELECT
    (SELECT COUNT(*) FROM [dbo].[Products]) AS ProductCount,
    (SELECT COUNT(*) FROM [dbo].[Banner]) AS BannerCount,
    (SELECT COUNT(*) FROM [dbo].[Menus]) AS MenuCount;
GO

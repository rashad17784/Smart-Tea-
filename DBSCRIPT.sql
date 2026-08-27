USE [master]
GO

IF DB_ID(N'TeaOnlineShop') IS NOT NULL
BEGIN
    THROW 51000, 'TeaOnlineShop already exists. This safe setup script will not overwrite or delete an existing database.', 1;
END
ELSE
BEGIN
    EXEC(N'CREATE DATABASE [TeaOnlineShop]');
END
GO
ALTER DATABASE [TeaOnlineShop] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [TeaOnlineShop].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [TeaOnlineShop] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET ARITHABORT OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [TeaOnlineShop] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [TeaOnlineShop] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET  DISABLE_BROKER 
GO
ALTER DATABASE [TeaOnlineShop] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [TeaOnlineShop] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [TeaOnlineShop] SET  MULTI_USER 
GO
ALTER DATABASE [TeaOnlineShop] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [TeaOnlineShop] SET DB_CHAINING OFF 
GO
ALTER DATABASE [TeaOnlineShop] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [TeaOnlineShop] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [TeaOnlineShop] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [TeaOnlineShop] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'TeaOnlineShop', N'ON'
GO
ALTER DATABASE [TeaOnlineShop] SET QUERY_STORE = ON
GO
ALTER DATABASE [TeaOnlineShop] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [TeaOnlineShop]
GO
/****** Object:  Table [dbo].[Banner]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Banner](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nchar](200) NULL,
	[SubTitle] [nvarchar](1000) NULL,
	[ImageName] [nvarchar](50) NULL,
	[Priority] [smallint] NULL,
	[Link] [nvarchar](100) NULL,
	[Positon] [nvarchar](50) NULL,
 CONSTRAINT [PK_Banner] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommentSection]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommentSection](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](50) NOT NULL,
	[CommmentText] [nvarchar](1200) NOT NULL,
	[ProductId] [int] NOT NULL,
	[CreateDate] [datetime] NOT NULL,
 CONSTRAINT [PK_CommentSection] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Delivery]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Delivery](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DeliveryCode] [varchar](50) NOT NULL,
	[SupplierId] [int] NOT NULL,
	[ReceivedById] [int] NOT NULL,
	[DeliveryDate] [datetime] NOT NULL,
	[TotalAmount] [decimal](10, 2) NULL,
	[Status] [varchar](20) NULL,
	[Notes] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeliveryItem]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeliveryItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DeliveryId] [int] NOT NULL,
	[ItemId] [int] NOT NULL,
	[Quantity] [decimal](10, 2) NOT NULL,
	[UnitPrice] [decimal](10, 2) NULL,
	[TotalPrice] [decimal](10, 2) NULL,
	[Notes] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Menus]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Menus](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MenuTitle] [nvarchar](50) NULL,
	[Link] [nvarchar](300) NULL,
	[Type] [nvarchar](30) NULL,
 CONSTRAINT [PK_Menus] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Order]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Order](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[Country] [nvarchar](50) NOT NULL,
	[Address] [nvarchar](200) NOT NULL,
	[City] [nvarchar](50) NOT NULL,
	[Email] [nvarchar](50) NOT NULL,
	[Phone] [nvarchar](50) NOT NULL,
	[Comment] [nvarchar](250) NULL,
	[Shipping] [money] NULL,
	[SubTotal] [money] NULL,
	[Total] [money] NULL,
	[CreateDate] [datetime] NULL,
	[TransId] [nvarchar](200) NULL,
	[Status] [nvarchar](50) NULL,
 CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductGallery]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductGallery](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductId] [int] NOT NULL,
	[ImageName] [nvarchar](70) NULL,
 CONSTRAINT [PK_ProductGallery] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[FullDescription] [nvarchar](4000) NULL,
	[Price] [money] NULL,
	[Discount] [money] NULL,
	[ImageName] [nvarchar](75) NULL,
	[Quantity] [int] NULL,
	[Tags] [nvarchar](1000) NULL,
	[VideoUrl] [nvarchar](400) NULL,
 CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[QRCodeScans]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[QRCodeScans](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[QRCodeData] [varchar](255) NOT NULL,
	[ScannedById] [int] NULL,
	[ScanDateTime] [datetime] NOT NULL,
	[ScanResult] [varchar](50) NULL,
	[ActionTaken] [varchar](50) NULL,
	[Notes] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Settings]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Settings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Shipping] [money] NULL,
 CONSTRAINT [PK_Settings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Supplier]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Supplier](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SupplierCode] [varchar](50) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[ContactPerson] [varchar](100) NULL,
	[Phone] [varchar](50) NULL,
	[Email] [varchar](100) NULL,
	[Address] [varchar](200) NULL,
	[RegistrationDate] [datetime] NOT NULL,
	[QRCodeData] [varchar](255) NOT NULL,
	[Status] [varchar](20) NULL,
	[Notes] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupplierCategory]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupplierCategory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Description] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupplierCategoryMapping]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupplierCategoryMapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SupplierId] [int] NULL,
	[CategoryId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SupplyItem]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SupplyItem](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](100) NOT NULL,
	[Category] [varchar](50) NOT NULL,
	[Unit] [varchar](20) NOT NULL,
	[Description] [varchar](500) NULL,
	[MinimumStock] [decimal](10, 2) NULL,
	[CurrentStock] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TeaInventoryItems]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TeaInventoryItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[TeaType] [nvarchar](50) NOT NULL,
	[Grade] [nvarchar](50) NOT NULL,
	[Origin] [nvarchar](100) NULL,
	[HarvestSeason] [nvarchar](50) NULL,
	[HarvestDate] [date] NULL,
	[BatchNumber] [nvarchar](50) NULL,
	[Description] [nvarchar](max) NULL,
	[CurrentStock] [decimal](18, 2) NOT NULL,
	[Unit] [nvarchar](10) NOT NULL,
	[MinimumStock] [decimal](18, 2) NULL,
	[ReorderLevel] [decimal](18, 2) NULL,
	[ReorderQuantity] [decimal](18, 2) NULL,
	[UnitCost] [decimal](18, 2) NULL,
	[RetailPrice] [decimal](18, 2) NULL,
	[Status] [nvarchar](20) NOT NULL,
	[QRCodeData] [nvarchar](100) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[LastUpdated] [datetime] NULL,
	[HasBeenCorrected] [bit] NOT NULL,
	[LastCorrectionDate] [datetime] NULL,
	[LastCorrectedBy] [nvarchar](100) NULL,
	[CorrectionReason] [nvarchar](max) NULL,
 CONSTRAINT [PK_TeaInventoryItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UK_TeaInventoryItems_QRCodeData] UNIQUE NONCLUSTERED 
(
	[QRCodeData] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TeaInventoryTransactions]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TeaInventoryTransactions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InventoryItemId] [int] NOT NULL,
	[TransactionDate] [datetime] NOT NULL,
	[TransactionType] [nvarchar](50) NOT NULL,
	[Quantity] [decimal](18, 2) NOT NULL,
	[PreviousStock] [decimal](18, 2) NOT NULL,
	[NewStock] [decimal](18, 2) NOT NULL,
	[ReferenceNumber] [nvarchar](50) NULL,
	[Notes] [nvarchar](max) NULL,
	[PerformedBy] [nvarchar](100) NULL,
	[IsCorrection] [bit] NOT NULL,
	[UnitPrice] [decimal](18, 2) NULL,
	[ReferenceId] [int] NULL,
	[CorrectionReason] [nvarchar](max) NULL,
	[QRCodeScanned] [nvarchar](100) NULL,
	[RelatedTransactionId] [int] NULL,
 CONSTRAINT [PK_TeaInventoryTransactions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 09/05/2025 3:11:40 am ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](50) NOT NULL,
	[FullName] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](50) NOT NULL,
	[IfAdmin] [bit] NOT NULL,
	[DateOfRegister] [datetime] NULL,
	[RecoveryCode] [int] NULL,
	[UserRole] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_TeaInventoryItems_BatchNumber]    Script Date: 09/05/2025 3:11:40 am ******/
CREATE NONCLUSTERED INDEX [IX_TeaInventoryItems_BatchNumber] ON [dbo].[TeaInventoryItems]
(
	[BatchNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_TeaInventoryItems_Status]    Script Date: 09/05/2025 3:11:40 am ******/
CREATE NONCLUSTERED INDEX [IX_TeaInventoryItems_Status] ON [dbo].[TeaInventoryItems]
(
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_TeaInventoryItems_TeaType]    Script Date: 09/05/2025 3:11:40 am ******/
CREATE NONCLUSTERED INDEX [IX_TeaInventoryItems_TeaType] ON [dbo].[TeaInventoryItems]
(
	[TeaType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TeaInventoryTransactions_InventoryItemId]    Script Date: 09/05/2025 3:11:40 am ******/
CREATE NONCLUSTERED INDEX [IX_TeaInventoryTransactions_InventoryItemId] ON [dbo].[TeaInventoryTransactions]
(
	[InventoryItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_TeaInventoryTransactions_TransactionDate]    Script Date: 09/05/2025 3:11:40 am ******/
CREATE NONCLUSTERED INDEX [IX_TeaInventoryTransactions_TransactionDate] ON [dbo].[TeaInventoryTransactions]
(
	[TransactionDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_TeaInventoryTransactions_TransactionType]    Script Date: 09/05/2025 3:11:40 am ******/
CREATE NONCLUSTERED INDEX [IX_TeaInventoryTransactions_TransactionType] ON [dbo].[TeaInventoryTransactions]
(
	[TransactionType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Delivery] ADD  DEFAULT (getdate()) FOR [DeliveryDate]
GO
ALTER TABLE [dbo].[Delivery] ADD  DEFAULT ('Received') FOR [Status]
GO
ALTER TABLE [dbo].[QRCodeScans] ADD  DEFAULT (getdate()) FOR [ScanDateTime]
GO
ALTER TABLE [dbo].[Supplier] ADD  DEFAULT (getdate()) FOR [RegistrationDate]
GO
ALTER TABLE [dbo].[Supplier] ADD  DEFAULT ('Active') FOR [Status]
GO
ALTER TABLE [dbo].[SupplyItem] ADD  DEFAULT ((0)) FOR [CurrentStock]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  CONSTRAINT [DF_TeaInventoryItems_Origin]  DEFAULT ('') FOR [Origin]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  CONSTRAINT [DF_TeaInventoryItems_HarvestSeason]  DEFAULT ('') FOR [HarvestSeason]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  CONSTRAINT [DF_TeaInventoryItems_BatchNumber]  DEFAULT ('') FOR [BatchNumber]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  CONSTRAINT [DF_TeaInventoryItems_Description]  DEFAULT ('') FOR [Description]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  DEFAULT ((0)) FOR [HasBeenCorrected]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  CONSTRAINT [DF_TeaInventoryItems_LastCorrectedBy]  DEFAULT ('') FOR [LastCorrectedBy]
GO
ALTER TABLE [dbo].[TeaInventoryItems] ADD  CONSTRAINT [DF_TeaInventoryItems_CorrectionReason]  DEFAULT ('') FOR [CorrectionReason]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  CONSTRAINT [DF_TeaInventoryTransactions_TransactionType]  DEFAULT ('') FOR [TransactionType]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  CONSTRAINT [DF_TeaInventoryTransactions_ReferenceNumber]  DEFAULT ('') FOR [ReferenceNumber]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  CONSTRAINT [DF_TeaInventoryTransactions_Notes]  DEFAULT ('') FOR [Notes]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  CONSTRAINT [DF_TeaInventoryTransactions_PerformedBy]  DEFAULT ('') FOR [PerformedBy]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  DEFAULT ((0)) FOR [IsCorrection]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  CONSTRAINT [DF_TeaInventoryTransactions_CorrectionReason]  DEFAULT ('') FOR [CorrectionReason]
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] ADD  CONSTRAINT [DF_TeaInventoryTransactions_QRCodeScanned]  DEFAULT ('') FOR [QRCodeScanned]
GO
ALTER TABLE [dbo].[Delivery]  WITH CHECK ADD  CONSTRAINT [FK_Delivery_Supplier] FOREIGN KEY([SupplierId])
REFERENCES [dbo].[Supplier] ([Id])
GO
ALTER TABLE [dbo].[Delivery] CHECK CONSTRAINT [FK_Delivery_Supplier]
GO
ALTER TABLE [dbo].[Delivery]  WITH CHECK ADD  CONSTRAINT [FK_Delivery_User] FOREIGN KEY([ReceivedById])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[Delivery] CHECK CONSTRAINT [FK_Delivery_User]
GO
ALTER TABLE [dbo].[DeliveryItem]  WITH CHECK ADD  CONSTRAINT [FK_DeliveryItem_Delivery] FOREIGN KEY([DeliveryId])
REFERENCES [dbo].[Delivery] ([Id])
GO
ALTER TABLE [dbo].[DeliveryItem] CHECK CONSTRAINT [FK_DeliveryItem_Delivery]
GO
ALTER TABLE [dbo].[DeliveryItem]  WITH CHECK ADD  CONSTRAINT [FK_DeliveryItem_SupplyItem] FOREIGN KEY([ItemId])
REFERENCES [dbo].[SupplyItem] ([Id])
GO
ALTER TABLE [dbo].[DeliveryItem] CHECK CONSTRAINT [FK_DeliveryItem_SupplyItem]
GO
ALTER TABLE [dbo].[QRCodeScans]  WITH CHECK ADD FOREIGN KEY([ScannedById])
REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[TeaInventoryTransactions]  WITH CHECK ADD  CONSTRAINT [FK_TeaInventoryTransactions_TeaInventoryItems] FOREIGN KEY([InventoryItemId])
REFERENCES [dbo].[TeaInventoryItems] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TeaInventoryTransactions] CHECK CONSTRAINT [FK_TeaInventoryTransactions_TeaInventoryItems]
GO
USE [master]
GO
ALTER DATABASE [TeaOnlineShop] SET  READ_WRITE 
GO

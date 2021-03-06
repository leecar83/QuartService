USE [Quartz]
GO
/****** Object:  Table [dbo].[JobRecords]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[JobRecords](
	[RecordId] [int] IDENTITY(1,1) NOT NULL,
	[JobId] [int] NULL,
	[JobName] [varchar](255) NULL,
	[BeginTime] [datetime] NULL,
	[EndTime] [datetime] NULL,
	[TimedOut] [int] NULL,
 CONSTRAINT [PK_JobRecords] PRIMARY KEY CLUSTERED 
(
	[RecordId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Jobs]    ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Jobs](
	[JobId] [int] NOT NULL,
	[JobName] [varchar](255) NULL,
	[JobGroup] [varchar](255) NULL,
	[Process] [varchar](255) NULL,
	[WorkingDirectory] [varchar](255) NULL,
	[Arguments] [varchar](255) NULL,
	[CronSchedule] [varchar](255) NULL,
	[TimeOut] [int] NULL,
 CONSTRAINT [PK__Job] PRIMARY KEY CLUSTERED 
(
	[JobId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

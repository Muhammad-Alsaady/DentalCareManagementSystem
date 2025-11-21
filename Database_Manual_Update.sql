-- =============================================
-- Manual Database Update Script
-- For Dental Care Management System
-- =============================================

USE [YourDatabaseName]; -- Replace with your actual database name
GO

-- =============================================
-- 1. CREATE PaymentTransactions TABLE
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND type in (N'U'))
BEGIN
    PRINT 'Creating PaymentTransactions table...';
    
    CREATE TABLE [dbo].[PaymentTransactions](
        [Id] [uniqueidentifier] NOT NULL,
        [PatientId] [uniqueidentifier] NOT NULL,
        [AppointmentId] [uniqueidentifier] NULL,
        [Amount] [decimal](18, 2) NOT NULL,
        [PaymentDate] [datetime2](7) NOT NULL,
        [CreatedBy] [nvarchar](450) NULL,
        [Notes] [nvarchar](max) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    PRINT 'PaymentTransactions table created successfully.';
END
ELSE
BEGIN
    PRINT 'PaymentTransactions table already exists.';
END
GO

-- =============================================
-- 2. CREATE FOREIGN KEYS
-- =============================================

-- FK to Patients
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PaymentTransactions_Patients_PatientId]'))
BEGIN
    PRINT 'Creating FK to Patients...';
    
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD CONSTRAINT [FK_PaymentTransactions_Patients_PatientId] 
    FOREIGN KEY([PatientId])
    REFERENCES [dbo].[Patients] ([Id])
    ON DELETE NO ACTION;  -- Restrict
    
    PRINT 'FK to Patients created.';
END
GO

-- FK to Appointments (with SET NULL on delete)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PaymentTransactions_Appointments_AppointmentId]'))
BEGIN
    PRINT 'Creating FK to Appointments...';
    
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD CONSTRAINT [FK_PaymentTransactions_Appointments_AppointmentId] 
    FOREIGN KEY([AppointmentId])
    REFERENCES [dbo].[Appointments] ([Id])
    ON DELETE SET NULL;
    
    PRINT 'FK to Appointments created.';
END
GO

-- FK to AspNetUsers
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PaymentTransactions_AspNetUsers_CreatedBy]'))
BEGIN
    PRINT 'Creating FK to AspNetUsers...';
    
    ALTER TABLE [dbo].[PaymentTransactions]
    ADD CONSTRAINT [FK_PaymentTransactions_AspNetUsers_CreatedBy] 
    FOREIGN KEY([CreatedBy])
    REFERENCES [dbo].[AspNetUsers] ([Id])
    ON DELETE NO ACTION;  -- Restrict
    
    PRINT 'FK to AspNetUsers created.';
END
GO

-- =============================================
-- 3. CREATE INDEXES for Performance
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_PatientId')
BEGIN
    PRINT 'Creating index on PatientId...';
    
    CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_PatientId] 
    ON [dbo].[PaymentTransactions]([PatientId] ASC);
    
    PRINT 'Index on PatientId created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_AppointmentId')
BEGIN
    PRINT 'Creating index on AppointmentId...';
    
    CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_AppointmentId] 
    ON [dbo].[PaymentTransactions]([AppointmentId] ASC);
    
    PRINT 'Index on AppointmentId created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_CreatedBy')
BEGIN
    PRINT 'Creating index on CreatedBy...';
    
    CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_CreatedBy] 
    ON [dbo].[PaymentTransactions]([CreatedBy] ASC);
    
    PRINT 'Index on CreatedBy created.';
END
GO

-- =============================================
-- 4. ADD PaidAmount to Appointments (if not exists)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') 
               AND name = 'PaidAmount')
BEGIN
    PRINT 'Adding PaidAmount column to Appointments...';
    
    ALTER TABLE [dbo].[Appointments]
    ADD [PaidAmount] [decimal](18, 2) NOT NULL DEFAULT 0;
    
    PRINT 'PaidAmount column added to Appointments.';
END
ELSE
BEGIN
    PRINT 'PaidAmount column already exists in Appointments.';
END
GO

-- =============================================
-- 5. INSERT Migration History Records
-- =============================================

-- Add migration record for AddPaidAmountToAppointment
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] 
               WHERE [MigrationId] = '20251115201004_AddPaidAmountToAppointment')
BEGIN
    PRINT 'Adding migration history for AddPaidAmountToAppointment...';
    
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251115201004_AddPaidAmountToAppointment', '9.0.10');
    
    PRINT 'Migration history added for AddPaidAmountToAppointment.';
END
GO

-- Add migration record for AddPaymentTransactionEntity
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] 
               WHERE [MigrationId] = '20251121161029_AddPaymentTransactionEntity')
BEGIN
    PRINT 'Adding migration history for AddPaymentTransactionEntity...';
    
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251121161029_AddPaymentTransactionEntity', '9.0.10');
    
    PRINT 'Migration history added for AddPaymentTransactionEntity.';
END
GO

-- =============================================
-- 6. VERIFICATION QUERIES
-- =============================================

PRINT '';
PRINT '===========================================';
PRINT 'VERIFICATION RESULTS';
PRINT '===========================================';

-- Check if PaymentTransactions table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]'))
    PRINT '? PaymentTransactions table exists';
ELSE
    PRINT '? PaymentTransactions table MISSING!';

-- Check if PaidAmount column exists in Appointments
IF EXISTS (SELECT * FROM sys.columns 
           WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') 
           AND name = 'PaidAmount')
    PRINT '? Appointments.PaidAmount column exists';
ELSE
    PRINT '? Appointments.PaidAmount column MISSING!';

-- Count foreign keys on PaymentTransactions
DECLARE @fkCount INT;
SELECT @fkCount = COUNT(*) 
FROM sys.foreign_keys 
WHERE parent_object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]');
PRINT '? PaymentTransactions has ' + CAST(@fkCount AS NVARCHAR) + ' foreign keys (expected: 3)';

-- Count indexes on PaymentTransactions
DECLARE @idxCount INT;
SELECT @idxCount = COUNT(*) 
FROM sys.indexes 
WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]')
AND name LIKE 'IX_%';
PRINT '? PaymentTransactions has ' + CAST(@idxCount AS NVARCHAR) + ' indexes (expected: 3)';

-- Check migration history
IF EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] 
           WHERE [MigrationId] IN ('20251115201004_AddPaidAmountToAppointment', 
                                   '20251121161029_AddPaymentTransactionEntity'))
    PRINT '? Migration history records exist';
ELSE
    PRINT '? Migration history records MISSING!';

PRINT '';
PRINT '===========================================';
PRINT 'DATABASE UPDATE COMPLETE!';
PRINT '===========================================';
GO

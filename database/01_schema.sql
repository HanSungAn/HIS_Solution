-- HIS Portfolio Database Schema (MSSQL)
IF DB_ID(N'HIS_DB') IS NULL
BEGIN
    CREATE DATABASE [HIS_DB];
END
GO

USE [HIS_DB];
GO

-- 1. 공통코드
IF OBJECT_ID(N'dbo.CommonCodes', N'U') IS NULL
CREATE TABLE dbo.CommonCodes (
    GroupCode   NVARCHAR(30)  NOT NULL,
    Code        NVARCHAR(30)  NOT NULL,
    CodeName    NVARCHAR(100) NOT NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_CommonCodes PRIMARY KEY (GroupCode, Code)
);
GO

-- 2. 사용자 (의사/간호사/원무/관리자)
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    LoginId      NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    UserName     NVARCHAR(50)  NOT NULL,
    Role         NVARCHAR(20)  NOT NULL, -- Admin, Doctor, Nurse, Desk
    DeptCode     NVARCHAR(30)  NULL,
    LicenseNo    NVARCHAR(30)  NULL,
    IsActive           BIT           NOT NULL DEFAULT 1,
    ActiveSessionToken NVARCHAR(64)  NULL,
    LastLoginAt        DATETIME2     NULL,
    LastActiveAt       DATETIME2     NULL,
    CreatedAt          DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

IF COL_LENGTH(N'dbo.Users', N'ActiveSessionToken') IS NULL
    ALTER TABLE dbo.Users ADD ActiveSessionToken NVARCHAR(64) NULL, LastLoginAt DATETIME2 NULL, LastActiveAt DATETIME2 NULL;
GO

-- 3. 상병 마스터 (KCD)
IF OBJECT_ID(N'dbo.DiseaseMaster', N'U') IS NULL
CREATE TABLE dbo.DiseaseMaster (
    KcdCode     NVARCHAR(20)  PRIMARY KEY,
    NameKr      NVARCHAR(200) NOT NULL,
    NameEn      NVARCHAR(200) NULL
);
GO

-- 4. 처방/수가 마스터 (약품/검사/처치)
IF OBJECT_ID(N'dbo.OrderMaster', N'U') IS NULL
CREATE TABLE dbo.OrderMaster (
    OrderCode   NVARCHAR(30)   PRIMARY KEY,
    OrderType   NVARCHAR(20)   NOT NULL, -- Drug, Lab, Treat
    OrderName   NVARCHAR(150)  NOT NULL,
    UnitPrice   DECIMAL(12, 2) NOT NULL,
    IsCovered   BIT            NOT NULL DEFAULT 1, -- 1: 급여, 0: 비급여
    Unit        NVARCHAR(20)   NULL,
    IsActive    BIT            NOT NULL DEFAULT 1
);
GO

-- 5. 환자 마스터
IF OBJECT_ID(N'dbo.Patients', N'U') IS NULL
CREATE TABLE dbo.Patients (
    PatientId     INT IDENTITY(1,1) PRIMARY KEY,
    ChartNo       NVARCHAR(20)  NOT NULL UNIQUE,
    PatientName   NVARCHAR(50)  NOT NULL,
    BirthDate     DATE          NOT NULL,
    Gender        CHAR(1)       NOT NULL, -- M, F
    SsnEncrypted  NVARCHAR(256) NULL,
    SsnMasked     NVARCHAR(20)  NULL,     -- 예: 900101-1******
    Phone         NVARCHAR(20)  NULL,
    Address       NVARCHAR(200) NULL,
    InsuranceType NVARCHAR(20)  NOT NULL DEFAULT N'NHI', -- NHI(건보), AID(의료급여), GEN(일반)
    CreatedAt     DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 6. 예약
IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
CREATE TABLE dbo.Appointments (
    ApptId      INT IDENTITY(1,1) PRIMARY KEY,
    PatientId   INT           NOT NULL FOREIGN KEY REFERENCES dbo.Patients(PatientId),
    DoctorId    INT           NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserId),
    DeptCode    NVARCHAR(30)  NOT NULL,
    ApptStart   DATETIME2     NOT NULL,
    ApptEnd     DATETIME2     NOT NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT N'Scheduled', -- Scheduled, Arrived, Cancelled
    Memo        NVARCHAR(300) NULL,
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 7. 외래 접수 / 내원
IF OBJECT_ID(N'dbo.Visits', N'U') IS NULL
CREATE TABLE dbo.Visits (
    VisitId       INT IDENTITY(1,1) PRIMARY KEY,
    PatientId     INT          NOT NULL FOREIGN KEY REFERENCES dbo.Patients(PatientId),
    DoctorId      INT          NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserId),
    DeptCode      NVARCHAR(30) NOT NULL,
    VisitDate     DATE         NOT NULL DEFAULT CAST(SYSDATETIME() AS DATE),
    Status        NVARCHAR(20) NOT NULL DEFAULT N'Wait', -- Wait, InCare, PayWait, Done, Cancelled
    ChiefMemo     NVARCHAR(300) NULL,
    CreatedBy     INT          NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserId),
    CreatedAt     DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt     DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    RowVer        ROWVERSION   NOT NULL
);
GO

-- 8. 진료 기록 (SOAP)
IF OBJECT_ID(N'dbo.MedicalRecords', N'U') IS NULL
CREATE TABLE dbo.MedicalRecords (
    RecordId      INT IDENTITY(1,1) PRIMARY KEY,
    VisitId       INT            NOT NULL UNIQUE FOREIGN KEY REFERENCES dbo.Visits(VisitId),
    DoctorId      INT            NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserId),
    Subjective    NVARCHAR(MAX)  NULL,
    Objective     NVARCHAR(MAX)  NULL,
    Assessment    NVARCHAR(MAX)  NULL,
    PlanNote      NVARCHAR(MAX)  NULL,
    MainKcdCode   NVARCHAR(20)   NULL FOREIGN KEY REFERENCES dbo.DiseaseMaster(KcdCode),
    RecordedAt    DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 9. 바이탈 사인
IF OBJECT_ID(N'dbo.VitalSigns', N'U') IS NULL
CREATE TABLE dbo.VitalSigns (
    VitalId       INT IDENTITY(1,1) PRIMARY KEY,
    VisitId       INT           NOT NULL FOREIGN KEY REFERENCES dbo.Visits(VisitId),
    PatientId     INT           NOT NULL FOREIGN KEY REFERENCES dbo.Patients(PatientId),
    SystolicBP    INT           NULL,
    DiastolicBP   INT           NULL,
    Pulse         INT           NULL,
    Temperature   DECIMAL(4,1)  NULL,
    MeasuredAt    DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 10. 처방 오더
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
    OrderId       INT IDENTITY(1,1) PRIMARY KEY,
    VisitId       INT            NOT NULL FOREIGN KEY REFERENCES dbo.Visits(VisitId),
    OrderCode     NVARCHAR(30)   NOT NULL FOREIGN KEY REFERENCES dbo.OrderMaster(OrderCode),
    Dosage        DECIMAL(10, 2) NOT NULL DEFAULT 1,
    Frequency     INT            NOT NULL DEFAULT 1,
    Days          INT            NOT NULL DEFAULT 1,
    Status        NVARCHAR(20)   NOT NULL DEFAULT N'Ordered', -- Ordered, LabDone, Dispensed, Cancelled
    ResultValue   NVARCHAR(200)  NULL,
    IsAbnormal    BIT            NOT NULL DEFAULT 0,
    OrderedAt     DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 11. 수납 내역
IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
CREATE TABLE dbo.Payments (
    PaymentId         INT IDENTITY(1,1) PRIMARY KEY,
    VisitId           INT            NOT NULL FOREIGN KEY REFERENCES dbo.Visits(VisitId),
    TotalAmount       DECIMAL(12, 2) NOT NULL,
    CoveredAmount     DECIMAL(12, 2) NOT NULL,
    NonCoveredAmount  DECIMAL(12, 2) NOT NULL,
    PatientPaidAmount DECIMAL(12, 2) NOT NULL,
    PayMethod         NVARCHAR(20)   NOT NULL, -- Card, Cash, Transfer
    CashierId         INT            NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserId),
    PaidAt            DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 12. 감사 로그 (개인정보 조회/수정/출력 이력)
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
CREATE TABLE dbo.AuditLogs (
    LogId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT           NOT NULL,
    UserName        NVARCHAR(50)  NOT NULL,
    ActionType      NVARCHAR(30)  NOT NULL, -- View, Create, Update, Print, Login
    TargetModule    NVARCHAR(50)  NOT NULL,
    TargetPatientId INT           NULL,
    Detail          NVARCHAR(500) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME()
);
GO

-- 13. 수납 내역 이력
IF OBJECT_ID(N'dbo.PaymentHistories', N'U') IS NULL
CREATE TABLE dbo.PaymentHistories (
    HistoryId         BIGINT IDENTITY(1,1) PRIMARY KEY,
    PaymentId         INT            NOT NULL FOREIGN KEY REFERENCES dbo.Payments(PaymentId),
    VisitId           INT            NOT NULL FOREIGN KEY REFERENCES dbo.Visits(VisitId),
    PatientId         INT            NOT NULL FOREIGN KEY REFERENCES dbo.Patients(PatientId),
    ActionType        NVARCHAR(20)   NOT NULL DEFAULT N'Paid', -- Paid, Cancelled, Refunded
    ReceiptNo         NVARCHAR(30)   NOT NULL,
    TotalAmount       DECIMAL(12, 2) NOT NULL,
    CoveredAmount     DECIMAL(12, 2) NOT NULL,
    NonCoveredAmount  DECIMAL(12, 2) NOT NULL,
    PatientPaidAmount DECIMAL(12, 2) NOT NULL,
    PayMethod         NVARCHAR(20)   NOT NULL, -- Card, Cash, Transfer
    CashierId         INT            NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserId),
    Memo              NVARCHAR(300)  NULL,
    OccurredAt        DATETIME2      NOT NULL DEFAULT SYSDATETIME()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PaymentHistories_OccurredAt' AND object_id = OBJECT_ID(N'dbo.PaymentHistories'))
CREATE INDEX IX_PaymentHistories_OccurredAt ON dbo.PaymentHistories (OccurredAt DESC);
GO

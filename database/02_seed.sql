USE [HIS_DB];
GO

-- 공통코드 (진료과, 보험유형)
MERGE dbo.CommonCodes AS target
USING (VALUES
    (N'DEPT', N'IM',  N'내과',       1),
    (N'DEPT', N'OS',  N'정형외과',   2),
    (N'DEPT', N'PED', N'소아청소년과', 3),
    (N'INS',  N'NHI', N'건강보험',   1),
    (N'INS',  N'AID', N'의료급여',   2),
    (N'INS',  N'GEN', N'일반(비급여)', 3)
) AS src(GroupCode, Code, CodeName, SortOrder)
ON target.GroupCode = src.GroupCode AND target.Code = src.Code
WHEN NOT MATCHED THEN
    INSERT (GroupCode, Code, CodeName, SortOrder) VALUES (src.GroupCode, src.Code, src.CodeName, src.SortOrder);
GO

-- 초기 사용자 (비밀번호: 1234 -> SHA256)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE LoginId = N'admin')
INSERT INTO dbo.Users (LoginId, PasswordHash, UserName, Role, DeptCode, LicenseNo) VALUES
(N'admin',  N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'시스템관리자', N'Admin',  NULL,   NULL),
(N'desk1',  N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'김원무',       N'Desk',   NULL,   NULL),
(N'doc1',   N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'이진료',       N'Doctor', N'IM',  N'MD-10234'),
(N'doc2',   N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'박정형',       N'Doctor', N'OS',  N'MD-20411'),
(N'nurse1', N'03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', N'최간호',       N'Nurse',  N'IM',  N'RN-50112');
GO

-- 상병 마스터 (KCD 샘플)
MERGE dbo.DiseaseMaster AS target
USING (VALUES
    (N'J00',   N'급성 비인두염(감기)',         N'Acute nasopharyngitis'),
    (N'J06.9', N'상세불명의 급성 상기도감염',   N'Acute upper respiratory infection'),
    (N'I10.9', N'상세불명의 본태성 고혈압',     N'Essential hypertension'),
    (N'E11.9', N'합병증을 동반하지 않은 2형 당뇨병', N'Type 2 diabetes mellitus'),
    (N'K29.7', N'상세불명의 위염',              N'Gastritis, unspecified'),
    (N'M54.5', N'요통',                         N'Low back pain')
) AS src(KcdCode, NameKr, NameEn)
ON target.KcdCode = src.KcdCode
WHEN NOT MATCHED THEN
    INSERT (KcdCode, NameKr, NameEn) VALUES (src.KcdCode, src.NameKr, src.NameEn);
GO

-- 처방/수가 마스터
MERGE dbo.OrderMaster AS target
USING (VALUES
    (N'FEE-001', N'Treat', N'초진 진찰료',              17500, 1, N'회'),
    (N'FEE-002', N'Treat', N'재진 진찰료',              12500, 1, N'회'),
    (N'DRG-101', N'Drug',  N'타이레놀8시간이알서방정',    350,   1, N'정'),
    (N'DRG-102', N'Drug',  N'아목시실린캡슐 500mg',       420,   1, N'캡슐'),
    (N'DRG-103', N'Drug',  N'넥시움정 20mg',             1100,  1, N'정'),
    (N'LAB-201', N'Lab',   N'일반혈액검사(CBC)',        15000, 1, N'건'),
    (N'LAB-202', N'Lab',   N'간기능검사(LFT)',          22000, 1, N'건'),
    (N'TRT-301', N'Treat', N'비급여 영양수액(마이어스)', 60000, 0, N'병')
) AS src(OrderCode, OrderType, OrderName, UnitPrice, IsCovered, Unit)
ON target.OrderCode = src.OrderCode
WHEN NOT MATCHED THEN
    INSERT (OrderCode, OrderType, OrderName, UnitPrice, IsCovered, Unit)
    VALUES (src.OrderCode, src.OrderType, src.OrderName, src.UnitPrice, src.IsCovered, src.Unit);
GO

-- 샘플 환자
IF NOT EXISTS (SELECT 1 FROM dbo.Patients WHERE ChartNo = N'P20260001')
INSERT INTO dbo.Patients (ChartNo, PatientName, BirthDate, Gender, SsnMasked, Phone, Address, InsuranceType) VALUES
(N'P20260001', N'홍길동', '1985-03-15', 'M', N'850315-1******', N'010-1234-5678', N'서울시 강남구 테헤란로 101', N'NHI'),
(N'P20260002', N'김영희', '1992-07-22', 'F', N'920722-2******', N'010-2345-6789', N'서울시 서초구 서초대로 55', N'NHI'),
(N'P20260003', N'이철수', '1970-11-04', 'M', N'701104-1******', N'010-3456-7890', N'경기도 성남시 분당구 판교역로 12', N'AID');
GO

-- 샘플 당일 접수 (대기 1건, 수납대기 0건)
IF NOT EXISTS (SELECT 1 FROM dbo.Visits)
BEGIN
    DECLARE @p1 INT = (SELECT PatientId FROM dbo.Patients WHERE ChartNo = N'P20260001');
    DECLARE @p2 INT = (SELECT PatientId FROM dbo.Patients WHERE ChartNo = N'P20260002');
    DECLARE @doc1 INT = (SELECT UserId FROM dbo.Users WHERE LoginId = N'doc1');
    DECLARE @desk1 INT = (SELECT UserId FROM dbo.Users WHERE LoginId = N'desk1');

    INSERT INTO dbo.Visits (PatientId, DoctorId, DeptCode, Status, ChiefMemo, CreatedBy) VALUES
    (@p1, @doc1, N'IM', N'Wait', N'발열 및 인후통 3일째', @desk1),
    (@p2, @doc1, N'IM', N'Wait', N'정기 혈압 체크 및 약 처방', @desk1);
END
GO

-- 기존 Payments 데이터 수납 내역 이력 백필 (중복 방지)
INSERT INTO dbo.PaymentHistories (
    PaymentId, VisitId, PatientId, ActionType, ReceiptNo,
    TotalAmount, CoveredAmount, NonCoveredAmount, PatientPaidAmount,
    PayMethod, CashierId, Memo, OccurredAt
)
SELECT
    p.PaymentId,
    p.VisitId,
    v.PatientId,
    N'Paid',
    CONCAT(N'RCP-', FORMAT(p.PaidAt, 'yyyyMMdd'), N'-', RIGHT('0000' + CAST(p.PaymentId AS VARCHAR(10)), 4)),
    p.TotalAmount,
    p.CoveredAmount,
    p.NonCoveredAmount,
    p.PatientPaidAmount,
    p.PayMethod,
    p.CashierId,
    N'기존 수납 내역 이관',
    p.PaidAt
FROM dbo.Payments p
INNER JOIN dbo.Visits v ON p.VisitId = v.VisitId
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PaymentHistories h WHERE h.PaymentId = p.PaymentId
);
GO

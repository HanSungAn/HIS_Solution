namespace HIS.Core.Entities;

/// <summary>
/// 외래 접수/내원 정보 엔티티 (DB: dbo.Visits)
/// </summary>
public class Visit
{
    public int VisitId { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public string DeptCode { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string Status { get; set; } = "Wait"; // Wait -> InCare -> PayWait -> Done
    public string? ChiefMemo { get; set; }       // 내원 사유 / 주호소
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 낙관적 동시성 제어(Optimistic Concurrency)용 타임스탬프.
    /// 여러 사용자가 동시에 같은 접수 건을 수정할 때 충돌을 감지합니다.
    /// </summary>
    public byte[] RowVer { get; set; } = [];

    // --- 아래는 화면 표시를 위해 Patients/Users 테이블과 JOIN하여 채우는 보조 필드 ---
    public string ChartNo { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string InsuranceType { get; set; } = "NHI";
}

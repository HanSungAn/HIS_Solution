namespace HIS.Core.Entities;

/// <summary>
/// 환자 마스터 엔티티 (DB: dbo.Patients 테이블과 1:1 매핑)
/// </summary>
public class Patient
{
    public int PatientId { get; set; }
    public string ChartNo { get; set; } = string.Empty;      // 병원 고유 차트번호 (예: P20260001)
    public string PatientName { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = "M";                // 성별 (M: 남, F: 여)
    public string? SsnEncrypted { get; set; }                // 암호화된 주민번호 (선택)
    public string? SsnMasked { get; set; }                   // 화면 표시용 마스킹 주민번호
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string InsuranceType { get; set; } = "NHI";       // 보험유형 (NHI: 건강보험, AID: 의료급여, GEN: 일반)
}

namespace HIS.Core.Enums;

/// <summary>
/// 병원 시스템 사용자 직군(권한) 구분
/// </summary>
public enum UserRole
{
    Admin,  // 시스템 관리자
    Doctor, // 의사 (진료/처방)
    Nurse,  // 간호사
    Desk    // 원무과 (접수/수납)
}

/// <summary>
/// 외래 접수 상태 흐름: Wait(대기) -> InCare(진료중) -> PayWait(수납대기) -> Done(완료)
/// </summary>
public enum VisitStatus
{
    Wait,      // 진료 대기
    InCare,    // 진료 중
    PayWait,   // 수납 대기
    Done,      // 수납 및 진료 완료
    Cancelled  // 접수 취소
}

/// <summary>
/// 처방(오더) 유형 구분
/// </summary>
public enum OrderType
{
    Drug,  // 약품
    Lab,   // 검사
    Treat  // 처치/진찰료
}

/// <summary>
/// 처방 진행 상태
/// </summary>
public enum OrderStatus
{
    Ordered,   // 처방 발행됨
    LabDone,   // 검사 완료
    Dispensed, // 약국 조제 완료
    Cancelled  // 처방 취소
}

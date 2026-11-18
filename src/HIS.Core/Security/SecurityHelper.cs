using System.Security.Cryptography;
using System.Text;

namespace HIS.Core.Security;

/// <summary>
/// 비밀번호 암호화(해시) 및 개인정보 마스킹을 담당하는 보안 유틸리티
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// 평문 비밀번호를 SHA-256 단방향 해시 문자열(소문자 16진수)로 변환합니다.
    /// DB에는 원본 비밀번호 대신 이 해시값을 저장하여 보안을 유지합니다.
    /// </summary>
    public static string HashPassword(string plainText)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 주민등록번호 뒷자리를 마스킹합니다. (예: 900101-1234567 -> 900101-1******)
    /// 숫자만 추출한 뒤 앞 6자리와 성별 구분 1자리만 남기고 나머지는 '*'로 가립니다.
    /// </summary>
    public static string MaskSsn(string? ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn)) return string.Empty;
        var digits = new string(ssn.Where(char.IsDigit).ToArray());
        if (digits.Length < 7) return "******-*******";
        return $"{digits[..6]}-{digits[6]}******";
    }
}

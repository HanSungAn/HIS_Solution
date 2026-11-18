using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

public class PatientRepository(IDbConnectionFactory dbFactory)
{
    public async Task<IEnumerable<Patient>> SearchAsync(string keyword)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP 50 PatientId, ChartNo, PatientName, BirthDate, Gender, SsnMasked, Phone, Address, InsuranceType
            FROM dbo.Patients
            WHERE ChartNo LIKE @Kw OR PatientName LIKE @Kw OR Phone LIKE @Kw
            ORDER BY PatientId DESC
            """;
        return await conn.QueryAsync<Patient>(sql, new { Kw = $"%{keyword}%" });
    }

    public async Task<int> InsertAsync(Patient patient)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            INSERT INTO dbo.Patients (ChartNo, PatientName, BirthDate, Gender, SsnEncrypted, SsnMasked, Phone, Address, InsuranceType)
            OUTPUT INSERTED.PatientId
            VALUES (@ChartNo, @PatientName, @BirthDate, @Gender, @SsnEncrypted, @SsnMasked, @Phone, @Address, @InsuranceType)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, patient);
    }

    public async Task<string> GenerateNextChartNoAsync()
    {
        using var conn = dbFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) + 1 FROM dbo.Patients");
        return $"P{DateTime.Now:yyyy}{count:D4}";
    }
}

using Dapper;
using HIS.Core.Entities;

namespace HIS.Data.Repositories;

public class MasterRepository(IDbConnectionFactory dbFactory)
{
    public async Task<IEnumerable<DiseaseMaster>> SearchDiseasesAsync(string keyword)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP 30 KcdCode, NameKr, NameEn
            FROM dbo.DiseaseMaster
            WHERE KcdCode LIKE @Kw OR NameKr LIKE @Kw
            ORDER BY KcdCode
            """;
        return await conn.QueryAsync<DiseaseMaster>(sql, new { Kw = $"%{keyword}%" });
    }

    public async Task<IEnumerable<OrderMaster>> SearchOrdersAsync(string keyword)
    {
        using var conn = dbFactory.CreateConnection();
        const string sql = """
            SELECT TOP 50 OrderCode, OrderType, OrderName, UnitPrice, IsCovered, Unit
            FROM dbo.OrderMaster
            WHERE IsActive = 1 AND (OrderCode LIKE @Kw OR OrderName LIKE @Kw)
            ORDER BY OrderType, OrderCode
            """;
        return await conn.QueryAsync<OrderMaster>(sql, new { Kw = $"%{keyword}%" });
    }
}

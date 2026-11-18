using System.Data;
using Microsoft.Data.SqlClient;

namespace HIS.Data;

/// <summary>
/// 데이터베이스 연결을 생성하는 팩토리 인터페이스.
/// Repository들이 직접 연결 문자열을 알지 않도록 추상화합니다.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

/// <summary>
/// MSSQL(SQL Server) 연결 객체를 생성하고 Open 상태로 반환합니다.
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }
}

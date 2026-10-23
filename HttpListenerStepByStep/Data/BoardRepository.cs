using System.Data;
using HttpListenerStepByStep.Models;
using Npgsql;
using NpgsqlTypes;

namespace HttpListenerStepByStep.Data;

public class BoardRepository(string connectionString)
    : BaseRepository(connectionString)
{
    public async Task<Guid> CreateAsync(string title, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        
        //Пример вызова функции или процедуры в базовом варианте. Не через Npgsql
        /*
            await using var command = new NpgsqlCommand("CALL public.create_board($1, NULL)", connection);
            command.Parameters.Add(new NpgsqlParameter
            {
                NpgsqlDbType = NpgsqlDbType.Varchar,
                Value = title
            });
            var result = await command.ExecuteScalarAsync(ct);
         */
        
        
        await using var command = new NpgsqlCommand("public.create_board", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("p_title", NpgsqlDbType.Varchar, title);
        command.Parameters.Add(new NpgsqlParameter("p_id", NpgsqlDbType.Uuid)
        {
            Direction = ParameterDirection.Output
        });

        var result = await command.ExecuteScalarAsync(ct);
        return result is Guid id
            ? id
            : throw new InvalidOperationException("Процедура не вернула UUID");
        
        
        //альтернативный вариант
        /*
         * await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException("Процедура не вернула UUID");
            return reader.GetGuid(reader.GetOrdinal("p_id"));
         */
        
        
        
        //третий вариант, вместо Scalar или reader
        /*
        await command.ExecuteNonQueryAsync(ct);
        return command.Parameters["p_id"].Value is Guid id
            ? id
            : throw new InvalidOperationException("Процедура не вернула UUID");
        */
    }
    
    public async Task<Board?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("SELECT * FROM public.get_board($1)", connection);
        
        command.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Uuid, Value = id
        });
        
        await using var reader = await command.ExecuteReaderAsync(ct);
        
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new Board
        {
            Id = reader.GetGuid(0),
            Title = reader.GetString(1),
            CreatedAt = reader.GetDateTime(2)
        };
    }
    
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand("public.delete_board", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("p_board_id", NpgsqlDbType.Uuid, id);
        command.Parameters.Add(new NpgsqlParameter("p_found", NpgsqlDbType.Boolean)
        {
            Direction = ParameterDirection.Output
        });
        
        return (bool)(await command.ExecuteScalarAsync(ct)
                      ?? throw new InvalidOperationException("Нет результата удаления"));
    }
}
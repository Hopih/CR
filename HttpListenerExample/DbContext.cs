using System.Data;
using CodeJam;
using HttpListenerExample.Entities;
using Npgsql;
using NpgsqlTypes;

namespace HttpListenerExample;

public class DbContext
{
    private const string DbConnectionString = "Host=localhost;Username=postgres;Password=postgres;Database=news_portal";
    private readonly NpgsqlConnection _dbConnection;

    public DbContext()
    {
        _dbConnection = new NpgsqlConnection(DbConnectionString);
    }

    public async Task CreateDbAsync(CancellationToken ctx = default)
    {
        await _dbConnection.OpenAsync(ctx);

        await using var transaction = await _dbConnection.BeginTransactionAsync(IsolationLevel.Serializable, ctx);
        try
        {
            await using var command = _dbConnection.CreateCommand();

            //Создаем таблички если их нет 
            command.CommandText = $@"CREATE TABLE IF NOT EXISTS public.users (
	        id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
            email varchar(64) NOT NULL,
	        full_name varchar(256) NOT NULL,
            password varchar(512) NOT NULL,
            CONSTRAINT users_pk PRIMARY KEY (id));

            CREATE INDEX users_id_idx ON public.users (id);";

            await command.ExecuteNonQueryAsync(ctx);

            command.CommandText = $@"CREATE TABLE IF NOT EXISTS public.news (
	        id uuid NOT NULL,
	        title varchar(128) NULL,
	        ""content"" text NULL,
	        author_id bigint NOT NULL,
	        
            CONSTRAINT news_pk PRIMARY KEY (id),
	        CONSTRAINT news_fk FOREIGN KEY (author_id) REFERENCES public.users(id));

            CREATE INDEX news_id_idx ON public.news (id,author_id);";
            //CONSTRAINT con1 CHECK (did > 100 AND name <> '') пример иных ограничений
            await command.ExecuteNonQueryAsync(ctx);

            await transaction.CommitAsync(ctx);
        }
        catch
        {
            await transaction.RollbackAsync(ctx);
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    //Вариант как делать не надо!
    public async Task<User> GetUserByEmailAsync(string email, CancellationToken ctx = default)
    {
        await _dbConnection.OpenAsync(ctx);
        try
        {
            await using var command = _dbConnection.CreateCommand();

            //Создаем таблички если их нет 
            command.CommandText = $@"select * from users where email = '{email}'";
            await using var reader = await command.ExecuteReaderAsync(ctx);

            if (reader.HasRows && await reader.ReadAsync(ctx))
            {
                return new User
                {
                    Id = reader.GetInt64("id"),
                    Email = reader.GetString("email"),
                    FullName = reader.GetString("full_name"),
                    News = new List<News>() //тут можно выполнить доп запрос с join или сделать комплексный вызов
                };
            }
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        throw new ArgumentException(nameof(email));
    }

    //Вариант с передачей параметров
    public async Task<List<News>> GetNewsByUserAsync(User user, CancellationToken ctx = default)
    {
        await _dbConnection.OpenAsync(ctx);
        try
        {
            await using var command = _dbConnection.CreateCommand();

            command.Parameters.Add("userId", NpgsqlDbType.Bigint);

            //Создаем таблички если их нет 
            command.CommandText = $@"select * from news where authorId = @userId ";

            await using var reader = await command.ExecuteReaderAsync(ctx);
            var news = new List<News>();
            while (reader.HasRows && await reader.ReadAsync(ctx))
            {
                news.Add(new News
                {
                    Id = reader.GetGuid("id"),
                    AuthorId = user.Id,
                    Content = reader.GetString("content"),
                    Title = reader.GetString("title")
                });
            }

            return news;
        }
        catch
        {
            //Тут можно отловить ошибки уровня БД при желании и ими управлять
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        throw new ArgumentException(nameof(user));
    }

    //Вариант с экранированием параметров
    public async Task<User> GetUserWithNewsAsync(string email, CancellationToken ctx = default)
    {
        await _dbConnection.OpenAsync(ctx);
        try
        {
            await using var command = _dbConnection.CreateCommand();

            //Создаем таблички если их нет 
            command.CommandText = $@"select * from users as us 
                inner join news as n on us.id = n.author_id where us.email = '{email}'";
            await using var reader = await command.ExecuteReaderAsync(ctx);

            if (await reader.ReadAsync(ctx))
            {
                var user = new User
                {
                    Id = reader.GetInt64("us.id"),
                    Email = reader.GetString("us.email"),
                    FullName = reader.GetString("us.full_name"),
                    News = new List<News>
                    {
                        new News
                        {
                            AuthorId = reader.GetInt64("n.author_id"),
                            Id = reader.GetGuid("n.id"),
                            Title = reader.GetString("n.title"),
                            Content = reader.GetString("n.content")
                        }
                    }
                };
                while (await reader.ReadAsync(ctx))
                {
                    user.News.Add(new News
                    {
                        AuthorId = reader.GetInt32("n.author_id"),
                        Id = reader.GetGuid("n.id"),
                        Title = reader.GetString("n.title"),
                        Content = reader.GetString("n.content")
                    });
                }
            }
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        throw new ArgumentException(nameof(email));
    }

    public async Task AddUserAsync(User user, CancellationToken ctx)
    {
        await _dbConnection.OpenAsync(ctx);

        try
        {
            await using var command = _dbConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "_full_name",
                NpgsqlDbType = NpgsqlDbType.Varchar,
                Direction = ParameterDirection.Input,
                Value = user.FullName
            });
            command.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "_email",
                NpgsqlDbType = NpgsqlDbType.Varchar,
                Direction = ParameterDirection.Input,
                Value = user.Email
            });
            command.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "_out_result",
                NpgsqlDbType = NpgsqlDbType.Varchar,
                Direction = ParameterDirection.Output
            });

            command.CommandText = "public.insert_user";
            var result = await command.ExecuteNonQueryAsync(ctx);

            Console.WriteLine(command.Parameters["_out_result"].Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToDiagnosticString());
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

}
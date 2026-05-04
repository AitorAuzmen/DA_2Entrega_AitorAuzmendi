using MySqlConnector;

namespace ZinemaKudeaketa;

public sealed class ZinemaRepository
{
    private readonly string logFile;
    private readonly string connectionString;

    public ZinemaRepository()
    {
        logFile = Path.Combine(FindProjectRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory, "mugimenduak.txt");
        connectionString = new MySqlConnectionStringBuilder
        {
            Server = "localhost",
            Port = 3306,
            Database = "zinema_kudeaketa",
            UserID = "root",
            Password = "1MG2024",
            AllowUserVariables = true
        }.ConnectionString;
    }

    public IReadOnlyList<Pelikula> GetActiveMovies() => GetMovies("WHERE p.ezabatuta = 0");

    public IReadOnlyList<Pelikula> GetAllMovies() => GetMovies("");

    public void CreateReservation(int movieId, string reservationName, int seatCount)
    {
        if (seatCount < 1 || seatCount > 5)
        {
            throw new InvalidOperationException("Erreserba bakoitzean 1 eta 5 eserleku artean gorde daitezke.");
        }

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var movie = GetMovie(connection, transaction, movieId);
        if (movie == null || movie.Ezabatuta)
        {
            throw new InvalidOperationException("Pelikula ez dago erabilgarri.");
        }

        if (movie.EserlekuLibreak < seatCount)
        {
            throw new InvalidOperationException($"Ez dago nahikoa eserleku libre. Libre: {movie.EserlekuLibreak}.");
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO erreserbak (pelikula_id, izena, eserleku_kopurua, sortze_data)
            VALUES (@pelikula_id, @izena, @eserleku_kopurua, NOW());
            """;
        command.Parameters.AddWithValue("@pelikula_id", movieId);
        command.Parameters.AddWithValue("@izena", reservationName);
        command.Parameters.AddWithValue("@eserleku_kopurua", seatCount);
        command.ExecuteNonQuery();

        transaction.Commit();
        WriteLog($"ERRESERBA SORTU | pelikula='{movie.Izena}' | eserlekuak={seatCount} | erabiltzailea='{reservationName}'");
    }

    public void CreateMovie(string name, int seats)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO pelikulak (izena, eserleku_kopurua, ezabatuta, sortze_data, aldaketa_data)
            VALUES (@izena, @eserleku_kopurua, 0, NOW(), NOW());
            """;
        command.Parameters.AddWithValue("@izena", name);
        command.Parameters.AddWithValue("@eserleku_kopurua", seats);
        command.ExecuteNonQuery();

        WriteLog($"PELIKULA SORTU | pelikula='{name}' | eserlekuak={seats}");
    }

    public void UpdateMovie(int id, string name, int seats, bool deleted)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE pelikulak
            SET izena = @izena,
                eserleku_kopurua = @eserleku_kopurua,
                ezabatuta = @ezabatuta,
                aldaketa_data = NOW()
            WHERE id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@izena", name);
        command.Parameters.AddWithValue("@eserleku_kopurua", seats);
        command.Parameters.AddWithValue("@ezabatuta", deleted ? 1 : 0);
        command.ExecuteNonQuery();

        WriteLog($"PELIKULA ALDATU | id={id} | pelikula='{name}' | eserlekuak={seats} | ezabatuta={deleted}");
    }

    public void SoftDeleteMovie(int id)
    {
        using var connection = OpenConnection();
        var movie = GetMovie(connection, null, id);

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE pelikulak
            SET ezabatuta = 1,
                aldaketa_data = NOW()
            WHERE id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();

        WriteLog($"PELIKULA SOFT DELETE | id={id} | pelikula='{movie?.Izena ?? "ezezaguna"}'");
    }

    public void RestoreMovie(int id)
    {
        using var connection = OpenConnection();
        var movie = GetMovie(connection, null, id);

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE pelikulak
            SET ezabatuta = 0,
                aldaketa_data = NOW()
            WHERE id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();

        WriteLog($"PELIKULA SOFT DELETE DESAKTIBATU | id={id} | pelikula='{movie?.Izena ?? "ezezaguna"}'");
    }

    public void HardDeleteMovie(int id)
    {
        using var connection = OpenConnection();
        var movie = GetMovie(connection, null, id);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM pelikulak WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();

        WriteLog($"PELIKULA HARD DELETE | id={id} | pelikula='{movie?.Izena ?? "ezezaguna"}'");
    }

    private IReadOnlyList<Pelikula> GetMovies(string where)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT p.id, p.izena, p.eserleku_kopurua, p.ezabatuta,
                   COALESCE(SUM(e.eserleku_kopurua), 0) AS erreserbatuta
            FROM pelikulak p
            LEFT JOIN erreserbak e ON e.pelikula_id = p.id
            {where}
            GROUP BY p.id, p.izena, p.eserleku_kopurua, p.ezabatuta
            ORDER BY p.ezabatuta ASC, p.izena ASC;
            """;

        var movies = new List<Pelikula>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            movies.Add(ReadMovie(reader));
        }

        return movies;
    }

    private Pelikula? GetMovie(MySqlConnection connection, MySqlTransaction? transaction, int id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT p.id, p.izena, p.eserleku_kopurua, p.ezabatuta,
                   COALESCE(SUM(e.eserleku_kopurua), 0) AS erreserbatuta
            FROM pelikulak p
            LEFT JOIN erreserbak e ON e.pelikula_id = p.id
            WHERE p.id = @id
            GROUP BY p.id, p.izena, p.eserleku_kopurua, p.ezabatuta;
            """;
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadMovie(reader) : null;
    }

    private static Pelikula ReadMovie(MySqlDataReader reader)
    {
        return new Pelikula
        {
            Id = reader.GetInt32(0),
            Izena = reader.GetString(1),
            EserlekuKopurua = reader.GetInt32(2),
            Ezabatuta = reader.GetInt32(3) == 1,
            Erreserbatuta = reader.GetInt32(4)
        };
    }

    private MySqlConnection OpenConnection()
    {
        var connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    private void WriteLog(string message)
    {
        File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}");
    }

    private static string? FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        for (var i = 0; i < 10 && directory != null; i++)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ZinemaKudeaketa.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

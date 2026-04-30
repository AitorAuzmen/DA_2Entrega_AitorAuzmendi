using MySqlConnector;

namespace ZinemaKudeaketa;

public sealed class ZinemaRepository
{
    private readonly string dataFolder;
    private readonly string connectionFile;
    private readonly string logFile;
    private readonly string connectionString;

    public ZinemaRepository()
    {
        dataFolder = Path.Combine(AppContext.BaseDirectory, "datuak");
        Directory.CreateDirectory(dataFolder);

        connectionFile = Path.Combine(dataFolder, "mysql_connection.txt");
        logFile = Path.Combine(FindProjectRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory, "mugimenduak.txt");
        connectionString = LoadConnectionString();

        EnsureDatabaseAndTables();
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

    private void EnsureDatabaseAndTables()
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var database = string.IsNullOrWhiteSpace(builder.Database) ? "zinema_kudeaketa" : builder.Database;
        builder.Database = "";

        using (var connection = new MySqlConnection(builder.ConnectionString))
        {
            connection.Open();
            using var createDb = connection.CreateCommand();
            createDb.CommandText = $"CREATE DATABASE IF NOT EXISTS `{EscapeIdentifier(database)}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            createDb.ExecuteNonQuery();
        }

        using var dbConnection = OpenConnection();
        Execute(dbConnection, """
            CREATE TABLE IF NOT EXISTS pelikulak (
                id INT NOT NULL AUTO_INCREMENT,
                izena VARCHAR(150) NOT NULL,
                eserleku_kopurua INT NOT NULL,
                ezabatuta TINYINT(1) NOT NULL DEFAULT 0,
                sortze_data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                aldaketa_data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                CONSTRAINT chk_pelikulak_eserlekuak CHECK (eserleku_kopurua > 0),
                CONSTRAINT chk_pelikulak_ezabatuta CHECK (ezabatuta IN (0, 1))
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);
        Execute(dbConnection, """
            CREATE TABLE IF NOT EXISTS erreserbak (
                id INT NOT NULL AUTO_INCREMENT,
                pelikula_id INT NOT NULL,
                izena VARCHAR(150) NOT NULL,
                eserleku_kopurua INT NOT NULL,
                sortze_data DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                PRIMARY KEY (id),
                INDEX idx_erreserbak_pelikula_id (pelikula_id),
                CONSTRAINT chk_erreserbak_eserlekuak CHECK (eserleku_kopurua BETWEEN 1 AND 5),
                CONSTRAINT fk_erreserbak_pelikulak
                    FOREIGN KEY (pelikula_id) REFERENCES pelikulak(id)
                    ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);

        using var count = dbConnection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM pelikulak;";
        if (Convert.ToInt32(count.ExecuteScalar()) > 0)
        {
            return;
        }

        Execute(dbConnection, """
            INSERT INTO pelikulak (izena, eserleku_kopurua) VALUES
            ('Dune: Part Two', 120),
            ('Inside Out 2', 90),
            ('Oppenheimer', 100),
            ('Robot Dreams', 70);
            """);
        WriteLog("HASIERAKO DATUAK SORTU | 4 pelikula gehitu dira");
    }

    private static void Execute(MySqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private MySqlConnection OpenConnection()
    {
        var connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    private string LoadConnectionString()
    {
        if (!File.Exists(connectionFile))
        {
            File.WriteAllText(
                connectionFile,
                "Server=127.0.0.1;Port=3306;Database=zinema_kudeaketa;User ID=root;Password=;Allow User Variables=True;");
        }

        return File.ReadAllText(connectionFile).Trim();
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

    private static string EscapeIdentifier(string value) => value.Replace("`", "``");
}

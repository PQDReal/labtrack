using System.Globalization;
using Microsoft.Data.Sqlite;

namespace LabTrack.Core;

public record Sample(long Id, string Code, string Kind, string Location, string Status, string Notes);

// A deliberately small repository: each method opens and disposes its own connection.
public sealed class SampleStore
{
    public static readonly string[] Statuses = ["Received", "In progress", "Completed"];
    private readonly string connectionString;

    public SampleStore(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Samples (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Code TEXT NOT NULL UNIQUE COLLATE NOCASE,
                Kind TEXT NOT NULL, Location TEXT NOT NULL,
                Status TEXT NOT NULL CHECK(Status IN ('Received','In progress','Completed')),
                Notes TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    public List<Sample> List(string search = "", string status = "All")
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Code, Kind, Location, Status, Notes FROM Samples
            WHERE ($status = 'All' OR Status = $status)
              AND (instr(lower(Code), lower($search)) > 0
                OR instr(lower(Kind), lower($search)) > 0
                OR instr(lower(Location), lower($search)) > 0)
            ORDER BY Id DESC;
            """;
        command.Parameters.AddWithValue("$search", search.Trim());
        command.Parameters.AddWithValue("$status", status);
        using var reader = command.ExecuteReader();
        var result = new List<Sample>();
        while (reader.Read()) result.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        return result;
    }

    public long Save(Sample sample)
    {
        sample = sample with { Code = sample.Code.Trim(), Kind = sample.Kind.Trim(), Location = sample.Location.Trim(), Notes = sample.Notes.Trim() };
        if (string.IsNullOrWhiteSpace(sample.Code) || sample.Code.Length > 40) throw new ArgumentException("Sample code is required (maximum 40 characters).");
        if (string.IsNullOrWhiteSpace(sample.Kind) || sample.Kind.Length > 60) throw new ArgumentException("Sample type is required (maximum 60 characters).");
        if (string.IsNullOrWhiteSpace(sample.Location) || sample.Location.Length > 60) throw new ArgumentException("Storage location is required (maximum 60 characters).");
        if (!Statuses.Contains(sample.Status)) throw new ArgumentException("Choose a valid status.");
        if (sample.Notes.Length > 1000) throw new ArgumentException("Notes must be at most 1,000 characters.");
        if (sample.Id < 0) throw new ArgumentException("Invalid sample ID.");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sample.Id == 0
            ? "INSERT INTO Samples(Code,Kind,Location,Status,Notes) VALUES($code,$kind,$location,$status,$notes) RETURNING Id;"
            : "UPDATE Samples SET Code=$code,Kind=$kind,Location=$location,Status=$status,Notes=$notes WHERE Id=$id RETURNING Id;";
        command.Parameters.AddWithValue("$id", sample.Id);
        command.Parameters.AddWithValue("$code", sample.Code);
        command.Parameters.AddWithValue("$kind", sample.Kind);
        command.Parameters.AddWithValue("$location", sample.Location);
        command.Parameters.AddWithValue("$status", sample.Status);
        command.Parameters.AddWithValue("$notes", sample.Notes);
        try
        {
            var id = command.ExecuteScalar();
            return id is null ? throw new InvalidOperationException("This sample no longer exists. Refresh the list.") : Convert.ToInt64(id, CultureInfo.InvariantCulture);
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == 2067)
        {
            throw new ArgumentException("That sample code already exists. Choose a unique code.", exception);
        }
    }

    public void Delete(long id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Samples WHERE Id=$id";
        command.Parameters.AddWithValue("$id", id);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("This sample no longer exists.");
    }

    public static string Csv(IEnumerable<Sample> samples)
    {
        // Quote every cell; neutralize spreadsheet formulas in user-controlled fields.
        static string Cell(string value)
        {
            if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        var lines = new List<string> { "Id,Code,Type,Location,Status,Notes" };
        lines.AddRange(samples.Select(s => string.Join(",", new[] { s.Id.ToString(CultureInfo.InvariantCulture), s.Code, s.Kind, s.Location, s.Status, s.Notes }.Select(Cell))));
        return string.Join("\r\n", lines) + "\r\n";
    }
}

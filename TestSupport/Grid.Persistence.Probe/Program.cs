using Grid.Persistence;

// Test-only subprocess. The parent kills this exact process after the uncommitted pages spill.
if (args.Length != 1) return 2;
using var connection = new SqliteGridDatabase(args[0]).OpenExisting();
using var command = connection.CreateCommand();
command.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA cache_size=10; PRAGMA cache_spill=ON;";
command.ExecuteNonQuery();
using var transaction = connection.BeginTransaction();
command.Transaction = transaction;
command.CommandText = """
    DELETE FROM Programs;
    UPDATE SimulationStates SET TickCount=TickCount+1, ElapsedTicks=ElapsedTicks+TickDurationTicks;
    CREATE TABLE InterruptedWrite(Data BLOB);
    INSERT INTO InterruptedWrite VALUES (zeroblob(2097152));
    """;
command.ExecuteNonQuery();
Console.WriteLine("UNCOMMITTED_WRITE_READY");
Console.Out.Flush();
// Never commit: terminating the child must leave SQLite responsible for journal recovery.
await Task.Delay(Timeout.Infinite);
return 0;

using TraceQ.Cli.Commands;

namespace TraceQ.Cli.Tests;

public class ImportCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOut;
    private readonly string _tempDbPath;

    public ImportCommandTests()
    {
        _originalOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"traceq-test-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleOutput.Dispose();

        // Clean up temp database files
        foreach (var file in new[] { _tempDbPath, _tempDbPath + "-shm", _tempDbPath + "-wal" })
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
        }
    }

    private string GetOutput() => _consoleOutput.ToString();

    private static string GetTestDataPath(string fileName)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "tests", "TestData")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        if (dir == null)
            throw new DirectoryNotFoundException("Could not find tests/TestData directory");

        return Path.Combine(dir, "tests", "TestData", "cli", fileName);
    }

    [Fact]
    public async Task SuccessfulImport_ReturnsZeroAndShowsCounts()
    {
        var path = GetTestDataPath("valid_minimal.csv");
        var exitCode = await ImportCommand.ExecuteAsync(new FileInfo(path), _tempDbPath);

        exitCode.Should().Be(0);
        var output = GetOutput();
        output.Should().Contain("Inserted:");
        output.Should().Contain("Import complete.");
    }

    [Fact]
    public async Task NonexistentFile_ReturnsExitCodeOne()
    {
        var exitCode = await ImportCommand.ExecuteAsync(new FileInfo("nonexistent.csv"), _tempDbPath);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("File not found");
    }

    [Fact]
    public async Task WrongExtension_ReturnsExitCodeOne()
    {
        var path = GetTestDataPath("not_a_csv.txt");
        var exitCode = await ImportCommand.ExecuteAsync(new FileInfo(path), _tempDbPath);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("File must have a .csv extension");
    }

    [Fact]
    public async Task ReImport_ShowsSkippedCounts()
    {
        var path = GetTestDataPath("valid_full.csv");

        // First import
        await ImportCommand.ExecuteAsync(new FileInfo(path), _tempDbPath);

        // Reset output
        _consoleOutput.GetStringBuilder().Clear();

        // Second import — same data, should be skipped
        var exitCode = await ImportCommand.ExecuteAsync(new FileInfo(path), _tempDbPath);

        exitCode.Should().Be(0);
        GetOutput().Should().Contain("Skipped:");
    }

    [Fact]
    public async Task CustomDbPath_CreatesDatabaseAtSpecifiedLocation()
    {
        var path = GetTestDataPath("valid_minimal.csv");
        var exitCode = await ImportCommand.ExecuteAsync(new FileInfo(path), _tempDbPath);

        exitCode.Should().Be(0);
        File.Exists(_tempDbPath).Should().BeTrue();
    }

    [Fact]
    public async Task EmptyFile_ReturnsExitCodeOne()
    {
        var path = GetTestDataPath("empty.csv");
        var exitCode = await ImportCommand.ExecuteAsync(new FileInfo(path), _tempDbPath);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("File is empty");
    }
}

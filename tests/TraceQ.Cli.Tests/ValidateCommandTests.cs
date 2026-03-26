using TraceQ.Cli;
using TraceQ.Cli.Commands;

namespace TraceQ.Cli.Tests;

public class ValidateCommandTests : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOut;

    public ValidateCommandTests()
    {
        _services = Program.ConfigureServices();
        _originalOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOut);
        _consoleOutput.Dispose();
    }

    private string GetOutput() => _consoleOutput.ToString();

    private static string GetTestDataPath(string fileName)
    {
        // Walk up from bin output to find tests/TestData
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
    public async Task ValidFile_ReturnsZeroExitCode()
    {
        var path = GetTestDataPath("valid_minimal.csv");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(0);
        GetOutput().Should().Contain("PASS");
        GetOutput().Should().Contain("file can be imported");
    }

    [Fact]
    public async Task ValidFullFile_ReturnsZeroExitCode()
    {
        var path = GetTestDataPath("valid_full.csv");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(0);
        GetOutput().Should().Contain("PASS");
        GetOutput().Should().Contain("Valid rows:");
    }

    [Fact]
    public async Task NonexistentFile_ReturnsExitCodeOne()
    {
        var exitCode = await ValidateCommand.ExecuteAsync(
            new FileInfo("nonexistent_file.csv"), _services);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("File not found");
    }

    [Fact]
    public async Task WrongExtension_ReturnsExitCodeOne()
    {
        var path = GetTestDataPath("not_a_csv.txt");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("File must have a .csv extension");
    }

    [Fact]
    public async Task EmptyFile_ReturnsExitCodeOne()
    {
        var path = GetTestDataPath("empty.csv");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("File is empty");
    }

    [Fact]
    public async Task MissingRequiredColumn_ReturnsExitCodeOne()
    {
        var path = GetTestDataPath("missing_number_column.csv");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(1);
        GetOutput().Should().Contain("Required column 'Number' is missing");
    }

    [Fact]
    public async Task RowErrors_ReportsErrorsAndReturnsExitCodeOne()
    {
        var path = GetTestDataPath("row_errors.csv");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(1);
        var output = GetOutput();
        output.Should().Contain("Error rows:");
        output.Should().Contain("FAIL");
    }

    [Fact]
    public async Task UnknownColumns_ReportsWarningsButPasses()
    {
        var path = GetTestDataPath("unknown_columns.csv");
        var exitCode = await ValidateCommand.ExecuteAsync(new FileInfo(path), _services);

        exitCode.Should().Be(0);
        var output = GetOutput();
        output.Should().Contain("WARN");
        output.Should().Contain("Custom Field 1");
        output.Should().Contain("file can be imported");
    }
}

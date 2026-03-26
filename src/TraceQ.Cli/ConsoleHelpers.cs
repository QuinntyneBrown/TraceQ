namespace TraceQ.Cli;

internal static class ConsoleHelpers
{
    public static void WriteHeader(string message)
    {
        Console.WriteLine();
        SetColor(ConsoleColor.Cyan);
        Console.WriteLine($"  {message}");
        ResetColor();
        Console.WriteLine(new string('─', Math.Min(60, message.Length + 4)));
        Console.WriteLine();
    }

    public static void WriteSectionHeader(string title)
    {
        SetColor(ConsoleColor.White);
        Console.WriteLine($"  [{title}]");
        ResetColor();
    }

    public static void WritePass(string message)
    {
        SetColor(ConsoleColor.Green);
        Console.Write("    PASS  ");
        ResetColor();
        Console.WriteLine(message);
    }

    public static void WriteError(string message)
    {
        SetColor(ConsoleColor.Red);
        Console.Write("    FAIL  ");
        ResetColor();
        Console.WriteLine(message);
    }

    public static void WriteWarning(string message)
    {
        SetColor(ConsoleColor.Yellow);
        Console.Write("    WARN  ");
        ResetColor();
        Console.WriteLine(message);
    }

    public static void WriteInfo(string message)
    {
        SetColor(ConsoleColor.Gray);
        Console.Write("    INFO  ");
        ResetColor();
        Console.WriteLine(message);
    }

    public static void WriteVerdict(bool passed, int errorCount)
    {
        Console.WriteLine();
        if (passed)
        {
            SetColor(ConsoleColor.Green);
            Console.WriteLine("  Result: PASS — file can be imported");
        }
        else
        {
            SetColor(ConsoleColor.Red);
            Console.WriteLine($"  Result: FAIL — {errorCount} error(s) found");
        }
        ResetColor();
        Console.WriteLine();
    }

    public static void SetColor(ConsoleColor color)
    {
        try { Console.ForegroundColor = color; } catch { /* redirected output */ }
    }

    public static void ResetColor()
    {
        try { Console.ResetColor(); } catch { /* redirected output */ }
    }
}

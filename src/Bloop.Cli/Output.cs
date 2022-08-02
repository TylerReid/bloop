
namespace Bloop.Cli;

public static class Output
{
    public static bool WriteColors { get; set; } = !Console.IsOutputRedirected;

    public static void Write(string s, ConsoleColor color)
    {
        if (WriteColors)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ForegroundColor = oldColor;
        }
        else
        {
            Console.Write(s);
        }
    }

    public static void WriteLine(string s, ConsoleColor color)
    {
        Write(s, color);
        WriteLine();
    }

    public static void Write(string s)
    {
        // todo implement an in string color code format
        Console.Write(s);
    }

    public static void WriteLine(string s)
    {
        Write(s);
        WriteLine();
    }

    public static void WriteError(string s) => WriteLine(s, ConsoleColor.Red);

    public static void Write(object? o) => Console.Write(o);

    public static void WriteLine(object? o)
    {
        Write(o);
        WriteLine();
    }

    public static void WriteLine() => Console.WriteLine();
}
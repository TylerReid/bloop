using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;

namespace Bloop.Avalonia.Ui.Resources;

internal static class HighlightLoader
{
    private static ConcurrentDictionary<string, IHighlightingDefinition> Definitions = new();

    public static IHighlightingDefinition GetBloopDefinition(this HighlightingManager manager, string name)
    {
        if (Definitions.TryGetValue(name, out var definition))
        {
            return definition;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var path = $"{assembly.GetName().Name}.Resources.{name}.xshd";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        using var reader = XmlReader.Create(stream!);
        definition = HighlightingLoader.Load(reader, manager);
        Definitions.TryAdd(name, definition);
        return definition;
    }
}

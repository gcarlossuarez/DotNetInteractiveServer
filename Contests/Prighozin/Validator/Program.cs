using System.Globalization;

if (args.Length < 3)
{
    Console.Error.WriteLine("Uso: Validator <DataSet> <Expected> <Actual> [Source]");
    return 2;
}

var dataDir = args[0];
var expectedDir = args[1];
var actualFile = args[2];
if (!Directory.Exists(dataDir) || !Directory.Exists(expectedDir) || !File.Exists(actualFile))
{
    Console.Error.WriteLine("No se encontraron DataSet, Expected o Actual.");
    return 2;
}

var inputs = Directory.GetFiles(dataDir, "dato*.txt")
    .OrderBy(Path.GetFileName, StringComparer.Ordinal)
    .ToArray();
var actual = File.ReadAllLines(actualFile)
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .Select(x => x.Trim())
    .ToArray();
var cursor = 0;

foreach (var inputFile in inputs)
{
    var expectedFile = Path.Combine(expectedDir, "Output_" + Path.GetFileName(inputFile));
    if (!File.Exists(expectedFile))
    {
        Console.WriteLine("Falta esperado: " + Path.GetFileName(expectedFile));
        return 2;
    }

    var expectedCount = File.ReadAllLines(expectedFile).Count(x => !string.IsNullOrWhiteSpace(x));
    if (cursor + expectedCount > actual.Length)
    {
        Console.WriteLine("Salida incompleta para " + Path.GetFileName(inputFile));
        return 1;
    }

    var block = actual[cursor..(cursor + expectedCount)];
    cursor += expectedCount;
    var lines = File.ReadAllLines(inputFile).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    if (lines.Length == 0) continue;

    var header = lines[0].Split(';');
    if (header.Length != 3 || !int.TryParse(header[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity))
    {
        Console.WriteLine("Cabecera inválida en " + Path.GetFileName(inputFile));
        return 2;
    }

    var classifier = header[0].Trim();
    var ratio = classifier == "Rossgvardia" ? 8 : 30;
    var items = new List<(string Name, int Estimated, int Neutralized)>();
    for (var i = 1; i < lines.Length; i++)
    {
        var parts = lines[i].Split(';');
        if (parts.Length != 3 || !int.TryParse(parts[1].Trim(), out var estimated) || !int.TryParse(parts[2].Trim(), out var neutralized))
        {
            Console.WriteLine("Entrada inválida en " + Path.GetFileName(inputFile));
            return 2;
        }
        items.Add((parts[0].Trim(), estimated, neutralized));
    }

    var dp = new int[items.Count + 1, capacity + 1];
    for (var i = 1; i <= items.Count; i++)
    {
        var cost = items[i - 1].Estimated * ratio;
        for (var w = 0; w <= capacity; w++)
            dp[i, w] = cost <= w ? Math.Max(dp[i - 1, w], items[i - 1].Neutralized + dp[i - 1, w - cost]) : dp[i - 1, w];
    }

    var first = block[0].Split(';');
    if (first.Length != 3 || !int.TryParse(first[1].Trim(), out var reportedCapacity) || !int.TryParse(first[2].Trim(), out var reported))
    {
        Console.WriteLine("Cabecera de salida inválida en " + Path.GetFileName(inputFile));
        return 1;
    }
    if (reportedCapacity != capacity || reported != dp[items.Count, capacity])
    {
        Console.WriteLine($"Valor óptimo incorrecto en {Path.GetFileName(inputFile)}: se esperaba {dp[items.Count, capacity]}, llegó {reported}");
        return 1;
    }

    var used = new bool[items.Count];
    var total = 0;
    var spent = 0;
    foreach (var line in block.Skip(1))
    {
        var parts = line.Split(';');
        if (parts.Length != 3 || !int.TryParse(parts[1].Trim(), out var estimated) || !int.TryParse(parts[2].Trim(), out var neutralized))
        {
            Console.WriteLine("Formato de salida inválido en " + Path.GetFileName(inputFile));
            return 1;
        }
        var outputName = parts[0].Trim().Replace(((char)0x2019).ToString(), "'").Replace(((char)0x2018).ToString(), "'");
        var match = -1;
        for (var i = 0; i < items.Count; i++)
            if (!used[i] && items[i].Name.Replace(((char)0x2019).ToString(), "'").Replace(((char)0x2018).ToString(), "'") == outputName && items[i].Estimated == estimated && items[i].Neutralized == neutralized) { match = i; break; }
        if (match < 0) { Console.WriteLine("Elemento no corresponde a la entrada: " + parts[0]); return 1; }
        used[match] = true;
        total += neutralized;
        spent += estimated * ratio;
    }
    if (total != reported || spent > capacity)
    {
        Console.WriteLine("La selección no es factible en " + Path.GetFileName(inputFile));
        return 1;
    }
}

if (cursor != actual.Length)
{
    Console.WriteLine("Hay líneas adicionales después del último caso");
    return 1;
}
Console.WriteLine("OK");
return 0;


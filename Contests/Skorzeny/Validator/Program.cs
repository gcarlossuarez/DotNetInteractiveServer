using System.Globalization;


Console.OutputEncoding = Encoding.UTF8; // Para no tener problemas con los caracteres especiales

if (args.Length < 3) { Console.Error.WriteLine("Uso: Validator <DataSet> <Expected> <Actual> [Source]"); return 2; }
var dataDir = args[0]; var expectedDir = args[1]; var actualFile = args[2];
var inputs = Directory.GetFiles(dataDir, "dato*.txt").OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray();
var actual = File.ReadAllLines(actualFile, Encoding.UTF8).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
var cursor = 0;
foreach (var inputFile in inputs)
{
    var expectedFile = Path.Combine(expectedDir, "Output_" + Path.GetFileName(inputFile));
    if (!File.Exists(expectedFile)) { Console.WriteLine("Falta esperado: " + Path.GetFileName(expectedFile)); return 2; }
    var expectedCount = File.ReadAllLines(expectedFile, Encoding.UTF8).Count(x => !string.IsNullOrWhiteSpace(x));
    //if (cursor + expectedCount > actual.Length) { Console.WriteLine("Salida incompleta para " + Path.GetFileName(inputFile)); return 1; }
    //ARREGLAR EL CURSOR.ESTÁ LEYENDO MAL SI LA SALIDA ESPERADA NO ES LA MISMA
    //var block = actual[cursor..(cursor + expectedCount)]; cursor += expectedCount;

    var block = GetBlock(actual, ref cursor);

    var inputLines = File.ReadAllLines(inputFile, Encoding.UTF8).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    if (!int.TryParse(inputLines[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var capacity)) { Console.WriteLine("Capacidad inválida"); return 2; }
    var itemsList = new List<(string Place, int Value, int Weight)>();
    for (var i = 1; i < inputLines.Length; i++)
    {
        var parts = inputLines[i].Split(';');
        if (parts.Length != 3 || !int.TryParse(parts[1].Trim(), out var value) || !int.TryParse(parts[2].Trim(), out var weight)) { Console.WriteLine("Entrada inválida"); return 2; }
        itemsList.Add((parts[0].Trim(), value, weight));
    }
    if (!int.TryParse(block[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var reported)) { Console.WriteLine("Valor máximo inválido"); return 1; }
    // Resuelve el Problema de la Mochila y compara el resultado obtenido por el validador, con el resultado obtenido
    // por el estudiante
    var n = itemsList.Count; var dp = new int[n + 1, capacity + 1];
    for (var i = 1; i <= n; i++) for (var w = 0; w <= capacity; w++) dp[i, w] = itemsList[i - 1].Weight <= w ? Math.Max(dp[i - 1, w], itemsList[i - 1].Value + dp[i - 1, w - itemsList[i - 1].Weight]) : dp[i - 1, w];
    if (reported != dp[n, capacity]) { Console.WriteLine($"Valor máximo incorrecto en {Path.GetFileName(inputFile)}: se esperaba {dp[n, capacity]}, llegó {reported}"); return 1; }
    var used = new bool[n]; var totalValue = 0; var totalWeight = 0;
    foreach (var line in block.Skip(1))
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !int.TryParse(parts[^2], out var informants) || !int.TryParse(parts[^1], out var soldiers)) { Console.WriteLine("Formato inválido en " + Path.GetFileName(inputFile)); return 1; }
        var place = string.Join(" ", parts[..^2]);  //  Toma todos los elementos desde el primero hasta llegar
                                                    //  justo antes de los últimos dos. Por ejemplo, si se tienen
                                                    //  5 elementos, se queda con los primeros 3.        var match = -1;
                                                    // Cuidado con los caracteres epseciales (UTF-8 y similar)
        for (var i = 0; i < n; i++) if (!used[i] && itemsList[i].Place == place && itemsList[i].Value == informants && itemsList[i].Weight == soldiers) { match = i; break; }
        //if (match < 0) { Console.WriteLine("Ciudad o datos no corresponden a la entrada: " + place); return 1; }
        used[match] = true; totalValue += informants; totalWeight += soldiers;
    }
    if (totalValue != reported || totalWeight > capacity) { Console.WriteLine("La selección no es factible en " + Path.GetFileName(inputFile)); return 1; }
}
//if (cursor != actual.Length) { Console.WriteLine("Hay líneas adicionales después del último caso"); return 1; }
Console.WriteLine("OK"); return 0;


static string[] GetBlock(string[] actual, ref int cursor)
{
    List<string> block = new List<string>();
    int initialCursor = cursor;
    block.Add(actual[cursor++]);
    while (cursor < actual.Length)
    {
        if (actual[cursor].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 1)
        {
            return block.ToArray();
        }
        block.Add(actual[cursor++]);
    }
    return block.ToArray();
}



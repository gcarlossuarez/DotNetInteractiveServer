// File: Validator.cs WarehouseGroupingSolver
// Uso: dotnet run -- <input> <expected> <actual>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Validator
{
    static void Main(string[] args)
    {
        try
        {
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: argumentos insuficientes (uso: Validator <input> <expected> <actual>)");
                Environment.Exit(1);
            }

            string inputPath = args[0];
            string expectedPath = args[1];
            string actualPath = args[2];

            if (!File.Exists(inputPath)) Error("no existe el archivo input");
            if (!File.Exists(expectedPath)) Error("no existe el archivo esperado");
            if (!File.Exists(actualPath)) Error("no existe el archivo generado por el estudiante");

            // ==== LECTURA ====
            string expectedText = File.ReadAllText(expectedPath).Trim();
            string actualText = File.ReadAllText(actualPath).Trim();

            string[] expectedLines = NormalizeLines(File.ReadAllLines(expectedPath));
            string[] actualLines = NormalizeLines(File.ReadAllLines(actualPath));

            if (actualLines.Length == 0)
			{
				Error("salida incompleta");
			}

            // ==== Comparación de primera línea (True/False) ====
            string expectedTF = expectedLines[0].Trim();
            string actualTF = actualLines[0].Trim();

            if (expectedTF != actualTF)
                Error("primera línea (True/False) incorrecta");

            // ==== Si la respuesta correcta es FALSE ====
            if (expectedTF == "False")
            {
                if (actualLines.Length > 1)
                    Error("si la respuesta es False, no deben existir más líneas");

                Console.WriteLine("OK");
                return;
            }

            // ==== Validación de líneas en blanco ====
            int blankCount = actualLines.Count(l => string.IsNullOrWhiteSpace(l));
            if (blankCount > 1)
			{
				Error("más de una línea en blanco en la salida");
			}

            // ==== LEER INPUT ====
            var inputLines = NormalizeLines(File.ReadAllLines(inputPath));
            var header = inputLines[0].Split();
            int N = int.Parse(header[0]);
            int M = int.Parse(header[1]);

            HashSet<int> validNodes = new HashSet<int>(Enumerable.Range(1, N));

            // ==== LEER ARISTAS DEL INPUT ====
            List<(int u, int v)> edges = new List<(int u, int v)>();

            for (int i = 1; i <= M; i++)
            {
                var parts = inputLines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int u = int.Parse(parts[0]);
                int v = int.Parse(parts[1]);
                edges.Add((u, v));
            }

            // ==== LEER LOS GRUPOS DEL ESTUDIANTE ====
            List<int> groupA = new List<int>();
            List<int> groupB = new List<int>();

            // Saltamos la primera línea (True)
            bool firstGroupFound = false;

            foreach (var line in actualLines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    firstGroupFound = true;
                    continue;
                }

                var nums = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var n in nums)
                {
                    if (!int.TryParse(n, out int value))
					{
						Error($"nodo inválido: {n}");
					}

                    if (!validNodes.Contains(value))
					{
						Error($"nodo {value} no existe en la entrada");
					}

                    if (!firstGroupFound)
                    {
                        if (groupA.Contains(value))
						{
							Error($"nodo repetido en grupo A: {value}");
						}
                        groupA.Add(value);
                    }
                    else
                    {
                        if (groupB.Contains(value))
						{
							Error($"nodo repetido en grupo B: {value}");
						}
                        groupB.Add(value);
                    }
                }
            }

            // ==== Validar que los grupos no se solapen ====
            foreach (int x in groupA)
			{
				if (groupB.Contains(x))
				{
                    Error($"nodo {x} aparece en ambos grupos");
				}
			}

            // ==== Validar bipartición ====
            HashSet<int> setA = new HashSet<int>(groupA);
            HashSet<int> setB = new HashSet<int>(groupB);

            foreach (var (u, v) in edges)
            {
                bool uA = setA.Contains(u);
                bool vA = setA.Contains(v);

                bool uB = setB.Contains(u);
                bool vB = setB.Contains(v);

                // Ambos en A → error
                if (uA && vA)
				{
					Error($"arista interna inválida: {u} - {v} dentro del grupo A");
				}

                // Ambos en B → error
                if (uB && vB)
				{
					Error($"arista interna inválida: {u} - {v} dentro del grupo B");
				}
            }

            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            Environment.Exit(1);
        }
    }

    static void Error(string msg)
    {
        Console.WriteLine("Resultado incorrecto: " + msg);
        Environment.Exit(1);
    }
	
	static string[] NormalizeLines(string[] lines)
	{
		int i = lines.Length - 1;

		while (i >= 0 && string.IsNullOrWhiteSpace(lines[i]))
			i--;

		return lines.Take(i + 1).ToArray();
	}

}

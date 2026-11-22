using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

class Validator
{
    static void Main(string[] args)
    {
        try
		{
			Process(args);
		}
		catch(Exception ex)
		{
			Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
		}
    }
	
	static void Process(string[] args)
	{
		if (args.Length < 3)
        {
            Console.WriteLine("ERROR: argumentos insuficientes (uso: Validator <input> <expected> <actual>)");
            Environment.Exit(1);
        }

        string inputPath = args[0];
        string expectedPath = args[1];
        string actualPath = args[2];

        // Leer archivos
        var inputLines = File.ReadAllLines(inputPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        var expected = File.ReadAllLines(expectedPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        var output = File.ReadAllLines(actualPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .ToList();

        if (expected.Count == 0)
        {
            Console.WriteLine("ERROR: archivo esperado vacío");
            Environment.Exit(1);
        }
        if (output.Count == 0)
        {
            Console.WriteLine("ERROR: salida del estudiante vacía");
            Environment.Exit(1);
        }

        // 1) Validar que la primera línea coincida
        if (expected[0] != output[0])
        {
            Console.WriteLine($"ERROR: la primera línea no coincide.\nEsperado: {expected[0]}\nObtenido: {output[0]}");
            Environment.Exit(1);
        }

        // 2) Validar que cada línea de la salida exista en el archivo de entrada (desde la segunda línea del input)
        var validLines = new HashSet<string>(inputLines.Skip(1));

        for (int i = 1; i < output.Count; i++)
        {
            if (!validLines.Contains(output[i]))
            {
                Console.WriteLine($"ERROR: la línea '{output[i]}' no existe en el archivo de entrada.");
                Environment.Exit(1);
            }
        }
		
		// 3) Validar la sumatoria
		string unitClassifier = inputLines[0].Trim().Split(";")[0];
		int superiorityRatio = unitClassifier == "Rossgvardia" ? 8 : 30;
		int total = 0;
		int knapsackCapacity = int.Parse(inputLines[0].Trim().Split(";")[2]);
		for (int i = 1; i < output.Count; i++)
		{
			string[] parts = output[i].Split(";");
			int numberOfWagnerMercenaries = int.Parse(parts[1]);
			total += (numberOfWagnerMercenaries * superiorityRatio);
		}
		if(total > knapsackCapacity)
		{
			Console.WriteLine($"ERROR: Capacidad de soldados superada.");
            Environment.Exit(1);
		}
		
		// 4) Validar que esté ordenado
		string previous = string.Empty;
		for (int i = 1; i < output.Count; i++)
		{
			string[] parts = output[i].Split(";");
			//Console.WriteLine($"previous={previous}, parts[0]:{parts[0]}");
			if(previous.CompareTo(parts[0]) > 0)
			{
				Console.WriteLine($"ERROR: La salida no está ordenada por localidad.");
				Environment.Exit(1);
			}
			previous = parts[0];
		}

        Console.WriteLine("OK");
        Environment.Exit(0);
	}
}


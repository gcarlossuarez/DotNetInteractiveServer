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
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void Process(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("ERROR: uso: Validator <input> <expected> <actual>");
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

        if (output.Count < 1)
        {
            Console.WriteLine("ERROR: salida vacía");
            Environment.Exit(1);
        }

        // --- 1) Validar formato de la primera línea ---
        if (!int.TryParse(output[0], out int freedSpace))
        {
            Console.WriteLine("ERROR: la primera línea debe ser un número entero (espacio liberado).");
            Environment.Exit(1);
        }

        // --- 2) Extraer apps desde el input ---
        // Input tiene estructura:
        // X
        // Y
        // N
        // Nombre;Tamaño;Inactividad
        var appLines = inputLines.Skip(3).ToList();
        if (appLines.Count == 0)
        {
            Console.WriteLine("ERROR: archivo de entrada sin datos de apps.");
            Environment.Exit(1);
        }

        var apps = new Dictionary<string, int>(); // Name -> Size
        foreach (var line in appLines)
        {
            var parts = line.Split(';');
            if (parts.Length < 3)
            {
                Console.WriteLine($"ERROR: formato inválido en línea de entrada: '{line}'");
                Environment.Exit(1);
            }

            string name = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out int size))
            {
                Console.WriteLine($"ERROR: tamaño inválido para '{name}'.");
                Environment.Exit(1);
            }

            apps[name] = size;
        }

        // --- 3) Verificar que todas las apps del output existan ---
        if (output.Count > 1)
        {
            var listedApps = output[1].Split(',')
                                      .Select(a => a.Trim())
                                      .Where(a => !string.IsNullOrEmpty(a))
                                      .ToList();

            foreach (var app in listedApps)
            {
                if (!apps.ContainsKey(app))
                {
                    Console.WriteLine($"ERROR: la app '{app}' no existe en el archivo de entrada.");
                    Environment.Exit(1);
                }
            }

            // --- 4) Verificar sumatoria ---
            int totalSize = listedApps.Sum(a => apps[a]);

            if (totalSize != freedSpace)
            {
                Console.WriteLine($"ERROR: suma de tamaños no coincide.\nEsperado: {freedSpace}\nCalculado: {totalSize}");
                Environment.Exit(1);
            }
        }
        else
        {
            // Caso especial: no se eliminó ninguna
            if (freedSpace != 0)
            {
                Console.WriteLine($"ERROR: no se listaron apps eliminadas, pero el espacio liberado es {freedSpace}.");
                Environment.Exit(1);
            }
        }

        Console.WriteLine("OK");
        Environment.Exit(0);
    }
}

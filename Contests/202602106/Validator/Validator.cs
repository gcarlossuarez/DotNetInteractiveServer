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
        List<string> boolValues = new List<string>() {"True", "False"};
        if (!boolValues.Exists(x=> x == output[0]))
        {
            Console.WriteLine("ERROR: la primera línea debe ser un valor booleano (indica si se pudo librearespacio liberado).");
            Environment.Exit(1);
        }

        // --- 2) Validar que la primera línea coincida ---
        if(output[0] != expected[0])
        {
            Console.WriteLine("ERROR: la primera línea no coincide con el valor esperado.");
            Environment.Exit(1);
        }

        // --- 3) Validar que si la respuesta es "False", solamente, pueden haber 2 líneas ---
        if(output[0] == "False" && output.Count != 2)
        {
            Console.WriteLine("ERROR: si el resultado es \"False\"; solamente, se pueden tener 2 líneas en total.");
            Environment.Exit(1);
        }

        // --- 4) Validar que si la respuesta es "False", la segunda línea, debe tener el valor "Ninguna" ---
        if(output[0] == "False" && output[1] != "Ninguna")
        {
            Console.WriteLine("ERROR: si el resultado es \"False\"; la siguiente línea debe tener el valor \"Ninguna\".");
            Environment.Exit(1);
        }

        // --- 5) Todo correcto si la respuesta es "False" y la segunda línea, tiene el valor "Ninguna". Deja de analizar ---
        if(output[0] == "False" && output[1] == "Ninguna")
        {
            Console.WriteLine("OK");
            Environment.Exit(0);
        }

        // --- 2) Extraer apps desde el input ---
        // Input tiene estructura:
        // X
        // Y
        // N
        // T
        // Nombre;Tamaño;Inactividad,Umbral
        var appLines = inputLines.Skip(4).ToList();
        if (appLines.Count == 0)
        {
            Console.WriteLine("ERROR: archivo de entrada sin datos de apps.");
            Environment.Exit(1);
        }

        // --- 6) Validación del archivo de entrada
        if(!int.TryParse(inputLines[1], out int Y))
        {
            Console.WriteLine("ERROR: archivo de entrada sin datos de megas a eliminar.");
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

        // --- 7) Verificar que todas las apps del output existan ---
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

            // --- 8) Verificar apps repetidas ---
            if (listedApps.Distinct().Count() != listedApps.Count)
            {
                Console.WriteLine("ERROR: no se puede seleccionar una app más de una vez.");
                Environment.Exit(1);
            }

            // --- 9) Verificar sumatoria ---
            int totalSize = listedApps.Sum(a => apps[a]);

            if (totalSize != Y)
            {
                Console.WriteLine($"ERROR: suma de tamaños no coincide.\nEsperado: {Y}\nCalculado: {totalSize}");
                Environment.Exit(1);
            }
        }
        else
        {
            // Caso especial: no se eliminó ninguna
            if (Y != 0)
            {
                Console.WriteLine($"ERROR: no se listaron apps eliminadas, pero el espacio liberado es {Y}.");
                Environment.Exit(1);
            }
        }

        Console.WriteLine("OK");
        Environment.Exit(0);
    }
}

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
        bool esValido = output[0] switch
        {
            "S" or "N" => true,
            _ => false // Caso por defecto (cualquier otro valor)
        };
        if (!esValido)
        {
            Console.WriteLine("ERROR: la primera línea debe ser 'S' o 'N'.");
            Environment.Exit(1);
        }

        // --- 2) Extraer valor del input ---
        // Input tiene estructura:
        // N
        // Numero de filas y columnas (matriz cuadrada)
        //Console.WriteLine($"entrada:{inputLines[0]}");
        if(!int.TryParse(inputLines[0], out int n))
        {
            Console.WriteLine("ERROR: archivo de entrada con datos erróneos.");
            Environment.Exit(1);
        }

        if(expected[0] != output[0])
        {
            Console.WriteLine($"ERROR: salida diferente. Se esperaba:{expected[0]} y se obtuvo:{output[0]}");
            Environment.Exit(1);
        }

        if(expected[0] == "S")
        {
            var lines = output.Skip(1).ToArray();   // Se necesita hacer la asiganción, para tener la referencia
                                                    // al objeto que se va a utilizar en el "foreach"
            //Console.WriteLine($"Lines:{lines}");
            int [,] matrix = new int[n, n];
            int ci = 0;
            foreach (var line in lines)
            {
                string[] parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < n)
                {
                    Console.WriteLine($"ERROR: formato inválido en línea de entrada: '{line}'");
                    Environment.Exit(1);
                }
    
                //Console.WriteLine($"line:{line}");
                int cj = 0;
                foreach(var e in parts)
                {
                    //Console.Write(e.PadLeft(4, ' '));
                    if(false == new List<string>{"Q", "_"}.Contains(e))
                    {
                        Console.WriteLine($"ERROR: formato inválido en el dato: '{e}'");
                        Environment.Exit(1);
                    }
                    matrix[ci, cj] = e == "Q" ? 1 : 0;
                    //Console.Write(matrix[i,j]);
                    ++cj;
                }
                //Console.WriteLine();
                ++ci;
            }
            for(int i = 0; i < n; ++i)
            {
                for(int j = 0; j < n; ++j)
                {
                    if(matrix[i, j] == 1)
                    {
                        // Misma columna
                        for(int i1=0; i1 < i; ++i1)
                        {
                            if(matrix[i1, j] == 1)
                            {
                                Console.WriteLine($"Choque de coordenadas en {i1},{j} y {i},{j}");
                                Environment.Exit(1);
                            }
                        }

                        // Misma fila
                        for(int j1=0; j1 < j; ++j1)
                        {
                            if(matrix[i, j1] == 1)
                            {
                                Console.WriteLine($"Choque de coordenadas en {i},{j1} y {i},{j}");
                                Environment.Exit(1);
                            }
                        }

                        // Misma diagonal superior izquierda
                        for(int i1 = i - 1, j1 = j - 1; i1 > 0 && j1 > 0; --i1, --j1)
                        {
                            if(matrix[i1, j1] == 1)
                            {
                                Console.WriteLine($"Choque de coordenadas en {i1},{j1} y {i},{j}");
                                Environment.Exit(1);
                            }
                        }

                        // Misma diagonal superior derecha
                        for(int i1 = i - 1, j1 = j + 1; i1 > 0 && j1 < n; --i1, ++j1)
                        {
                            if(matrix[i1, j1] == 1)
                            {
                                Console.WriteLine($"Choque de coordenadas en {i1},{j1} y {i},{j}");
                                Environment.Exit(1);
                            }
                        }

                        // Misma diagonal inferior derecha
                        for(int i1 = i + 1, j1 = j + 1; i1 < n && j1 < n; ++i1, ++j1)
                        {
                            if(matrix[i1, j1] == 1)
                            {
                                Console.WriteLine($"Choque de coordenadas en {i1},{j1} y {i},{j}");
                                Environment.Exit(1);
                            }
                        }

                        // Misma diagonal inferior izquierda
                        for(int i1 = i + 1, j1 = j - 1; i1 < n && j1 > 0; ++i1, --j1)
                        {
                            if(matrix[i1, j1] == 1)
                            {
                                Console.WriteLine($"Choque de coordenadas en {i1},{j1} y {i},{j}");
                                Environment.Exit(1);
                            }
                        }
                    }
                }
            }
    
            int queensCounter = 0;
            for(int i = 0; i < n; ++i)
            {
                for(int j = 0; j < n; ++j)
                {
                    if(matrix[i, j] == 1) queensCounter++;
                }
            }
            if(n != queensCounter)
            {
                Console.WriteLine($"Conteo de reinas:{queensCounter} es diferente de 'N':{n}.");
                Environment.Exit(1);
            }
        }

        Console.WriteLine("OK");
        Environment.Exit(0);
    }
}

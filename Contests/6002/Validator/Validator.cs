// File: Validator.cs
// Uso: dotnet run -- <input> <expected> <actual>
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
            if (args.Length < 3)
            {
                Console.WriteLine("ERROR: argumentos insuficientes (uso: Validator <input> <expected> <actual>)");
                Environment.Exit(1);
            }

            string inputPath = args[0];
            string expectedPath = args[1];
            string actualPath = args[2];

            if (!File.Exists(inputPath))
            {
                Console.WriteLine("ERROR: no existe el archivo input");
                Environment.Exit(1);
            }
			
			if (!File.Exists(expectedPath))
            {
                Console.WriteLine("ERROR: no existe el archivo esperado");
                Environment.Exit(1);
            }
			
			if (!File.Exists(actualPath))
            {
                Console.WriteLine("ERROR: no existe el archivo generado por el estudiante");
                Environment.Exit(1);
            }

            var inputLines = File.ReadAllLines(inputPath);
            var expectedLines = File.ReadAllLines(expectedPath);
            var actualLines = File.ReadAllLines(actualPath);

            // Lectura de entrada
            var first = inputLines[0].Trim().Split();
            int N = int.Parse(first[0]);
            int C = int.Parse(first[1]);
            var weights = inputLines[1].Trim().Split().Select(int.Parse).ToArray();

            // Caso esperado: NO
            if (expectedLines[0].Trim() == "NO")
            {
                if (actualLines.Length == 0 || actualLines[0].Trim() != "NO")
                {
                    Console.WriteLine("Resultado incorrecto: se esperaba NO");
                    Environment.Exit(1);
                }
                Console.WriteLine("OK");
                return;
            }

            // Caso esperado: YES
            if (actualLines.Length < 2 || actualLines[0].Trim() != "YES")
            {
                Console.WriteLine("Resultado incorrecto: faltó YES o formato inválido");
                Environment.Exit(1);
            }

            // Validar subconjunto del estudiante
            var indices = actualLines[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(int.Parse).ToList();

            if (indices.Any(i => i < 1 || i > N))
            {
                Console.WriteLine("Resultado incorrecto: índice fuera de rango");
                Environment.Exit(1);
            }

            int suma = indices.Sum(i => weights[i - 1]);
            if (suma != C)
            {
                Console.WriteLine($"Resultado incorrecto: la suma es {suma}, se esperaba {C}");
                Environment.Exit(1);
            }

            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            Environment.Exit(1);
        }
    }
}

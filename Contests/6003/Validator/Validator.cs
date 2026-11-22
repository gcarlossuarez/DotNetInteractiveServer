// File: Validator.cs (Problema 2 - mínimo número de paquetes)
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
                Console.WriteLine(
                    "ERROR: argumentos insuficientes (uso: Validator <input> <expected> <actual>)"
                );
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

            // Leer entrada
            var first = inputLines[0].Trim().Split();
            int N = int.Parse(first[0]);
            int C = int.Parse(first[1]);
            var weights = inputLines[1].Trim().Split().Select(int.Parse).ToArray();

            // Caso imposible
            if (expectedLines[0].Trim() == "IMPOSSIBLE")
            {
                if (actualLines.Length == 0 || actualLines[0].Trim() != "IMPOSSIBLE")
                {
                    Console.WriteLine("Resultado incorrecto: se esperaba IMPOSSIBLE");
                    Environment.Exit(1);
                }
                Console.WriteLine("OK");
                return;
            }

            // Caso con solución
            if (actualLines.Length < 2)
            {
                Console.WriteLine("Resultado incorrecto: salida incompleta");
                Environment.Exit(1);
            }

            int claimedCount;
            if (!int.TryParse(actualLines[0].Trim(), out claimedCount) || claimedCount <= 0)
            {
                Console.WriteLine(
                    "Resultado incorrecto: primera línea debe ser número de paquetes > 0"
                );
                Environment.Exit(1);
            }

            var indices = actualLines[1]
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            if (indices.Count != claimedCount)
            {
                Console.WriteLine(
                    "Resultado incorrecto: número de índices no coincide con cantidad declarada"
                );
                Environment.Exit(1);
            }

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

            // Verificar que la solución es óptima (mínimo número de paquetes)
            int minCount = ComputeMinCount(weights, C);
            if (claimedCount != minCount)
            {
                Console.WriteLine(
                    $"Resultado incorrecto: la cantidad de paquetes no es mínima (se esperaba {minCount})"
                );
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

    // DP estilo coin change 0/1 para hallar mínimo número de paquetes
    static int ComputeMinCount(int[] w, int C)
    {
        const int INF = 1_000_000;
        var dp = Enumerable.Repeat(INF, C + 1).ToArray();
        dp[0] = 0;

        foreach (var wi in w)
        {
            for (int s = C; s >= wi; s--)
            {
                dp[s] = Math.Min(dp[s], dp[s - wi] + 1);
            }
        }
        return dp[C] >= INF ? -1 : dp[C];
    }
}

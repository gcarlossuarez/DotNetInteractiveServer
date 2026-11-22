// File: Validator.cs (Problema 3 - Mochila 0/1)
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
            var actualLines = File.ReadAllLines(actualPath);

            // Leer entrada
            var first = inputLines[0].Trim().Split();
            int N = int.Parse(first[0]);
            int C = int.Parse(first[1]);
            var w = new int[N];
            var v = new int[N];
            for (int i = 0; i < N; i++)
            {
                var parts = inputLines[i + 1].Trim().Split();
                w[i] = int.Parse(parts[0]);
                v[i] = int.Parse(parts[1]);
            }

            if (actualLines.Length < 2)
            {
                Console.WriteLine("Resultado incorrecto: salida incompleta");
                Environment.Exit(1);
            }

            if (!int.TryParse(actualLines[0].Trim(), out int claimedValue))
            {
                Console.WriteLine("Resultado incorrecto: primera línea debe ser el valor máximo");
                Environment.Exit(1);
            }

            var indices = actualLines[1]
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            if (indices.Any(i => i < 1 || i > N))
            {
                Console.WriteLine("Resultado incorrecto: índice fuera de rango");
                Environment.Exit(1);
            }

            int totalWeight = indices.Sum(i => w[i - 1]);
            int totalValue = indices.Sum(i => v[i - 1]);

            if (totalWeight > C)
            {
                Console.WriteLine(
                    $"Resultado incorrecto: se excede la capacidad ({totalWeight} > {C})"
                );
                Environment.Exit(1);
            }

            if (totalValue != claimedValue)
            {
                Console.WriteLine(
                    $"Resultado incorrecto: el valor declarado {claimedValue} no coincide con el calculado {totalValue}"
                );
                Environment.Exit(1);
            }

            int optimalValue = ComputeKnapsack(w, v, C);
            if (claimedValue != optimalValue)
            {
                Console.WriteLine(
                    $"Resultado incorrecto: el valor no es óptimo (se esperaba {optimalValue})"
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

    // DP clásico de mochila 0/1 para valor máximo
    static int ComputeKnapsack(int[] w, int[] v, int C)
    {
        int N = w.Length;
        var dp = new int[N + 1, C + 1];

        for (int i = 1; i <= N; i++)
        {
            for (int c = 0; c <= C; c++)
            {
                dp[i, c] = dp[i - 1, c];
                if (c >= w[i - 1])
                    dp[i, c] = Math.Max(dp[i, c], dp[i - 1, c - w[i - 1]] + v[i - 1]);
            }
        }
        return dp[N, C];
    }
}

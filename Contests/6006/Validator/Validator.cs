using System;
using System.Collections.Generic;
using System.IO;

// Clases para el algoritmo mejorado con diccionario de operaciones
public enum OperationType { INSERT, DELETE, MODIFY }

public class Operation
{
    public OperationType Type { get; set; }
    public int SourceLine { get; set; }      // Línea en archivo original (1-indexed)
    public int DestinationLine { get; set; } // Línea en archivo modificado (1-indexed)
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    
    public override string ToString()
    {
        return $"{Type}: Source={SourceLine}, Dest={DestinationLine}, Old='{OldValue}', New='{NewValue}'";
    }
}

class Validator
{
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("ERROR: uso Validator <input> <expected> <actual> [--improved]");
            Environment.Exit(1);
        }

        string inputPath = args[0];
        string expectedPath = args[1];
        string actualPath = args[2];
        
        // Verificar si se debe usar el algoritmo mejorado
        bool useOriginal = args.Length > 3 && args[3] == "--original";
        bool useImproved = !useOriginal;
        bool debugMode = args.Length > 4 && args[4] == "--debug";

        if (!File.Exists(inputPath))
        {
            Console.WriteLine("ERROR: falta archivo de entrada");
            Environment.Exit(1);
        }
        if (!File.Exists(expectedPath))
        {
            Console.WriteLine("ERROR: faltan archivo de salida esperada");
            Environment.Exit(1);
        }
        if (!File.Exists(actualPath))
        {
            Console.WriteLine("ERROR: falta archivo generado por el estudiante");
            Environment.Exit(1);
        }

        // Elegir algoritmo a usar
        if (useImproved)
        {
            if(debugMode) Console.WriteLine("🚀 USANDO ALGORITMO MEJORADO CON DICCIONARIO DE OPERACIONES");
            ValidateImproved(inputPath, expectedPath, actualPath, debugMode);
            return;
        }
        else
        {
            if(debugMode) Console.WriteLine("⚠️ USANDO ALGORITMO ORIGINAL (PUEDE TENER PROBLEMAS)");
        }

        try
        {
            var inputLines = File.ReadAllLines(inputPath);
            int sepIndex = Array.IndexOf(inputLines, "====");
            if (sepIndex == -1)
            {
                Console.WriteLine("ERROR: input inválido, falta separador ====");
                Environment.Exit(1);
            }
            var original = new List<string>(inputLines[..sepIndex]);
            var modified = new List<string>(inputLines[(sepIndex + 1)..]);

            var expectedLines = File.ReadAllLines(expectedPath);
            var actualLines = File.ReadAllLines(actualPath);

            if (actualLines.Length == 0)
            {
                Console.WriteLine("ERROR: salida inválida (vacía).");
                Environment.Exit(1);
            }
            if (actualLines[0] != expectedLines[0])
            {
                Console.WriteLine("ERROR: salida inválida (primera línea incorrecta).");
                Environment.Exit(1);
            }


            // número de operaciones del alumno
            string actLine = Array.Find(actualLines, l => l.StartsWith("Cantidad de operaciones"));
            if (actLine == null)
            {
                Console.WriteLine("ERROR: salida inválida (no hay cantidad de operaciones).");
                Environment.Exit(1);
            }
            int actValue = int.Parse(actLine.Split(':')[1]);

            // aplicar operaciones sobre original
            var resultado = new List<string>(original);

            foreach (var line in actualLines)
            {
                if (line.StartsWith("Cambiado"))
                {
                    // Cambiado Línea:4, valor 'X' por => Línea:4, valor:'Y'
                    var partes = line.Split(" por => ");
                    if (partes.Length != 2)
                    {
                        Console.WriteLine($"ERROR: 1) formato inválido en línea de cambio: {line}.");
                        Environment.Exit(1);
                    }
                    if (partes[0].IndexOf("Línea:") == -1 || partes[1].IndexOf("Línea:") == -1)
                    {
                        Console.WriteLine($"ERROR: 2) formato inválido en línea de cambio: {line}.");
                        Environment.Exit(1);
                    }
                    string idxStrFile1 = partes[0].Split("Línea:")[1].Split(',')[0];
                    int idxLineFile1 = int.Parse(idxStrFile1) - 1;

                    string idxStrFile2 = partes[1].Split("Línea:")[1].Split(',')[0];
                    int idxLineFile2 = int.Parse(idxStrFile2) - 1;

                    int oldStartIdx = partes[0].IndexOf("valor:'") + 7;
                    int oldEndIdx = partes[0].IndexOf("'", oldStartIdx);
                    string oldValue = partes[0].Substring(oldStartIdx, oldEndIdx - oldStartIdx);

                    int newStartIdx = partes[1].IndexOf("valor:'") + 7;
                    int newEndIdx = partes[1].IndexOf("'", newStartIdx);
                    string newValue = partes[1].Substring(newStartIdx, newEndIdx - newStartIdx);

                    // Find the line in resultado that contains the oldValue
                    int actualIdx = -1;
                    for (int i = 0; i < resultado.Count; i++)
                    {
                        if (resultado[i] == oldValue)
                        {
                            actualIdx = i;
                            break;
                        }
                    }

                    if (actualIdx == -1)
                    {
                        Console.WriteLine($"ERROR: no se encontró la línea a cambiar: {line}.");
                        Environment.Exit(1);
                    }

                    resultado[actualIdx] = newValue;

                    if (newValue != modified[idxLineFile2])
                    {
                        Console.WriteLine($"ERROR: el valor cambiado no coincide con el esperado: {line}.");
                        Environment.Exit(1);
                    }
                    if (oldValue != original[idxLineFile1])
                    {
                        Console.WriteLine($"ERROR: el valor original no coincide con el esperado: {line}.");
                        Environment.Exit(1);
                    }
                }
                else if (line.StartsWith("+ Línea"))
                {
                    // + Línea:3, valor:'texto'  => de la línea:1
                    if (line.IndexOf("Línea:") == -1)
                    {
                        Console.WriteLine($"ERROR: formato inválido en línea de adición de archivo original: {line}.");
                        Environment.Exit(1);
                    }
                    int idxLineFile1 = int.Parse(line.Split("Línea:")[1].Split(',')[0]) - 1;
                    if (idxLineFile1 == -1) idxLineFile1 = 0; // añadido al principio

                    if (idxLineFile1 < 0 || idxLineFile1 > resultado.Count)
                    {
                        Console.WriteLine($"ERROR: índice inválido en línea de adición de archivo original: {line}.");
                        Environment.Exit(1);
                    }
                    if (line.IndexOf("=> de la línea:") == -1)
                    {
                        Console.WriteLine($"ERROR: formato inválido en línea de adición de archivo modificado: {line}.");
                        Environment.Exit(1);
                    }
                    var parts = line.Split("=> de la línea:")[1].Split(',');
                    int idxFileModified = int.Parse(parts[0]) - 1;
                    int startIdx = line.IndexOf("valor:'") + 7;
                    int endIdx = line.IndexOf("'", startIdx);
                    string value = line.Substring(startIdx, endIdx - startIdx);
                    if (idxFileModified < 0 || idxFileModified >= modified.Count)
                    {
                        Console.WriteLine($"ERROR: índice inválido en línea de adición de archivo modificado: {line}.");
                        Environment.Exit(1);
                    }
                    if (value != modified[idxFileModified])
                    {
                        Console.WriteLine($"ERROR: el valor añadido no coincide con el esperado: {line}.");
                        Environment.Exit(1);
                    }
                    resultado.Insert(idxLineFile1, value);
                }
                else if (line.StartsWith("- Línea"))
                {
                    // - Línea:5, valor:'texto'
                    if (line.IndexOf("Línea:") == -1)
                    {
                        Console.WriteLine($"ERROR: formato inválido en línea de eliminación: {line}.");
                        Environment.Exit(1);
                    }
                    var parts = line.Split("Línea:")[1].Split(',');
                    int originalIdx = int.Parse(parts[0]) - 1;
                    if (originalIdx < 0 || originalIdx >= original.Count)
                    {
                        Console.WriteLine($"ERROR: índice inválido en línea de eliminación: {line}.");
                        Environment.Exit(1);
                    }
                    int startIdx = line.IndexOf("valor:'") + 7;
                    int endIdx = line.IndexOf("'", startIdx);
                    string value = line.Substring(startIdx, endIdx - startIdx);
                    if (value != original[originalIdx])
                    {
                        Console.WriteLine($"ERROR: el valor eliminado no coincide con el esperado: {line}.");
                        Environment.Exit(1);
                    }

                    // Find the line to remove by its content
                    int removeIdx = -1;
                    for (int i = 0; i < resultado.Count; i++)
                    {
                        if (resultado[i] == value)
                        {
                            removeIdx = i;
                            break;
                        }
                    }

                    if (removeIdx == -1)
                    {
                        Console.WriteLine($"ERROR: no se encontró la línea a eliminar: {line}.");
                        Environment.Exit(1);
                    }

                    resultado.RemoveAt(removeIdx);
                }
            }

            // comparar con el modificado real
            if (resultado.Count != modified.Count)
            {
                Console.WriteLine("ERROR: la reconstrucción no llega al texto final.");
                Environment.Exit(1);
            }
            for (int i = 0; i < resultado.Count; i++)
            {
                if (resultado[i] != modified[i])
                {
                    Console.WriteLine("ERROR: diferencia en línea " + (i + 1));
                    Console.WriteLine($"Esperado: '{modified[i]}'.");
                    Console.WriteLine($"Obtenido: '{resultado[i]}'.");
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR en el validador: " + ex.Message);
            Console.WriteLine("Stack trace: " + ex.StackTrace);
            Environment.Exit(1);
        }
    }

    // NUEVO ALGORITMO MEJORADO CON DICCIONARIO DE OPERACIONES
    static void ValidateImproved(string inputPath, string expectedPath, string actualPath,
            bool debugMode)
    {
        try
        {
            // Fase 1: Lectura y parsing inicial
            var inputLines = File.ReadAllLines(inputPath);
            int sepIndex = Array.IndexOf(inputLines, "====");
            if (sepIndex == -1)
            {
                Console.WriteLine("ERROR: input inválido, falta separador ====");
                Environment.Exit(1);
            }
            
            var original = new List<string>(inputLines[..sepIndex]);
            var modified = new List<string>(inputLines[(sepIndex + 1)..]);
            var expectedLines = File.ReadAllLines(expectedPath);
            var actualLines = File.ReadAllLines(actualPath);

            // Validaciones básicas
            if (actualLines.Length == 0)
            {
                Console.WriteLine("ERROR: salida inválida (vacía).");
                Environment.Exit(1);
            }
            if (actualLines[0] != expectedLines[0])
            {
                Console.WriteLine("ERROR: salida inválida (primera línea incorrecta).");
                Environment.Exit(1);
            }

            // Extraer cantidad de operaciones
            string actLine = Array.Find(actualLines, l => l.StartsWith("Cantidad de operaciones"));
            if (actLine == null)
            {
                Console.WriteLine("ERROR: salida inválida (no hay cantidad de operaciones).");
                Environment.Exit(1);
            }
            int actValue = int.Parse(actLine.Split(':')[1]);

            // Fase 2: Crear diccionario de operaciones por línea
            var operacionesPorLinea = new Dictionary<int, List<Operation>>();
            
            if(debugMode) Console.WriteLine("🔍 PROCESANDO OPERACIONES:");
            foreach (var line in actualLines)
            {
                if (line.StartsWith("+ Línea"))
                {
                    var operation = ParseInsertOperation(line, original, modified);
                    AgregarOperacion(operacionesPorLinea, operation.SourceLine, operation);
                    if(debugMode) Console.WriteLine($"  ➕ {operation}");
                }
                else if (line.StartsWith("- Línea"))
                {
                    var operation = ParseDeleteOperation(line, original, modified);
                    AgregarOperacion(operacionesPorLinea, operation.SourceLine, operation);
                    if(debugMode) Console.WriteLine($"  ➖ {operation}");
                }
                else if (line.StartsWith("Cambiado"))
                {
                    var operation = ParseModifyOperation(line, original, modified);
                    AgregarOperacion(operacionesPorLinea, operation.SourceLine, operation);
                    if(debugMode) Console.WriteLine($"  🔄 {operation}");
                }
            }

            // Fase 3: MERGE - Reconstrucción línea por línea
            if(debugMode) Console.WriteLine("\n🏗️ INICIANDO FASE DE MERGE:");
            var resultado = ReconstructFile(original, modified, operacionesPorLinea, debugMode);

            // Fase 4: Validación final
            if (debugMode) Console.WriteLine("\n🔍 VALIDANDO RESULTADO FINAL:");
            if (ValidateReconstruction(resultado, modified, debugMode))
            {
                if(debugMode) Console.WriteLine("✅ RECONSTRUCCIÓN EXITOSA");
                Console.WriteLine("OK");
            }
            else
            {
                Console.WriteLine("❌ ERROR: la reconstrucción no coincide con el archivo modificado");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: en el validador mejorado: " + ex.Message);
            Environment.Exit(1);
        }
    }

    static void AgregarOperacion(Dictionary<int, List<Operation>> dict, int lineNumber, Operation operation)
    {
        if (!dict.ContainsKey(lineNumber))
        {
            dict[lineNumber] = new List<Operation>();
        }
        dict[lineNumber].Add(operation);
    }

    static Operation ParseInsertOperation(string line, List<string> original, List<string> modified)
    {
        // + Línea:0, valor:'x' => de la línea:1
        if (line.IndexOf("Línea:") == -1 || line.IndexOf("=> de la línea:") == -1)
        {
            throw new Exception($"Formato inválido en línea de adición: {line}");
        }

        int destLine = int.Parse(line.Split("Línea:")[1].Split(',')[0]);
        var parts = line.Split("=> de la línea:")[1].Split(',');
        int srcLineModified = int.Parse(parts[0]);
        
        int startIdx = line.IndexOf("valor:'") + 7;
        int endIdx = line.IndexOf("'", startIdx);
        string value = line.Substring(startIdx, endIdx - startIdx);

        // Verificar que el valor existe en modified
        if (srcLineModified < 1 || srcLineModified > modified.Count)
        {
            throw new Exception($"Índice inválido en archivo modificado: {line}");
        }
        if (value != modified[srcLineModified - 1])
        {
            throw new Exception($"El valor añadido no coincide con el esperado: {line}");
        }

        return new Operation
        {
            Type = OperationType.INSERT,
            SourceLine = destLine, // Línea donde insertar en el original
            DestinationLine = srcLineModified,
            OldValue = "",
            NewValue = value
        };
    }

    static Operation ParseDeleteOperation(string line, List<string> original, List<string> modified)
    {
        // - Línea:5, valor:'texto'
        if (line.IndexOf("Línea:") == -1)
        {
            throw new Exception($"Formato inválido en línea de eliminación: {line}");
        }

        var parts = line.Split("Línea:")[1].Split(',');
        int originalIdx = int.Parse(parts[0]);
        
        int startIdx = line.IndexOf("valor:'") + 7;
        int endIdx = line.IndexOf("'", startIdx);
        string value = line.Substring(startIdx, endIdx - startIdx);

        if (originalIdx < 1 || originalIdx > original.Count)
        {
            throw new Exception($"Índice inválido en línea de eliminación: {line}");
        }
        if (value != original[originalIdx - 1])
        {
            throw new Exception($"El valor eliminado no coincide con el esperado: {line}");
        }

        return new Operation
        {
            Type = OperationType.DELETE,
            SourceLine = originalIdx,
            DestinationLine = -1,
            OldValue = value,
            NewValue = ""
        };
    }

    static Operation ParseModifyOperation(string line, List<string> original, List<string> modified)
    {
        // Cambiado Línea:4, valor 'X' por => Línea:4, valor:'Y'
        var partes = line.Split(" por => ");
        if (partes.Length != 2)
        {
            throw new Exception($"Formato inválido en línea de cambio: {line}");
        }

        string idxStrFile1 = partes[0].Split("Línea:")[1].Split(',')[0];
        int idxLineFile1 = int.Parse(idxStrFile1);

        string idxStrFile2 = partes[1].Split("Línea:")[1].Split(',')[0];
        int idxLineFile2 = int.Parse(idxStrFile2);

        int oldStartIdx = partes[0].IndexOf("valor:'") + 7;
        int oldEndIdx = partes[0].IndexOf("'", oldStartIdx);
        string oldValue = partes[0].Substring(oldStartIdx, oldEndIdx - oldStartIdx);

        int newStartIdx = partes[1].IndexOf("valor:'") + 7;
        int newEndIdx = partes[1].IndexOf("'", newStartIdx);
        string newValue = partes[1].Substring(newStartIdx, newEndIdx - newStartIdx);

        // Verificaciones
        if (idxLineFile1 < 1 || idxLineFile1 > original.Count)
        {
            throw new Exception($"Índice inválido en archivo original: {line}");
        }
        if (idxLineFile2 < 1 || idxLineFile2 > modified.Count)
        {
            throw new Exception($"Índice inválido en archivo modificado: {line}");
        }
        if (oldValue != original[idxLineFile1 - 1])
        {
            throw new Exception($"El valor original no coincide con el esperado: {line}");
        }
        if (newValue != modified[idxLineFile2 - 1])
        {
            throw new Exception($"El valor cambiado no coincide con el esperado: {line}");
        }

        return new Operation
        {
            Type = OperationType.MODIFY,
            SourceLine = idxLineFile1,
            DestinationLine = idxLineFile2,
            OldValue = oldValue,
            NewValue = newValue
        };
    }

    static List<string> ReconstructFile(List<string> original, List<string> modified, Dictionary<int, List<Operation>> operacionesPorLinea,
                    bool debugMode)
    {
        var resultado = new List<string>();
        
        // Procesar inserciones en posición 0 primero (al principio)
        if (operacionesPorLinea.ContainsKey(0))
        {
            var insertions = operacionesPorLinea[0];
            if(debugMode) Console.WriteLine($"  ➕ Procesando {insertions.Count} inserción(es) al principio");
            
            foreach (var op in insertions)
            {
                if (op.Type == OperationType.INSERT)
                {
                    resultado.Add(op.NewValue);
                    if(debugMode) Console.WriteLine($"    ➕ Insertando al principio: '{op.NewValue}'");
                }
            }
        }
        
        // Procesar cada línea del archivo original
        for (int i = 1; i <= original.Count; i++)
        {
            string currentLine = original[i - 1];
            bool lineProcessed = false;
            
            // Verificar si hay operaciones para esta línea
            if (operacionesPorLinea.ContainsKey(i))
            {
                var operaciones = operacionesPorLinea[i];
                if(debugMode) Console.WriteLine($"  📝 Línea {i}: {operaciones.Count} operación(es)");
                
                foreach (var op in operaciones)
                {
                    switch (op.Type)
                    {
                        case OperationType.DELETE:
                            if(debugMode) Console.WriteLine($"    ❌ DELETE: Omitiendo '{op.OldValue}'");
                            lineProcessed = true; // No agregar esta línea
                            break;
                            
                        case OperationType.MODIFY:
                            if(debugMode) Console.WriteLine($"    ✏️ MODIFY: '{op.OldValue}' → '{op.NewValue}'");
                            resultado.Add(op.NewValue);
                            lineProcessed = true;
                            break;
                            
                        case OperationType.INSERT:
                            // Las inserciones en posiciones > 0 se agregan después de procesar la línea actual
                            if(debugMode) Console.WriteLine($"    ➕ INSERT: Agregando '{op.NewValue}' después de línea {i}");
                            if (!lineProcessed)
                            {
                                resultado.Add(currentLine); // Agregar línea original primero
                                lineProcessed = true;
                            }
                            resultado.Add(op.NewValue); // Agregar inserción
                            break;
                    }
                }
            }
            
            // Si no hubo operaciones o no se procesó la línea, agregar la línea original
            if (!lineProcessed)
            {
                resultado.Add(currentLine);
                if(debugMode) Console.WriteLine($"  📄 Copiando línea {i} sin cambios: '{currentLine}'");
            }
        }

        return resultado;
    }

    static bool ValidateReconstruction(List<string> resultado, List<string> modified,
                        bool debugMode)
    {
        if (resultado.Count != modified.Count)
        {
            if(debugMode) Console.WriteLine($"❌ Diferente número de líneas: resultado={resultado.Count}, modificado={modified.Count}");
            return false;
        }

        for (int i = 0; i < resultado.Count; i++)
        {
            if (resultado[i] != modified[i])
            {
                if(debugMode) Console.WriteLine($"❌ Diferencia en línea {i + 1}:");
                if(debugMode) Console.WriteLine($"   Esperado: '{modified[i]}'");
                if(debugMode) Console.WriteLine($"   Obtenido: '{resultado[i]}'");
                return false;
            }
        }

        if(debugMode) Console.WriteLine($"✅ Todas las {resultado.Count} líneas coinciden perfectamente");
        return true;
    }
}



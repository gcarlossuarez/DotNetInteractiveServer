using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
public static class DatasetInputs
{
    public static IResult Read(string root, string problemId, string fileName)
    {
        if (!Regex.IsMatch(problemId, @"\A[A-Za-z0-9_-]+\z") ||
            !Regex.IsMatch(fileName, @"\Adatos[A-Za-z0-9_-]*\.txt\z", RegexOptions.IgnoreCase))
            return Results.BadRequest("Problema o nombre de entrada no válido.");
        var problem = Path.Combine(root, problemId);
        var folder = Path.Combine(problem, "DataSet");
        var file = Path.Combine(folder, fileName);
        try
        {
            foreach (var path in new[] { root, problem, folder, file })
            {
                if (!Path.Exists(path)) return Results.NotFound("No se encontró la entrada. Comprueba la carpeta Contests configurada en el lanzador.");
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    return Results.BadRequest("No se permiten enlaces en las rutas de entradas.");
            }
            if (new FileInfo(file).Length > 2 * 1024 * 1024)
                return Results.BadRequest("La entrada supera el límite de visualización de 2 MB.");
            return Results.Text(File.ReadAllText(file), "text/plain; charset=utf-8");
        }
        catch (IOException) { return Results.Problem("No se pudo leer el caso de prueba."); }
        catch (UnauthorizedAccessException) { return Results.Problem("No hay permiso de lectura del caso."); }
    }
}

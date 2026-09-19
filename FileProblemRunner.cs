using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
public static class FileProblemRunner
{
    public sealed record Mode(string InputMode = "stdin", string InputDirectory = "DataSet", string OutputDirectory = "Output", string OutputPrefix = "Output_", string OutputMode = "per-file", string? ValidatorPath = null);
    public sealed record Submission(string Code, string Problem, int TimeoutMs = 5000);
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    static readonly SemaphoreSlim Queue = new(1, 1);
    static Mode GetMode(string appRoot, string contests, string id)
    {
        if (!Regex.IsMatch(id, @"\A[A-Za-z0-9_-]+\z")) throw new InvalidDataException("Identificador inválido.");
        var specific = Path.Combine(contests, id, "problem.json");
        if (File.Exists(specific)) return JsonSerializer.Deserialize<Mode>(File.ReadAllText(specific), Json) ?? new();
        var settings = Path.Combine(appRoot, "execution-settings.json");
        if (!File.Exists(settings)) return new();
        using var doc = JsonDocument.Parse(File.ReadAllText(settings));
        return doc.RootElement.TryGetProperty("problems", out var problems) && problems.TryGetProperty(id, out var mode)
            ? mode.Deserialize<Mode>(Json) ?? new() : new();
    }
    static string Subpath(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Se requiere una ruta relativa.");
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (full != Path.GetFullPath(root) && !full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Ruta fuera de la carpeta de trabajo.");
        return full;
    }
    public static void Map(WebApplication app, string appRoot, string contests)
    {
        app.MapGet("/datasets/{problemId}/execution-mode", (string problemId) =>
        {
            try { return Results.Json(GetMode(appRoot, contests, problemId)); }
            catch (Exception e) when(e is IOException or JsonException) { return Results.BadRequest(new { message = e.Message }); }
        });
        app.MapGet("/datasets/{problemId}/validation-route", (string problemId) =>
        {
            try
            {
                var mode = GetMode(appRoot, contests, problemId);
                var endpoint = mode.InputMode.Equals("file", StringComparison.OrdinalIgnoreCase) ? "/validate-files" : "/validate-dataset";
                return Results.Json(new { problem = problemId, inputMode = mode.InputMode, outputMode = mode.OutputMode, endpoint, validatorPath = mode.ValidatorPath });
            }
            catch (Exception e) when (e is IOException or JsonException or InvalidDataException)
            {
                return Results.BadRequest(new { message = e.Message });
            }
        });        app.MapPost("/execute-files", async (Submission request, HttpContext context) =>
        {
            var ct = context.RequestAborted;
            if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 200000 || request.TimeoutMs < 1 || request.TimeoutMs > 120000)
                return Results.BadRequest("Código o tiempo inválidos.");
            await Queue.WaitAsync(ct);
            var temp = Path.Combine(Path.GetTempPath(), "JudgeFileRuns", Guid.NewGuid().ToString("N"));
            try
            {
                var mode = GetMode(appRoot, contests, request.Problem);
                if (!mode.InputMode.Equals("file", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest("Este problema no está configurado en modo file.");
                var problem = Path.Combine(contests, request.Problem);
                var originalInput = Path.Combine(problem, "DataSet");
                var cases = Directory.GetFiles(originalInput, "dato*.txt").OrderBy(x => x, StringComparer.Ordinal).ToArray();
                if (cases.Length == 0) return Results.BadRequest("No hay casos dato*.txt.");
                var source = Path.Combine(temp, "student");
                Directory.CreateDirectory(source);
                await File.WriteAllTextAsync(Path.Combine(source, "Program.cs"), request.Code, ct);
                await File.WriteAllTextAsync(Path.Combine(source, "Student.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><UseAppHost>false</UseAppHost></PropertyGroup></Project>", ct);
                await File.WriteAllTextAsync(Path.Combine(source, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>", ct);
                var build = await Run("dotnet", new[] { "build", "Student.csproj", "-c", "Release", "-o", "built", "--nologo" }, source, 120000, ct);
                if (build.Exit != 0) return Results.Json(new { output = build.Out, error = build.Err, timeMs = build.Time, exitCode = build.Exit }, statusCode: 422);
                var built = Path.Combine(source, "built");
                var runtimeInput = Subpath(built, mode.InputDirectory);
                Directory.CreateDirectory(runtimeInput);
                foreach (var input in cases) File.Copy(input, Path.Combine(runtimeInput, Path.GetFileName(input)));
                Directory.CreateDirectory(Subpath(built, mode.OutputDirectory));
                var result = await Run("dotnet", new[] { Path.Combine(built, "Student.dll") }, built, request.TimeoutMs, ct);
                return Results.Json(new { output = result.Out, error = result.Err, timeMs = result.Time, exitCode = result.Exit });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return Results.StatusCode(499); }
            catch (Exception e) { return Results.Json(new { message = e.Message }, statusCode: 400); }
            finally { try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch (IOException) { } Queue.Release(); }
        });        app.MapPost("/validate-files", async (Submission request, HttpContext context) =>
        {
            var ct = context.RequestAborted;
            if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 200000 || request.TimeoutMs < 1 || request.TimeoutMs > 120000)
            { context.Response.StatusCode = 400; await context.Response.WriteAsync("Código o tiempo inválidos.", ct); return; }
            await Queue.WaitAsync(ct);
            var temp = Path.Combine(Path.GetTempPath(), "JudgeFileRuns", Guid.NewGuid().ToString("N"));
            try
            {
                var mode = GetMode(appRoot, contests, request.Problem);
                if (mode.InputMode != "file") throw new InvalidDataException("Este problema no está configurado en modo file.");
                if (mode.OutputPrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || mode.OutputPrefix.Contains('/') || mode.OutputPrefix.Contains('\\')) throw new InvalidDataException("Prefijo inválido.");
                var problem = Path.Combine(contests, request.Problem);
                // El DataSet completo se entrega al programa. Cada archivo es un caso,
                // pero el programa se ejecuta una sola vez y escribe la salida agregada.
                var originalInput = Path.Combine(problem, "DataSet");
                var expectedDirectory = Path.Combine(problem, ".Expected");
                var cases = Directory.GetFiles(originalInput, "dato*.txt").OrderBy(x => x, StringComparer.Ordinal).ToArray();
                if (cases.Length == 0) throw new InvalidDataException("No hay casos dato*.txt.");
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                async Task Send(string type, object data) { await context.Response.WriteAsync($"event: {type}\ndata: {JsonSerializer.Serialize(data)}\n\n", ct); await context.Response.Body.FlushAsync(ct); }
                await Send("start", new { totalCases = cases.Length, problem = request.Problem });
                var source = Path.Combine(temp, "student"); Directory.CreateDirectory(source);
                await File.WriteAllTextAsync(Path.Combine(source, "Program.cs"), request.Code, ct);
                await File.WriteAllTextAsync(Path.Combine(source, "Student.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><UseAppHost>false</UseAppHost></PropertyGroup></Project>", ct);
                await File.WriteAllTextAsync(Path.Combine(source, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>", ct);
                var build = await Run("dotnet", new[] { "build", "Student.csproj", "-c", "Release", "-o", "built", "--nologo" }, source, 120000, ct);
                if (build.Exit != 0) throw new InvalidDataException("Error de compilación del alumno:\n" + build.Out + build.Err);
                var dll = Path.Combine(source, "built", "Student.dll");
                var built = Path.Combine(source, "built");
                var runtimeInput = Subpath(built, mode.InputDirectory); Directory.CreateDirectory(runtimeInput);
                foreach (var input in cases) File.Copy(input, Path.Combine(runtimeInput, Path.GetFileName(input)));
                var runtimeOutput = Subpath(built, mode.OutputDirectory); Directory.CreateDirectory(runtimeOutput);
                var result = await Run("dotnet", new[] { Path.Combine(built, "Student.dll") }, built, request.TimeoutMs, ct);
                var actualFile = Path.Combine(temp, "actual-console.txt");
                await File.WriteAllTextAsync(actualFile, result.Out, ct);
                string verdict, detail = "";
                if (result.Exit != 0) { verdict = result.Exit == -2 ? "Time Limit" : "Runtime Error"; detail = result.Err; }
                else if (mode.OutputMode.Equals("console-batch", StringComparison.OrdinalIgnoreCase))
                {
                    if (mode.ValidatorPath is { Length: > 0 })
                    {
                        var validatorDir = Subpath(appRoot, mode.ValidatorPath);
                        if (!Directory.Exists(validatorDir)) throw new InvalidDataException("Falta el validador por lotes: " + validatorDir);
                        var validator = await BuildValidator(validatorDir, temp, ct);
                        var checkedRun = await Run("dotnet", new[] { validator, originalInput, expectedDirectory, actualFile, Path.Combine(source, "Program.cs") }, temp, request.TimeoutMs, ct);
                        verdict = checkedRun.Exit == 0 && checkedRun.Out.Trim() == "OK" ? "Accepted" : checkedRun.Exit == 1 ? "Wrong Answer" : "Validator Error";
                        detail = checkedRun.Out + checkedRun.Err;
                    }
                    else
                    {
                        var expected = string.Join(Environment.NewLine, cases.SelectMany(x => File.ReadAllLines(Path.Combine(problem, ".Expected", "Output_" + Path.GetFileName(x)))).Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
                        verdict = result.Out.Trim() == expected ? "Accepted" : "Wrong Answer";
                        detail = verdict == "Accepted" ? "Comparación exacta agregada." : "La salida agregada no coincide.";
                    }
                }
                else throw new InvalidDataException("OutputMode no soportado: " + mode.OutputMode);
                await Send("case-result", new { caseNumber = 1, totalCases = 1, caseName = "DataSet completo", result = verdict, timeMs = result.Time, diff = "", validatorOutput = detail });                await Send("complete", new { totalCases = cases.Length, completed = true });
            }
            catch (OperationCanceledException) when(ct.IsCancellationRequested) { }
            catch (Exception e)
            {
                if (!context.Response.HasStarted) context.Response.ContentType = "text/event-stream";
                await context.Response.WriteAsync("event: error\ndata: " + JsonSerializer.Serialize(new { message = e.Message }) + "\n\n", ct);
            }
            finally { try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch (IOException) { } Queue.Release(); }
        });
    }
    static async Task<string> BuildValidator(string sourceDir, string temp, CancellationToken ct)
    {
        var work = Path.Combine(temp, "validator"); Directory.CreateDirectory(work);
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file); if (rel.Split(Path.DirectorySeparatorChar).Any(x => x is "bin" or "obj")) continue;
            var target = Subpath(work, rel); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
        }
        var project = Directory.GetFiles(work, "*.csproj"); if (project.Length != 1) throw new InvalidDataException("El validador debe tener un único csproj.");
        var build = await Run("dotnet", new[] { "build", project[0], "-c", "Release", "-o", "built", "--nologo" }, work, 120000, ct);
        if (build.Exit != 0) throw new InvalidDataException("Error de compilación del validador:\n" + build.Out + build.Err);
        var dll = Path.Combine(work, "built", Path.GetFileNameWithoutExtension(project[0]) + ".dll"); if (!File.Exists(dll)) throw new InvalidDataException("No se encontró la DLL del validador."); return dll;
    }
    static async Task<(int Exit, string Out, string Err, long Time)> Run(string executable, string[] args, string directory, int timeout, CancellationToken ct)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo(executable) { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true, UseShellExecute = false, CreateNoWindow = true } };
        foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct); limit.CancelAfter(timeout);
        var watch = Stopwatch.StartNew(); process.Start(); process.StandardInput.Close();
        async Task<string> Read(StreamReader reader)
        {
            var text = new StringBuilder(); var buffer = new char[4096]; int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), limit.Token)) > 0)
            { text.Append(buffer, 0, count); if (text.Length > 1000000) { process.Kill(true); throw new InvalidDataException("Salida del proceso excesiva."); } }
            return text.ToString();
        }
        var stdout = Read(process.StandardOutput); var stderr = Read(process.StandardError);
        try { await process.WaitForExitAsync(limit.Token); return (process.ExitCode, await stdout, await stderr, watch.ElapsedMilliseconds); }
        catch { if (!process.HasExited) process.Kill(true); try { await Task.WhenAll(stdout, stderr); } catch { } ct.ThrowIfCancellationRequested(); if (limit.IsCancellationRequested) return (-2, "", "Tiempo límite excedido.", watch.ElapsedMilliseconds); throw; }
    }
}







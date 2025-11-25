// ===============================
// 🧰 Juez Local C# (Sandbox dinámico)
// Basado en Microsoft.DotNet.Interactive
// Autor: Germán Carlos Suárez
// ===============================
// 
// 📐 ARQUITECTURA GENERAL:
// ┌─────────────────────────────────────────────────────────────┐
// │                     DOTNET INTERACTIVE SERVER                │
// ├─────────────────────────────────────────────────────────────┤
// │                                                               │
// │  Frontend (HTTP)  →  ASP.NET Core API  →  .NET Interactive  │
// │                                                               │
// │  ┌──────────┐      ┌──────────┐       ┌──────────────────┐ │
// │  │ Client   │ POST │/execute  │       │ CSharpKernel     │ │
// │  │ (HTML/JS)│ ───→ │/validate │ ───→  │ (Roslyn Compiler)│ │
// │  │          │ SSE  │/datasets │       │                  │ │
// │  └──────────┘      └──────────┘       └──────────────────┘ │
// │                                                               │
// └─────────────────────────────────────────────────────────────┘
// 

using Microsoft.AspNetCore.Builder;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;

// ═══════════════════════════════════════════════════════════════
// 📚 SECCIÓN 1: CONFIGURACIÓN INICIAL DEL SERVIDOR
// ═══════════════════════════════════════════════════════════════
// Aquí se configura:
// - CORS (permitir peticiones desde cualquier origen)
// - JSON case-insensitive (Code = code = CODE)
// ═══════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// CORS para permitir peticiones desde cualquier origen
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JSON case-insensitive
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

// === ⚠️ IMPORTANTE: CORS debe ir ANTES de los endpoints ===
app.UseCors();

// ═══════════════════════════════════════════════════════════════
// 📚 SECCIÓN 2: ENDPOINTS DEL API
// ═══════════════════════════════════════════════════════════════
// 
// 🗺️ MAPA DE ENDPOINTS:
// 
//   GET  /ping                     → Verifica que el servidor esté vivo
//   GET  /info                     → Información del sistema (.NET, OS)
//   GET  /version                  → Versión de la aplicación
//   POST /execute                  → Ejecuta código C# (con timeout)
//   POST /reset                    → Libera memoria (GC.Collect)
//   POST /upload-dataset           → Sube archivos de prueba
//   GET  /datasets                 → Lista todos los datasets
//   GET  /datasets/{problemId}     → Info de un dataset específico
//   POST /validate-dataset         → Valida código contra dataset (SSE/JSON)
//   DELETE /datasets/{problemId}   → Elimina un dataset específico
// 
// ═══════════════════════════════════════════════════════════════

// --- 1. Verifica si el sandbox está vivo ---
app.MapGet("/ping", () => Results.Ok("✅ Sandbox activo y listo"));

// --- 2. Muestra información del entorno ---
app.MapGet("/info", () => Results.Ok(new
{
    Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
    Time = DateTime.Now
}));

// --- 3. Devuelve la versión compilada ---
app.MapGet("/version", () =>
{
    var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev";
    return Results.Ok(new { version });
});

// ┌─────────────────────────────────────────────────────────────┐
// │ 📚 ENDPOINT CRÍTICO: /execute                                │
// ├─────────────────────────────────────────────────────────────┤
// │ Flujo de ejecución:                                          │
// │   1. Validar input (código no vacío)                         │
// │   2. Crear kernel aislado (CompositeKernel + CSharpKernel)  │
// │   3. Configurar timeout (5 segundos por defecto)             │
// │   4. Suscribirse a eventos (stdout, stderr)                  │
// │   5. Configurar stdin si existe                              │
// │   6. Ejecutar código con SubmitCode                          │
// │   7. Capturar salida y errores                               │
// │   8. Retornar resultado en JSON                              │
// └─────────────────────────────────────────────────────────────┘

app.MapPost("/execute", async (Request input) =>
{
    if (input == null || string.IsNullOrWhiteSpace(input.Code))
        return Results.BadRequest("Missing code");

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(input.TimeoutMs > 0 ? input.TimeoutMs : 5000));

    try
    {
        using var kernel = new CompositeKernel("root");
        using var csharpKernel = new CSharpKernel().UseKernelHelpers();
        kernel.Add(csharpKernel);

        var sb = new StringBuilder();

        kernel.KernelEvents.Subscribe(e =>
        {
            switch (e)
            {
                
                case StandardOutputValueProduced std:
                    // 🖨️ Console.WriteLine() o Console.Write()
                    var stdValue = std.FormattedValues.FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(stdValue))
                        sb.Append(stdValue); // ⚠️ Usar Append, NO AppendLine (ya tiene \n)
                    break;
                case DisplayedValueProduced val:
                    // 📊 Valores mostrados automáticamente (ej: última línea sin ;)
                    var valValue = val.FormattedValues.FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(valValue))
                        sb.Append(valValue); // ⚠️ Usar Append, NO AppendLine
                    break;
                case CommandFailed fail:
                    // ❌ Errores de compilación o runtime
                    sb.AppendLine(fail.Message);
                    break;  
            }
        });

        // 🔧 Configurar stdin si se proporciona
        if (!string.IsNullOrEmpty(input.Stdin))
        {
            Console.SetIn(new StringReader(input.Stdin));
        }

        // 🚀 Compilar el código (define clases, métodos, etc.)
        await kernel.SendAsync(new Microsoft.DotNet.Interactive.Commands.SubmitCode(input.Code), cts.Token);

        // 🎯 Si el código define Main(), invocarlo usando reflexión
        await InvokeMainIfExists(kernel, input.Code, cts.Token);

        return Results.Ok(new { output = sb.ToString() });
    }
    catch (OperationCanceledException)
    {
        return Results.Ok(new { output = "⏳ Tiempo de ejecución excedido (5 segundos)." });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { output = $"⚠️ Excepción: {ex.Message}" });
    }
});


// --- 5. Reinicia el kernel y limpia la memoria ---
app.MapPost("/reset", () =>
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
    return Results.Ok("🔄 Kernel reiniciado");
});

// --- 7. Subir dataset de problemas ---
app.MapPost("/upload-dataset", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var upload = JsonSerializer.Deserialize<DatasetUpload>(body, options);

    if (upload == null || string.IsNullOrWhiteSpace(upload.ProblemId))
        return Results.BadRequest("Missing problemId");

    if (upload.Files == null || upload.Files.Count == 0)
        return Results.BadRequest("No files provided");

    string basePath = Path.Combine(AppContext.BaseDirectory, "Contests", upload.ProblemId);
    Directory.CreateDirectory(basePath);

    int saved = 0;
    foreach (var f in upload.Files)
    {
        if (string.IsNullOrWhiteSpace(f.Path) || string.IsNullOrEmpty(f.Content))
            continue;

        // Evitar rutas peligrosas
        var safePath = f.Path.Replace("..", "").Replace("\\", "/").TrimStart('/');
        var fullPath = Path.Combine(basePath, safePath);

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(fullPath, f.Content, Encoding.UTF8);
        saved++;
    }

    return Results.Json(new { ok = true, problem = upload.ProblemId, saved });
});

// Lista TODO lo instalado
app.MapGet("/datasets", () =>
{
    string contestsRoot = Path.Combine(AppContext.BaseDirectory, "Contests");
    if (!Directory.Exists(contestsRoot))
        return Results.Json(new { problems = Array.Empty<object>() });

    var problems = Directory.GetDirectories(contestsRoot)
        .Select(dir =>
        {
            var id = Path.GetFileName(dir);
            var inputsDir   = Path.Combine(dir, "DataSet");
            var expectedDir = Path.Combine(dir, ".Expected");
            var inputs   = Directory.Exists(inputsDir)   ? Directory.GetFiles(inputsDir, "*.txt").Select(Path.GetFileName).OrderBy(x => x).ToArray() : Array.Empty<string>();
            var expected = Directory.Exists(expectedDir) ? Directory.GetFiles(expectedDir, "*.txt").Select(Path.GetFileName).OrderBy(x => x).ToArray() : Array.Empty<string>();

            return new {
                id,
                inputsCount   = inputs.Length,
                expectedCount = expected.Length,
                inputs,
                expected
            };
        })
        .OrderBy(p => p.id)
        .ToArray();

    return Results.Json(new { problems });
});

// Detalle por problema
app.MapGet("/datasets/{problemId}", (string problemId) =>
{
    string basePath    = Path.Combine(AppContext.BaseDirectory, "Contests", problemId);
    string inputsDir   = Path.Combine(basePath, "DataSet");
    string expectedDir = Path.Combine(basePath, ".Expected");

    var inputs   = Directory.Exists(inputsDir)   ? Directory.GetFiles(inputsDir, "*.txt").Select(Path.GetFileName).OrderBy(x => x).ToArray() : Array.Empty<string>();
    var expected = Directory.Exists(expectedDir) ? Directory.GetFiles(expectedDir, "*.txt").Select(Path.GetFileName).OrderBy(x => x).ToArray() : Array.Empty<string>();

    bool installed = Directory.Exists(basePath) && inputs.Length > 0;

    return Results.Json(new {
        problemId,
        installed,
        inputsCount   = inputs.Length,
        expectedCount = expected.Length,
        inputs,
        expected
    });
});

// ┌─────────────────────────────────────────────────────────────┐
// │ 🗑️ ENDPOINT: DELETE /datasets/{problemId}                   │
// ├─────────────────────────────────────────────────────────────┤
// │ Limpia completamente un problema:                           │
// │  - Borra DataSet/ (inputs)                                  │
// │  - Borra .Expected/ (outputs esperados)                     │
// │  - Borra el directorio completo del problema                │
// │                                                             │
// │ Útil para reestructurar datasets sin basura antigua         │
// └─────────────────────────────────────────────────────────────┘

app.MapDelete("/datasets/{problemId}", (string problemId) =>
{
    // Validar que el problemId no tenga caracteres peligrosos
    if (string.IsNullOrWhiteSpace(problemId) || 
        problemId.Contains("..") || 
        problemId.Contains("/") || 
        problemId.Contains("\\"))
    {
        return Results.BadRequest("Invalid problemId");
    }

    string basePath = Path.Combine(AppContext.BaseDirectory, "Contests", problemId);

    if (!Directory.Exists(basePath))
    {
        return Results.NotFound(new { 
            ok = false, 
            message = $"Problem '{problemId}' does not exist" 
        });
    }

    try
    {
        // 🗑️ Eliminar directorio completo recursivamente
        Directory.Delete(basePath, recursive: true);
        
        return Results.Json(new { 
            ok = true, 
            problemId, 
            message = $"Problem '{problemId}' deleted successfully" 
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { 
            ok = false, 
            problemId,
            error = ex.Message 
        });
    }
});

// ┌─────────────────────────────────────────────────────────────┐
// │ 📚 ENDPOINT AVANZADO: /validate-dataset                     │
// ├─────────────────────────────────────────────────────────────┤
// │ Dual-mode endpoint: SSE streaming + JSON tradicional        │
// │                                                             │
// │ 🔀 MODO 1: SSE (Server-Sent Events)                         │
// │    Accept: text/event-stream                                │
// │    ┌──────┐      ┌──────┐      ┌──────┐                     │
// │    │start │  →   │case  │  →   │complete│                   │
// │    │event │      │events│      │event   │                   │
// │    └──────┘      └──────┘      └────────┘                   │
// │    Envía progreso en tiempo real caso por caso              │
// │                                                             │
// │ 📦 MODO 2: JSON (Retrocompatibilidad)                       │
// │    Accept: application/json                                 │
// │    Ejecuta todos los casos y devuelve array completo        │
// │                                                             │
// │ 📂 Estructura de archivos esperada:                         │
// │    Contests/{problemId}/DataSet/datos001.txt                │
// │    Contests/{problemId}/.Expected/Output_datos001.txt       │
// └─────────────────────────────────────────────────────────────┘
app.MapPost("/validate-dataset", async (
    HttpContext ctx,
    CancellationToken cancellationToken) => // ⬅️ ADD THIS PARAMETER
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync(cancellationToken); // ⬅️ ADD cancellationToken

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var input = JsonSerializer.Deserialize<Request>(body, options);

    if (input == null || string.IsNullOrWhiteSpace(input.Code))
        return Results.BadRequest("Missing code");

    if (string.IsNullOrWhiteSpace(input.Problem))
        return Results.BadRequest("Missing problem name");

    // 🐛 DEBUG: Log del problema recibido
    Console.WriteLine($"🔍 [validate-dataset] Problem: '{input.Problem}' @ {DateTime.Now:HH:mm:ss.fff}");

    // Construimos rutas específicas al problema
    string basePath = Path.Combine(AppContext.BaseDirectory, "Contests", input.Problem);
    string datasetDir = Path.Combine(basePath, "DataSet");
    string expectedDir = Path.Combine(basePath, ".Expected");
    
    // 🐛 DEBUG: Log de rutas construidas
    Console.WriteLine($"📂 DataSet path: {datasetDir}");
    Console.WriteLine($"📂 Expected path: {expectedDir}");

    if (!Directory.Exists(datasetDir))
        return Results.BadRequest($"DataSet not found for problem {input.Problem}");

    if (!Directory.Exists(expectedDir))
        return Results.BadRequest($"Expected not found for problem {input.Problem}");

    var files = Directory.GetFiles(datasetDir, "datos*.txt").OrderBy(f => f).ToList();
    int totalCases = files.Count;

    // 🔍 Detectar si el cliente quiere SSE (streaming)
    bool wantsStreaming = ctx.Request.Headers["Accept"].ToString().Contains("text/event-stream");

    if (wantsStreaming)
    {
        // ✨ MODO STREAMING: Enviar eventos en tiempo real
        ctx.Response.Headers["Content-Type"] = "text/event-stream";
        ctx.Response.Headers["Cache-Control"] = "no-cache";
        ctx.Response.Headers["Connection"] = "keep-alive";

        // ⚠️ NO usar using/dispose para StreamWriter sobre ctx.Response.Body
        var writer = new StreamWriter(ctx.Response.Body, Encoding.UTF8, leaveOpen: true);
        try
        {
            await writer.FlushAsync(cancellationToken);

            // Evento inicial: Total de casos
            await SendSSE(writer, "start", new { totalCases, problem = input.Problem }, cancellationToken);

            int caseIndex = 0;
            foreach (var inputFile in files)
            {
                // ✅ CHECK FOR CANCELLATION
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"🛑 Validation cancelled by client for problem {input.Problem} at case {caseIndex}/{totalCases}");
                    return Results.Empty;
                }

                caseIndex++;
                var name = Path.GetFileName(inputFile);
                var expectedFile = Path.Combine(expectedDir, "Output_" + name);
                string stdin = await File.ReadAllTextAsync(inputFile, cancellationToken);
                string expected = File.Exists(expectedFile)
                    ? await File.ReadAllTextAsync(expectedFile, cancellationToken)
                    : "";

                var (stdout, stderr, timeMs) = await RunSingleCase(input.Code, stdin, input.TimeoutMs);

                string verdict;
                if (!string.IsNullOrEmpty(stderr))
                    verdict = "Error";
                else if (stdout.Trim() == expected.Trim())
                    verdict = "Accepted";
                else if (stdout.Contains("Tiempo límite excedido"))
                    verdict = "Time Limit";
                else
                    verdict = "Wrong Answer";

                // Enviar evento por cada caso procesado
                await SendSSE(writer, "case-result", new
                {
                    caseNumber = caseIndex,
                    totalCases,
                    caseName = name,
                    result = verdict,
                    timeMs,
                    diff = (verdict == "Wrong Answer") ? BuildDiff(expected, stdout) : ""
                }, cancellationToken);
            }

            // Evento final: Completado
            await SendSSE(writer, "complete", new { totalCases, completed = true }, cancellationToken);
            await writer.FlushAsync(cancellationToken);

            Console.WriteLine($"✅ Validation completed successfully for problem {input.Problem}");
            return Results.Empty;
        }
        catch (OperationCanceledException)
        {
            // ✅ CLIENT CANCELLED - CLEAN EXIT
            Console.WriteLine($"✅ Cancellation handled cleanly for problem {input.Problem}");
            return Results.Empty;
        }
        catch (IOException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            // ✅ CLIENT DISCONNECTED
            Console.WriteLine($"🔌 Client disconnected during validation of problem {input.Problem}");
            return Results.Empty;
        }
        catch (Exception ex)
        {
            // ❌ REAL ERROR
            Console.WriteLine($"❌ Error during validation: {ex.Message}");
            throw;
        }
    }
    else
    {
        // 📦 MODO TRADICIONAL: Respuesta JSON completa (retrocompatibilidad)
        var results = new List<object>();

        foreach (var inputFile in files)
        {
            // ✅ CHECK FOR CANCELLATION (también en modo JSON)
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"🛑 JSON validation cancelled by client for problem {input.Problem}");
                return Results.StatusCode(499); // Client Closed Request
            }
            
            var name = Path.GetFileName(inputFile);
            var expectedFile = Path.Combine(expectedDir, "Output_" + name);
            string stdin = await File.ReadAllTextAsync(inputFile, cancellationToken);
            string expected = File.Exists(expectedFile) 
                ? await File.ReadAllTextAsync(expectedFile, cancellationToken) 
                : "";

            var (stdout, stderr, timeMs) = await RunSingleCase(input.Code, stdin, input.TimeoutMs);

            string verdict;
            if (!string.IsNullOrEmpty(stderr))
                verdict = "Error";
            else if (stdout.Trim() == expected.Trim())
                verdict = "Accepted";
            else if (stdout.Contains("Tiempo límite excedido"))
                verdict = "Time Limit";
            else
                verdict = "Wrong Answer";

            results.Add(new
            {
                Case = name,
                Result = verdict,
                TimeMs = timeMs,
                Diff = (verdict == "Wrong Answer") ? BuildDiff(expected, stdout) : ""
            });
        }

        return Results.Json(results);
    }
});

// ✅ UPDATE SendSSE to support CancellationToken
static async Task SendSSE(
    StreamWriter writer, 
    string eventType, 
    object data,
    CancellationToken cancellationToken = default)
{
    var json = JsonSerializer.Serialize(data);
    await writer.WriteLineAsync($"event: {eventType}".AsMemory(), cancellationToken);
    await writer.WriteLineAsync($"data: {json}".AsMemory(), cancellationToken);
    await writer.WriteLineAsync(string.Empty.AsMemory(), cancellationToken); // Empty line = end of event
    await writer.FlushAsync(cancellationToken);
}

// ┌─────────────────────────────────────────────────────────────┐
// │ 📚 FUNCIÓN CORE: RunSingleCase                               │
// ├─────────────────────────────────────────────────────────────┤
// │ Ejecuta un solo caso de prueba con aislamiento completo      │
// │                                                               │
// │ 🔄 CICLO DE VIDA:                                            │
// │                                                               │
// │  1️⃣ Crear kernel →  2️⃣ Configurar eventos → 3️⃣ Set timeout │
// │           ↓                                                   │
// │  4️⃣ Redirect stdin → 5️⃣ Ejecutar código  → 6️⃣ Capturar output│
// │           ↓                                                   │
// │  7️⃣ Restaurar stdin → 8️⃣ Retornar (stdout, stderr, time)    │
// │                                                               │
// │ ⚠️ IMPORTANTE:                                                │
// │  - Cada caso tiene su propio kernel (aislamiento)            │
// │  - stdin se restaura SIEMPRE (finally)                       │
// │  - Timeout previene loops infinitos                          │
// └─────────────────────────────────────────────────────────────┘

static async Task<(string stdout, string stderr, long timeMs)> RunSingleCase(string code, string stdin, int timeoutMs)
{
    // 🔧 Crear un kernel aislado para esta ejecución
    // Cada caso tiene su propio kernel → no comparte variables entre casos
    using var kernel = new CompositeKernel(); // ⚠️ CRITICAL: using para liberar memoria
    using var csharpKernel = new Microsoft.DotNet.Interactive.CSharp.CSharpKernel().UseKernelHelpers();
    kernel.Add(csharpKernel);

    // 📝 StringBuilders para capturar salida y errores
    var sbOut = new StringBuilder();
    var sbErr = new StringBuilder();
    
    // ⏱️ Iniciar cronómetro para medir tiempo de ejecución
    var sw = System.Diagnostics.Stopwatch.StartNew();

    // ⏳ Configurar timeout (por defecto 5 segundos)
    // Previene loops infinitos o código que tarda mucho
    using var cts = new CancellationTokenSource(timeoutMs > 0 ? timeoutMs : 5000);

    // 📡 Suscribirse a eventos del kernel para capturar toda la salida
    kernel.KernelEvents.Subscribe(e =>
    {
        switch (e)
        {
            case StandardOutputValueProduced std:
                // 🖨️ Console.WriteLine() o Console.Write()
                var stdValue = std.FormattedValues.FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(stdValue))
                    sbOut.Append(stdValue); // ⚠️ Usar Append, NO AppendLine (ya tiene \n)
                break;
            case DisplayedValueProduced val:
                // 📊 Valores mostrados automáticamente (ej: última línea sin ;)
                var valValue = val.FormattedValues.FirstOrDefault()?.Value;
                if (!string.IsNullOrEmpty(valValue))
                    sbOut.Append(valValue); // ⚠️ Usar Append, NO AppendLine
                break;
            case CommandFailed fail:
                // ❌ Errores de compilación o runtime
                sbErr.AppendLine(fail.Message);
                break;
        }
    });

    // 💾 Guardar el stdin original para restaurarlo después
    var originalIn = Console.In;
    
    try
    {
        // 🔄 Redirigir stdin: hace que Console.ReadLine() lea de este string
        // El código del estudiante puede usar ReadLine() normalmente
        Console.SetIn(new StringReader(stdin));
        
        // 🚀 Compilar el código (define clases, métodos, etc.)
        await kernel.SendAsync(new SubmitCode(code), cts.Token);
        
        // 🎯 Si el código define Main(), invocarlo usando reflexión
        var hasMain = await InvokeMainIfExists(kernel, code, cts.Token);
        if (hasMain)
        {
            Console.WriteLine("🐛 DEBUG: Main() fue invocado exitosamente");
        }
    }
    catch (OperationCanceledException)
    {
        // ⏰ Se alcanzó el tiempo límite
        sbErr.AppendLine("Tiempo límite excedido");
    }
    catch (Exception ex)
    {
        // 💥 Cualquier otro error (runtime, null reference, etc.)
        sbErr.AppendLine("Error: " + ex.Message);
    }
    finally
    {
        // 🔙 IMPORTANTE: Restaurar stdin original para no contaminar otras ejecuciones
        Console.SetIn(originalIn);
    }

    // 🏁 Detener cronómetro y retornar resultados
    sw.Stop();
    return (sbOut.ToString(), sbErr.ToString(), sw.ElapsedMilliseconds);
}

// ┌─────────────────────────────────────────────────────────────┐
// │ 📚 FUNCIÓN AUXILIAR: InvokeMainIfExists                     │
// ├─────────────────────────────────────────────────────────────┤
// │ Detecta si el código tiene Main() e invócalo usando         │
// │ reflexión para soportar métodos privados/protegidos         │
// │                                                              │
// │ 🔍 FLUJO:                                                    │
// │                                                              │
// │  ┌─────────────────────────────────────────────────┐        │
// │  │ 1️⃣ Detectar firma de Main()                     │        │
// │  │    - static void Main(                          │        │
// │  │    - static async Task Main(                    │        │
// │  │    - static Task Main(                          │        │
// │  └─────────────────────────────────────────────────┘        │
// │                    ↓                                         │
// │  ┌─────────────────────────────────────────────────┐        │
// │  │ 2️⃣ Construir código de reflexión                │        │
// │  │    typeof(Program).GetMethod("Main",            │        │
// │  │        BindingFlags.NonPublic | Public)         │        │
// │  └─────────────────────────────────────────────────┘        │
// │                    ↓                                         │
// │  ┌─────────────────────────────────────────────────┐        │
// │  │ 3️⃣ Invocar Main()                               │        │
// │  │    mainMethod.Invoke(null, args)                │        │
// │  │    if (result is Task) await task               │        │
// │  └─────────────────────────────────────────────────┘        │
// │                                                              │
// │ ⚠️ IMPORTANTE:                                               │
// │  - Asume que la clase se llama "Program"                    │
// │  - Soporta Main público Y privado (por reflexión)           │
// │  - Maneja Main síncrono y asíncrono (Task/async Task)       │
// └─────────────────────────────────────────────────────────────┘

static async Task<bool> InvokeMainIfExists(CompositeKernel kernel, string code, CancellationToken cancellationToken)
{
    // 🔍 Detectar si hay un método Main en el código
    if (!code.Contains("static void Main(") && 
        !code.Contains("static async Task Main(") && 
        !code.Contains("static Task Main("))
    {
        return false; // No hay Main(), no hacer nada
    }

    // 🎯 Construir código de reflexión para invocar Main()
    // Usa BindingFlags.NonPublic para acceder a métodos privados
    string reflectionCode = 
        "var mainMethod = typeof(Program).GetMethod(\"Main\", " +
        "System.Reflection.BindingFlags.Static | " +
        "System.Reflection.BindingFlags.Public | " +
        "System.Reflection.BindingFlags.NonPublic); " +
        "if (mainMethod != null) { " +
        "var result = mainMethod.Invoke(null, mainMethod.GetParameters().Length == 0 ? null : new object[] { new string[0] }); " +
        "if (result is System.Threading.Tasks.Task task) await task; " +
        "}";

    // 🚀 Ejecutar el código de reflexión en el kernel
    await kernel.SendAsync(new SubmitCode(reflectionCode), cancellationToken);
    
    return true; // Main() fue invocado
}

// Pequeña función para marcar diferencias (líneas distintas)
static string BuildDiff(string expected, string actual)
{
    var eLines = expected.Split('\n');
    var aLines = actual.Split('\n');
    var sb = new StringBuilder();

    for (int i = 0; i < Math.Max(eLines.Length, aLines.Length); i++)
    {
        var exp = i < eLines.Length ? eLines[i].TrimEnd() : "";
        var act = i < aLines.Length ? aLines[i].TrimEnd() : "";
        if (exp != act)
            sb.AppendLine($"Línea {i + 1}: Esperado [{exp}] / Obtenido [{act}]");
    }
    return sb.ToString();
}



// ===============================
// 🚀 Arranque del servidor
// ===============================
app.Run("http://localhost:1100");

// ===============================
// 🧩 Record Request
// ===============================
record Request(string Code, string? Stdin = null, int TimeoutMs = 5000, string? Problem = null);

record DatasetUpload(string ProblemId, List<DatasetFile> Files);
record DatasetFile(string Path, string Content);

// ===============================
// 🧩 Extensions
static class Extensions
{
    public static Queue<T> ToQueue<T>(this IEnumerable<T> items) => new(items);
}
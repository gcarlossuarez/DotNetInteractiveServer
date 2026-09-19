using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
public static class EditorAccess
{
    private const int Iterations = 210000;
    private static readonly object Gate = new();
    private static readonly Queue<DateTime> Attempts = new();
    private static string ConfigPath => Environment.GetEnvironmentVariable("JUDGE_TEACHER_CONFIG") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DotNetInteractiveServer", "teacher-key.json");
    public sealed record KeyData(string Salt, string Hash, int Iterations);
    public sealed record UnlockRequest(string Password);
    public static void Map(WebApplication app)
    {
        app.MapPost("/teacher/editor-unlock", (UnlockRequest input, HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var origin = context.Request.Headers.Origin.ToString();
            if (origin.Length > 0 && origin != $"{context.Request.Scheme}://{context.Request.Host}")
                return Results.StatusCode(403);
            lock (Gate)
            {
                while (Attempts.TryPeek(out var first) && DateTime.UtcNow - first > TimeSpan.FromMinutes(1)) Attempts.Dequeue();
                if (Attempts.Count >= 5) return Results.Json(new { message = "Demasiados intentos. Espera un minuto." }, statusCode: 429);
                Attempts.Enqueue(DateTime.UtcNow);
            }
            if (string.IsNullOrEmpty(input.Password) || input.Password.Length > 256) return Results.BadRequest(new { message = "Clave no válida." });
            try
            {
                if (!File.Exists(ConfigPath)) return Results.Json(new { message = "La clave docente aún no está configurada en este equipo." }, statusCode: 503);
                var key = JsonSerializer.Deserialize<KeyData>(File.ReadAllText(ConfigPath));
                if (key == null || key.Iterations < 100000 || key.Iterations > 1000000) throw new InvalidDataException();
                var hash = Rfc2898DeriveBytes.Pbkdf2(input.Password, Convert.FromBase64String(key.Salt), key.Iterations, HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(key.Hash))
                    ? Results.Json(new { enabled = true })
                    : Results.Json(new { message = "Clave incorrecta." }, statusCode: 401);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or FormatException)
            { return Results.Json(new { message = "No se pudo leer la configuración docente." }, statusCode: 503); }
        });
    }
    public static void Configure()
    {
        Console.WriteLine("Configurar clave docente local (mínimo 10 caracteres). No se guarda la clave original.");
        string ReadPassword(string label)
        {
            Console.Write(label); var text = new System.Text.StringBuilder();
            while (true)
            {
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); return text.ToString(); }
                if (k.Key == ConsoleKey.Backspace) { if (text.Length > 0) text.Length--; }
                else if (!char.IsControl(k.KeyChar) && text.Length < 256) text.Append(k.KeyChar);
            }
        }
        var password = ReadPassword("Nueva clave: ");
        if (password.Length < 10 || password != ReadPassword("Repetir clave: "))
            throw new InvalidOperationException("La clave es demasiado corta o no coincide. No se cambió la configuración.");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new KeyData(Convert.ToBase64String(salt), Convert.ToBase64String(hash), Iterations)));
        Console.WriteLine("Clave configurada. Archivo privado del usuario: " + ConfigPath);
    }
}

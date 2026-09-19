if (args.Contains("--configure-teacher-key")) { EditorAccess.Configure(); return; }
var settings = new ConfigurationBuilder().AddCommandLine(args).Build();
var root = Path.GetFullPath(settings["frontend-root"] ?? Directory.GetCurrentDirectory());
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, WebRootPath = Path.Combine(root, "wwwroot") });
var contests = Path.GetFullPath(builder.Configuration["contests"] ?? Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "DotNetInteractiveServer", "Contests"));
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
EditorAccess.Map(app);
FileProblemRunner.Map(app, root, contests);
app.MapGet("/datasets/{problemId}/statement", (string problemId) => ProblemStatement.Read(contests, problemId));
app.MapGet("/datasets/{problemId}/inputs/{fileName}", (string problemId, string fileName) => DatasetInputs.Read(contests, problemId, fileName));
Console.WriteLine("Visor de entradas: " + contests);
app.Run("http://localhost:1101");




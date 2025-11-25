# DotNetInteractiveServer

C# Sandbox server based on Microsoft.DotNet.Interactive for executing and validating C# code dynamically.

## 📋 Requirements for Students

Students **MUST** have **.NET 10 SDK** installed on their machines to run the sandbox.

**Download .NET 10 SDK:**
- https://dotnet.microsoft.com/download/dotnet/10.0

The SDK (not just the runtime) is required because `Microsoft.DotNet.Interactive` needs access to:
- C# compiler (Roslyn)
- Reference assemblies
- NuGet packages resolution

## 🚀 Publishing for Distribution

### ✅ **Recommended: Framework-dependent deployment**

This is the **ONLY way that works** with `Microsoft.DotNet.Interactive`:

```bash
dotnet publish -c Release -o "bin\Release\Deploy"
```

**What's included in the `Deploy` folder:**
- `DotNetInteractiveServer.exe` - Main executable
- `DotNetInteractiveServer.dll` - Application assembly
- `Microsoft.DotNet.Interactive.dll` - Interactive kernel
- `Microsoft.CodeAnalysis.*.dll` - Roslyn compiler libraries
- `appsettings.json` - Configuration files
- Other dependency DLLs

**Requirements on student machines:**
- ✅ .NET 10 SDK installed
- ✅ Windows x64 (or adjust `-r` parameter for other platforms)

**How students run it:**
1. Install .NET 10 SDK
2. Extract the `Deploy` folder
3. Run `DotNetInteractiveServer.exe`
4. Server starts on `http://localhost:1100`

---

### ❌ **Why self-contained deployment DOESN'T work**

```bash
# ⚠️ DO NOT USE - This will fail at runtime!
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

**Why it fails:**

`Microsoft.DotNet.Interactive` is designed for **interactive environments** (like Jupyter notebooks, VS Code notebooks) that require:

1. **SDK reference assemblies** - The interactive kernel needs access to the full .NET SDK reference assemblies to compile user code dynamically. Self-contained deployments only include runtime assemblies, not the compilation infrastructure.

2. **Roslyn workspace dependencies** - `InteractiveWorkspace.TryParseVersion()` tries to locate and parse SDK version information from registry/file system paths that don't exist in self-contained deployments.

3. **NuGet package resolution** - When users use `#r "nuget:PackageName"`, the kernel needs access to NuGet tooling from the SDK.

**Error you'll see with self-contained:**
```
System.NullReferenceException: Object reference not set to an instance of an object.
   at Microsoft.DotNet.Interactive.CSharp.InteractiveWorkspace.TryParseVersion(String versionString, Version& v)
```

**Alternatives if you must avoid SDK dependency:**

1. **Use Roslyn Scripting API** (`Microsoft.CodeAnalysis.CSharp.Scripting`) - Lighter weight but still requires reference assemblies
2. **Compile to temporary assemblies** - Compile user code to DLL and execute in isolated process (complex)
3. **Use external code execution service** - Send code to Judge0, Piston API, or similar (requires internet)

None of these alternatives are as feature-rich as `Microsoft.DotNet.Interactive` for teaching C# interactively.

---

## 🛠️ Development

**Run locally:**
```bash
dotnet run
```

**Build:**
```bash
dotnet build
```

---

## 📡 Endpoints

- `GET /ping` - Health check
- `GET /info` - System information
- `GET /version` - Application version
- `POST /execute` - Execute C# code with optional stdin and configurable timeout (default 5s)
- `POST /reset` - Force garbage collection
- `POST /validate-dataset` - Validate code against test cases (supports SSE streaming)
- `POST /upload-dataset` - Upload problem test data
- `GET /datasets` - List all installed datasets
- `GET /datasets/{problemId}` - Get specific dataset info

---

## 🎯 Features

✅ Execute C# code dynamically with Roslyn compiler
✅ Support for stdin input (Console.ReadLine)
✅ Timeout protection (configurable, default 5 seconds) - prevents infinite loops
✅ Real-time progress with Server-Sent Events (SSE)
✅ Dataset validation with diff output
✅ CORS enabled for web frontends
✅ Error handling and compilation diagnostics
✅ Memory management with garbage collection endpoint

---

## 📦 Dependencies

- `Microsoft.DotNet.Interactive` - Interactive C# kernel
- `Microsoft.DotNet.Interactive.CSharp` - C# languadotge support
- `Microsoft.AspNetCore.App` - Web server framework
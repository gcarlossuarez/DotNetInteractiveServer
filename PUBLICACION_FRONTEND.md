# Publicación del frontend local en GitHub

## Alcance

El cambio incorpora el frontend C#, el visor de casos instalados y su alojamiento con .NET 10 SDK, sin Python. El destino es el repositorio https://github.com/gcarlossuarez/DotNetInteractiveServer, rama main. La publicación en GitHub conserva el código; no despliega ni reinicia el juez que está ejecutándose.

La raíz del espacio migration-to-the-cloud no es un repositorio Git. El repositorio se encuentra en analisis_nube/fuentes/DotNetInteractiveServer. Los comandos siguientes se ejecutan desde esa carpeta. Cuando se ejecutan desde el espacio general, la opción git -C analisis_nube/fuentes/DotNetInteractiveServer dirige cada operación al repositorio correcto.

## Comandos de revisión

```powershell
git status --short --untracked-files=all
```

El comando muestra los archivos modificados y nuevos. Permite comprobar que se incluyen las fuentes y no las compilaciones, datasets o archivos temporales.

```powershell
git remote -v
git branch --show-current
```

El primer comando identifica las direcciones de descarga y publicación de origin. El segundo identifica la rama activa, main.

```powershell
git fetch origin
git rev-list --left-right --count HEAD...origin/main
```

El primer comando actualiza las referencias de GitHub sin modificar los archivos de trabajo. El segundo cuenta commits locales y remotos exclusivos; antes de esta publicación devolvió 0 y 0, por lo que ambas referencias coincidían.

```powershell
git diff --stat
git diff --check
node --check wwwroot/app.js
```

El primer comando resume cambios de archivos ya rastreados. El segundo detecta problemas de espacios en el diff. El tercero comprueba sintaxis JavaScript; Node se utilizó para desarrollo y no es necesario para ejecutar el frontend.

También se ejecutó desde la raíz del espacio:

```powershell
node analisis_nube/verificar_frontend.cjs
```

El comprobador verificó lectura SSE fragmentada, UTF-8, CRLF y respuesta incompleta. Las compilaciones previas del host y la API finalizaron sin errores; la API emitió siete advertencias de nulabilidad. Se comprobó HTTP 200 y lectura exacta de un archivo de entrada. No se realizó una prueba visual automatizada ni la evaluación completa de 52 casos.

## Registro y publicación

```powershell
git add DotNetInteractiveServer.csproj Program.cs DatasetInputs.cs abrir-frontend.cmd tools/FrontendHost/FrontendHost.csproj tools/FrontendHost/NuGet.Config tools/FrontendHost/Program.cs wwwroot/index.html wwwroot/app.js PUBLICACION_FRONTEND.md
git diff --cached --stat
```

El primer comando prepara explícitamente las fuentes, el lanzador y este documento para el commit. El segundo permite revisar el contenido preparado. Los archivos bin/ y obj/ permanecen excluidos por .gitignore.

```powershell
git commit -m "Add offline C# frontend and dataset viewer with .NET host"
git push origin main
```

El commit guarda una versión local identificable. El push publica esa versión en main del repositorio origin utilizando las credenciales autorizadas. No se utiliza force ni se reemplaza el historial remoto.

```powershell
git status --short
git rev-parse HEAD
git ls-remote origin refs/heads/main
```

El primer comando comprueba si quedan cambios sin registrar. Los siguientes permiten comparar el identificador local con el de main en GitHub y confirmar que el commit quedó publicado.

## Ejecución por el alumno

```powershell
.\abrir-frontend.cmd
```

El lanzador compila el host con .NET 10 SDK y sirve el frontend en http://localhost:1101. DotNetInteractive debe permanecer activo en http://localhost:1100. El host auxiliar no necesita Python ni Node. Tener únicamente el runtime no permite compilar este host.

```powershell
.\abrir-frontend.cmd "D:\MiSandbox\Contests"
```

El argumento permite indicar una carpeta de datasets diferente de la ubicación predeterminada en Downloads/DotNetInteractiveServer/Contests. El visor carga el archivo seleccionado en la entrada manual sin modificarlo.

El comando interno de compilación dotnet build prepara el host; el comando dotnet seguido de FrontendHost.dll inicia el proceso. Ctrl+C en la ventana del lanzador detiene ese frontend. Las futuras compilaciones de la API también sirven la página desde el puerto 1100; los borradores guardados en el navegador pertenecen al origen utilizado.

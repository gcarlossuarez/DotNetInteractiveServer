# Guía de uso del juez local C#

## Qué hace esta aplicación

El alumno escribe código C#, prueba entradas y valida casos instalados sin depender de Internet. El resultado es una prueba local, no una entrega oficial al juez central. La interfaz incluye visor de entradas, enunciado Word, referencia C# con búsqueda, guardado de borradores y descarga de fuente.

## Requisitos y arranque

Se requiere .NET 10 SDK, no únicamente el runtime. Python y Node no son necesarios para usar la aplicación. El código del repositorio y la instalación publicada del sandbox pueden estar en carpetas distintas.

1. Se inicia DotNetInteractiveServer.exe desde su instalación. La API escucha en http://localhost:1100.
2. Desde la raíz de este repositorio se ejecuta abrir-frontend.cmd. Compila un host pequeño y lo inicia en http://localhost:1101.
3. Se abre http://localhost:1101 y se mantiene abierta la ventana del lanzador.

El host no inicia ni detiene la API del puerto 1100. Si el puerto 1101 está ocupado, debe cerrarse la instancia anterior antes de abrir otra.

Por defecto, el host busca los problemas en:

```text
%USERPROFILE%\Downloads\DotNetInteractiveServer\Contests
```

Para utilizar otra carpeta, desde una terminal en la raíz del repositorio:

```powershell
.\abrir-frontend.cmd "D:\Sandbox\Contests"
```

Debe indicarse la misma carpeta de problemas que utiliza la API activa, para que el visor y la validación correspondan a los mismos datos.

Las futuras compilaciones de la API también sirven el frontend y sus endpoints desde http://localhost:1100. En esa modalidad no se requiere el host 1101. No se recomienda alternar direcciones durante una evaluación: los borradores del navegador se almacenan por origen y no se comparten entre ambos puertos.

## Uso por el alumno

1. Se selecciona un problema instalado.
2. Se consulta el enunciado y la referencia C#.
3. Se escribe la solución en el editor. Abrir .cs está retirado de la interfaz.
4. Se selecciona un archivo datos*.txt en el visor. Su contenido aparece como entrada manual y puede editarse sin modificar el archivo original.
5. Ejecutar entrada manual realiza una prueba con esa entrada. Validar casos instalados evalúa el conjunto de casos del problema.
6. Descargar .cs guarda una copia del código. Descargar reporte guarda los resultados locales en JSON. Ninguno de estos botones envía una entrega oficial.

El navegador conserva borradores por problema. La descarga de la fuente sigue siendo necesaria como respaldo, especialmente antes de cambiar de navegador, equipo o dirección. Dejar de esperar interrumpe la conexión, pero el motor actual podría seguir ejecutando el caso: debe comprobarse su consola antes de iniciar otro trabajo.

## Enunciado Word y referencia C#

El Word debe estar directamente en Contests/IDENTIFICADOR, no dentro de DataSet. Se admite .docx; un .doc antiguo debe convertirse antes.

- Si existe enunciado.docx, se utiliza ese archivo.
- Si tiene otro nombre y es el único .docx de la carpeta, se utiliza automáticamente.
- Si no hay Word, se conservan las notas guardadas para ese problema.
- Si hay varios Word y ninguno se llama enunciado.docx, aparece un aviso de ambigüedad. Por ejemplo, un enunciado más Referencias.docx exige identificar el principal con el nombre enunciado.docx.

El visor conserva los párrafos, incluidos los saltos dentro de tablas. Es una vista de texto simplificada: imágenes, ecuaciones y formato complejo no se reproducen. El docente debe comparar los ejemplos con el original antes del examen.

Consulta de C# permite buscar palabras como Split, Join, LINQ, arrays, matrices, tuplas, ReadAllLines o Directory.Delete. Los ejemplos están en wwwroot/reference.js y funcionan sin Internet. No se ejecutan al consultarlos.

## Bloqueo del portapapeles

El editor comienza bloqueado en cada carga de la página. No permite copiar, cortar, pegar, soltar texto arrastrado ni iniciar un arrastre desde el editor. Se interceptan también los atajos habituales, incluidos Ctrl/Cmd+C, X y V, Ctrl+Insert, Shift+Insert y Shift+Delete. El menú contextual del editor se bloquea.

El bloqueo se aplica únicamente al editor C#. No bloquea el portapapeles de Windows, otros programas, la entrada manual ni la referencia de consulta. Descargar .cs permanece habilitado para conservar el trabajo.

### Configuración de la clave docente

Antes del examen, el docente cierra el frontend del puerto 1101 y ejecuta, desde la raíz del repositorio:

```powershell
.\configurar-clave-docente.cmd
```

El asistente solicita una clave de al menos diez caracteres y su confirmación. Los caracteres no aparecen en pantalla. Si no coinciden, no se guarda el cambio. Después se vuelve a iniciar abrir-frontend.cmd.

La clave original no se guarda. Se almacena un hash PBKDF2-SHA256 con sal aleatoria y 210000 iteraciones en:

```text
%LOCALAPPDATA%\DotNetInteractiveServer\teacher-key.json
```

El archivo está fuera de wwwroot y del repositorio; no debe publicarse en GitHub. La configuración pertenece al usuario de Windows que ejecuta el servidor. En otro equipo o perfil debe provisionarse la configuración antes del examen. Una clave ya configurada se reemplaza ejecutando nuevamente el asistente. El servidor lee el archivo al verificar cada solicitud, por lo que el cambio de la clave no exige recompilar.

El entorno JUDGE_TEACHER_CONFIG permite definir otra ruta al archivo de configuración, principalmente para instalación administrada y pruebas. Si no existe configuración, el editor permanece bloqueado y el desbloqueo muestra un aviso. No hay clave predeterminada ni clave de pruebas activa.

### Habilitar y volver a bloquear

1. El docente pulsa Portapapeles: bloqueado.
2. Introduce personalmente su clave en el diálogo.
3. La página consulta al servidor local. Si la clave es correcta, habilita el portapapeles solamente en esa página.
4. El mismo botón permite volver a bloquear, sin pedir clave.
5. Una recarga vuelve a bloquear. No se guarda la autorización en localStorage.

El servidor rechaza claves incorrectas y solicitudes de otros orígenes web. Limita las comprobaciones a cinco por minuto por proceso; al superar ese límite se espera un minuto. No registra la clave en logs ni la incluye en JavaScript. El hash no es cifrado reversible: sirve para comprobar la clave, no para recuperarla.

### Dónde está implementado

- wwwroot/index.html: retira Abrir .cs y añade el botón y el diálogo docente.
- wwwroot/editor-access.js: intercepta eventos del editor y conserva la habilitación temporal en memoria.
- EditorAccess.cs: verifica la clave en POST /teacher/editor-unlock y configura su hash.
- configurar-clave-docente.cmd: abre el asistente de configuración.
- Program.cs y tools/FrontendHost/Program.cs: registran el endpoint en la API y en el host auxiliar.

### Alcance real de esta protección

Es una medida disuasoria de la interfaz, no un entorno de examen inviolable. El alumno que pueda modificar JavaScript, usar herramientas de desarrollo, cambiar el archivo de configuración o controlar el sistema operativo puede eludirla. Los archivos de configuración pertenecen al usuario local: no se estableció una separación de permisos docente/alumno en Windows. La contraseña no convierte los resultados locales en calificaciones confiables.

Para restricciones fuertes se necesitan cuentas y políticas administradas del laboratorio, además de aislamiento del código ejecutado. No se implementó un bloqueo de Windows ni supervisión remota. La clave se introduce en la página local y viaja únicamente al servidor de ese equipo, mediante HTTP de loopback.

## Detener y actualizar

Ctrl+C en la ventana de abrir-frontend.cmd detiene el frontend 1101. Cerrar esa ventana también lo detiene; la API 1100 sigue aparte. Si se inició en segundo plano, puede localizarse el proceso por su puerto en PowerShell y comprobarse antes de detenerlo:

```powershell
Get-NetTCPConnection -LocalPort 1101 -State Listen |
    Select-Object -ExpandProperty OwningProcess -Unique
```

Los cambios de HTML y JavaScript requieren recargar la página, preferentemente Ctrl+F5. Los cambios de C# requieren recompilar y reiniciar el proceso correspondiente. No debe reiniciarse una evaluación en curso.

## Comprobaciones antes de utilizarlo en clase

Se prueba el problema real con soluciones correctas e incorrectas, se revisan enunciado y casos, se comprueba guardado/descarga, y se prueba el bloqueo y desbloqueo docente en el navegador que usarán los alumnos. Conviene repetir la prueba sin Internet.

El desarrollo verificó compilación, eventos del bloqueo mediante prueba automatizada y endpoint con clave ficticia correcta/incorrecta y origen externo. La clave ficticia se utilizó en un proceso de prueba separado, ya detenido. No se realizó una prueba visual automatizada completa ni se garantiza equivalencia de todos los programas con el juez oficial.

## Modo de archivos

La configuración execution-settings.json en la raíz del repositorio permite inputMode=file para Skorzenny y Prighozin. Cada caso ejecuta el programa en una carpeta nueva con DataSet y recoge Output/Output_<entrada>. Se utiliza Validar casos instalados. La solución debe definir directoryOut="Output" y usar el nombre del archivo de entrada al generar la salida. Los problemas sin configuración siguen en modo consola. El modo de archivos usa procesos locales, no aislamiento del sistema operativo.

### Flujo cuando DataSet contiene todos los casos

La API se llama **DotNetInteractiveServer**. En la interfaz auxiliar, el endpoint que activa esta variante es `POST /validate-files`; en la aplicación integrada, el mismo flujo queda registrado por `FileProblemRunner`.

```text
Contests/Skorzenny/DataSet/              (fuente original, no se modifica)
  dato0001.txt  dato0002.txt ... dato0052.txt
            |
            | 1. FileProblemRunner enumera dato*.txt y los ordena
            v
      caso 1: carpeta temporal nueva
        DataSet/dato0001.txt              (se copia solo este archivo)
        Output/                            (vacía)
            |
            | 2. Se compila una vez la solución y se ejecuta desde esta carpeta
            | 3. Se espera Output/Output_dato0001.txt
            | 4. Se valida contra .Expected/Output_dato0001.txt
            v
      evento SSE: Accepted / Wrong Answer / Missing Output / Error

      caso 2: otra carpeta temporal nueva
        DataSet/dato0002.txt              (dato0001 no está presente)
        Output/
            |
            v
      ... se repite hasta el último archivo del DataSet

      al finalizar: se eliminan las carpetas temporales
```

El hecho de que el directorio original contenga todos los archivos no significa que el alumno los reciba juntos durante la evaluación. Cada ejecución ve un único caso. Esto evita que una solución lea anticipadamente los demás casos y mantiene independiente el contenido generado en `Output`.

El programa debe usar rutas relativas (`DataSet` y `Output`), nunca una ruta absoluta del docente. Si usa `Directory.GetFiles(@".\DataSet\", "dato*.txt")`, encontrará un solo archivo por ejecución.

El endpoint procesa la cola secuencialmente en esta implementación. La interfaz muestra eventos SSE caso por caso; no indica que todos se ejecuten simultáneamente.

## Modalidad por archivos en lote

La API de esta modalidad es `DotNetInteractiveServer`, con el endpoint `POST /validate-files` del `FrontendHost` (puerto 1101). El flujo es:

```text
Contests/Skorzeny/DataSet/dato*.txt (todos los archivos)
              |
              v
se copian a built/Contests/Skorzeny/DataSet
              |
              v
el programa del estudiante se ejecuta una sola vez
              |
              v
stdout agregado de todos los casos
              |
              v
Contests/Skorzeny/Validator -> Accepted / Wrong Answer
```

Cada archivo `dato*.txt` sigue siendo un caso independiente, pero no se inicia un proceso por archivo. El validador se guarda junto al problema, en `Contests/Skorzeny/Validator`, siguiendo el patrón de los demás problemas.

### Consulta del endpoint de corrección

El método `GET /datasets/{problemId}/validation-route` permite comprobar qué flujo utilizará el frontend. Para un problema con `inputMode: file`, devuelve `/validate-files` y el `validatorPath` configurado. Si `inputMode` no es `file`, devuelve `/validate-dataset`.

### Prueba manual con problemas por archivos

El botón de ejecución manual también funciona para problemas con `inputMode: file`. En ese caso el frontend llama a `POST /execute-files`. El host compila el código, copia todo el `DataSet` a `AppContext.BaseDirectory/Contests/{problema}/DataSet`, ejecuta una vez el programa y devuelve `stdout` al área de salida. Así el estudiante puede probar su lectura de archivos antes de pulsar la corrección.

La corrección usa `POST /validate-files`; la ejecución manual usa `POST /execute-files`. Ambas rutas están en `FrontendHost` y requieren reiniciarlo después de actualizar el código.

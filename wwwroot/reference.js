'use strict';
const csharpReference = [
['Entrada y conversión','Console ReadLine int Parse TryParse leer números',`string linea = Console.ReadLine() ?? "";
if (int.TryParse(linea, out int numero))
    Console.WriteLine(numero);
// Parse lanza una excepción si la entrada no es válida.
long grande = long.Parse("3000000000");`],
['Split: separar una línea','cadenas espacios tokens separar',`string linea = "10  20 30";
string[] partes = linea.Split(' ', StringSplitOptions.RemoveEmptyEntries);
int[] numeros = Array.ConvertAll(partes, int.Parse);
// Para cualquier espacio en blanco (incluye tabuladores):
string[] tokens = linea.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);`],
['Join: unir elementos','cadenas salida string concatenar',`int[] valores = { 3, 7, 9 };
string texto = string.Join(" ", valores);
Console.WriteLine(texto); // 3 7 9
// Join coloca el separador entre elementos, no al final.`],
['Condiciones: if, else y switch','comparar booleanos operadores',`int n = 7;
if (n > 0 && n % 2 == 0)
    Console.WriteLine("Positivo y par");
else if (n > 0)
    Console.WriteLine("Positivo e impar");
else
    Console.WriteLine("No positivo");
// &&: y; ||: o; !: negación; ==: igualdad; !=: diferente.`],
['Bucles: for, foreach, while','iterar recorrer break continue',`int[] datos = { 2, 4, 6 };
for (int i = 0; i < datos.Length; i++)
    Console.WriteLine(datos[i]);
foreach (int valor in datos)
    Console.WriteLine(valor);
int j = 0;
while (j < datos.Length) { Console.WriteLine(datos[j]); j++; }
// break termina el bucle; continue pasa a la siguiente iteración.`],
['List<T>: listas','colecciones agregar eliminar ordenar Count',`using System.Collections.Generic;
var lista = new List<int> { 4, 2, 8 };
lista.Add(6);
lista.Remove(2);  // Elimina la primera coincidencia.
lista.RemoveAt(0); // Elimina por índice.
lista.Sort();
int cantidad = lista.Count;
bool existe = lista.Contains(8);
// No modificar una lista dentro de su foreach.`],
['Dictionary: claves y valores','diccionario frecuencia contar TryGetValue',`using System.Collections.Generic;
var conteos = new Dictionary<string, int>();
string palabra = "hola";
conteos.TryGetValue(palabra, out int anterior);
conteos[palabra] = anterior + 1;
foreach (var par in conteos)
    Console.WriteLine($"{par.Key}: {par.Value}");
// Las claves son únicas. Add falla si la clave ya existe.`],
['HashSet: elementos únicos','repetidos duplicados conjunto',`using System.Collections.Generic;
var vistos = new HashSet<int>();
bool primero = vistos.Add(5); // true
bool repetido = !vistos.Add(5); // true
bool contiene = vistos.Contains(5);
// Add devuelve false cuando el elemento ya estaba presente.`],
['LINQ: Where, Select y OrderBy','filtrar transformar ordenar ThenBy',`using System.Linq;
int[] datos = { 7, 2, 4, 1 };
var resultado = datos.Where(x => x % 2 == 0)
                     .Select(x => x * 10)
                     .OrderByDescending(x => x)
                     .ToList();
// Where filtra; Select transforma; ToList materializa.`],
['LINQ: Any, All, Count, Sum y FirstOrDefault','consultar buscar total vacío',`using System.Linq;
int[] datos = { 2, 3, 5 };
bool alguno = datos.Any(x => x > 4);
bool todos = datos.All(x => x > 0);
int cantidad = datos.Count(x => x % 2 == 1);
long total = datos.Sum(x => (long)x);
int primero = datos.FirstOrDefault(x => x > 10); // 0 si no encuentra.
// Any() sobre vacío: false; All(...) sobre vacío: true.
// Min/Max sobre int vacío lanzan excepción.`],
['LINQ: GroupBy y Distinct','agrupar repetidos duplicados',`using System.Linq;
int[] datos = { 1, 2, 2, 3 };
bool hayRepetidos = datos.GroupBy(x => x).Any(g => g.Count() > 1);
int[] unicos = datos.Distinct().ToArray();
foreach (var grupo in datos.GroupBy(x => x))
    Console.WriteLine($"{grupo.Key}: {grupo.Count()}");`],
['Arrays y matrices','arreglos índices Length bidimensional',`int[] valores = new int[5];
valores[0] = 10;
int[,] matriz = new int[2, 3];
matriz[1, 2] = 7;
int filas = matriz.GetLength(0);
int columnas = matriz.GetLength(1);
// Índices válidos: de 0 a Length - 1.`],
['Cadenas: Trim, Substring, Contains','string texto comparar mayúsculas',`string texto = "  Hola mundo  ";
string limpio = texto.Trim();
bool contiene = limpio.Contains("mundo");
string parte = limpio.Substring(0, 4);
bool igual = string.Equals("HOLA", "hola", StringComparison.OrdinalIgnoreCase);
// Substring exige índices dentro de la cadena.`],
['Métodos y tuplas','funciones return parámetros',`static int Doble(int valor) { return valor * 2; }
static (int menor, int mayor) Ordenar(int a, int b)
{
    return a <= b ? (a, b) : (b, a);
}
// En un programa con clase Program, los métodos van dentro de la clase.
// Ejemplo dentro de Main: var par = Ordenar(8, 3);`],
['Queue y Stack','cola pila FIFO LIFO',`using System.Collections.Generic;
var cola = new Queue<int>();
cola.Enqueue(4);
if (cola.Count > 0) Console.WriteLine(cola.Dequeue());
var pila = new Stack<int>();
pila.Push(8);
if (pila.Count > 0) Console.WriteLine(pila.Pop());
// Peek consulta sin retirar; comprobar Count antes.`],
['División, long y decimales','overflow precisión Math CultureInfo',`int a = 5, b = 2;
int entera = a / b; // 2
double real = (double)a / b; // 2.5
long producto = (long)100000 * 100000;
// Convertir antes de multiplicar evita overflow de int.
double numero = double.Parse("3.14", System.Globalization.CultureInfo.InvariantCulture);
Console.WriteLine(numero.ToString(System.Globalization.CultureInfo.InvariantCulture));`]
];
csharpReference.push(
['Arrays: crear, recorrer, copiar y ordenar','arreglos vectores Array Sort Reverse Copy Clone IndexOf Resize',`int[] numeros = { 8, 3, 5, 1 };
int[] copia = (int[])numeros.Clone(); // Otro array independiente.
Array.Sort(copia);    // 1, 3, 5, 8
Array.Reverse(copia); // 8, 5, 3, 1
int posicion = Array.IndexOf(numeros, 5); // 2; -1 si no existe.
for (int i = 0; i < numeros.Length; i++)
    Console.WriteLine($"Índice {i}: {numeros[i]}");
Array.Resize(ref copia, 6); // Crea otro array; añade ceros.
// int[] alias = numeros; comparte el mismo array, no lo copia.
// Clone es copia superficial si los elementos son objetos.`],
['Matrices: recorrer filas y columnas','arrays bidimensionales matriz GetLength suma diagonal',`int[,] matriz = { { 1, 2, 3 }, { 4, 5, 6 } };
int filas = matriz.GetLength(0);
int columnas = matriz.GetLength(1);
for (int f = 0; f < filas; f++)
{
    int sumaFila = 0;
    for (int c = 0; c < columnas; c++)
    {
        sumaFila += matriz[f, c];
        Console.Write(matriz[f, c] + " ");
    }
    Console.WriteLine($"Suma: {sumaFila}");
}
// Length es el total de celdas: aquí 6.
int sumaDiagonal = 0;
for (int i = 0; i < Math.Min(filas, columnas); i++)
    sumaDiagonal += matriz[i, i];`],
['Arrays escalonados: filas de distinto tamaño','jagged matriz irregular int[][]',`int[][] filas = new int[3][];
filas[0] = new[] { 1, 2 };
filas[1] = new[] { 3, 4, 5 };
filas[2] = new[] { 6 };
for (int f = 0; f < filas.Length; f++)
    for (int c = 0; c < filas[f].Length; c++)
        Console.WriteLine(filas[f][c]);
// Cada fila debe inicializarse antes de usarla.
// int[][] y int[,] son tipos diferentes.`],
['Tuplas: campos, desestructuración y listas','ValueTuple Item1 Item2 ordenar tupla',`using System.Collections.Generic;
using System.Linq;
(string nombre, int puntos) alumno = ("Ana", 18);
Console.WriteLine(alumno.nombre);
var (nombre, puntos) = alumno;
var pares = new List<(int id, int valor)> { (1, 30), (2, 20) };
var ordenados = pares.OrderBy(p => p.valor).ThenBy(p => p.id);
foreach (var (id, valor) in ordenados)
    Console.WriteLine($"{id}: {valor}");
// Sin nombres explícitos se accede con Item1, Item2, etc.`],
['File.ReadAllLines y File.WriteAllLines','archivos leer escribir líneas ReadAllLines WriteAllLines',`using System.IO;
using System.Text;
string carpeta = Path.Combine(Path.GetTempPath(), "PracticaCSharp");
Directory.CreateDirectory(carpeta);
string ruta = Path.Combine(carpeta, "lineas.txt");
string[] originales = { "Primera línea", "Segunda línea" };
File.WriteAllLines(ruta, originales, Encoding.UTF8);
string[] lineas = File.ReadAllLines(ruta, Encoding.UTF8);
for (int i = 0; i < lineas.Length; i++)
    Console.WriteLine($"{i + 1}: {lineas[i]}");
// WriteAllLines crea o sobrescribe el archivo y añade saltos de línea.
// ReadAllLines devuelve un array sin los terminadores de línea.
// Ambas operaciones cargan/escriben todo; no crean carpetas padre.`],
['File.ReadAllText y File.WriteAllText','archivos texto completo ReadAllText WriteAllText',`using System.IO;
using System.Text;
string carpeta = Path.Combine(Path.GetTempPath(), "PracticaCSharp");
Directory.CreateDirectory(carpeta);
string ruta = Path.Combine(carpeta, "texto.txt");
File.WriteAllText(ruta, "Hola" + Environment.NewLine + "Mundo", Encoding.UTF8);
string contenido = File.ReadAllText(ruta, Encoding.UTF8);
Console.WriteLine(contenido);
// ReadAllText devuelve un solo string y conserva los saltos.
// WriteAllText sobrescribe; no añade un salto final automáticamente.
// Leer un archivo inexistente lanza una excepción.`],
['Directory.GetFiles: listar archivos','directorios carpetas patrón búsqueda recursiva GetFiles',`using System.IO;
string carpeta = Path.Combine(Path.GetTempPath(), "PracticaCSharp");
if (Directory.Exists(carpeta))
{
    string[] archivos = Directory.GetFiles(carpeta, "*.txt");
    Array.Sort(archivos, StringComparer.Ordinal);
    foreach (string ruta in archivos)
        Console.WriteLine(Path.GetFileName(ruta));
    // Incluye subcarpetas; requiere permisos para acceder a ellas:
    string[] todos = Directory.GetFiles(carpeta, "*.txt", SearchOption.AllDirectories);
}
// GetFiles no garantiza el orden: ordenar si el ejercicio lo necesita.
// Devuelve rutas; Path.GetFileName obtiene solo el nombre.`],
['Directory.CreateDirectory y Directory.Delete','crear borrar eliminar carpeta DeleteDirectory directorios',`using System.IO;
// Carpeta temporal única, creada exclusivamente para este ejemplo:
string carpeta = Path.Combine(Path.GetTempPath(), "PracticaCSharp_" + Guid.NewGuid());
Directory.CreateDirectory(carpeta);
File.WriteAllText(Path.Combine(carpeta, "ejemplo.txt"), "Prueba");
if (Directory.Exists(carpeta))
    Directory.Delete(carpeta, recursive: true);
// CreateDirectory crea también padres; si ya existe, no la vacía.
// Directory.Delete(carpeta) solo elimina una carpeta vacía.
// recursive: true elimina también archivos y subcarpetas.
// En C# se llama Directory.Delete, no DeleteDirectory.
// Las operaciones de archivos pueden fallar por permisos o bloqueos.`]
);
function renderReference(){
 const normalize=s=>s.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase();
 const words=normalize(document.getElementById('referenceSearch').value).split(/\s+/).filter(Boolean);
 const matches=csharpReference.filter(item=>words.every(w=>normalize(item.join(' ')).includes(w)));
 const container=document.getElementById('referenceResults');container.replaceChildren();
 for(const [title,tags,code] of matches){const details=document.createElement('details');details.className='case';details.open=words.length>0;const summary=document.createElement('summary');summary.textContent=title;const pre=document.createElement('pre');pre.textContent=code;details.append(summary,pre);container.append(details);}
 document.getElementById('referenceCount').textContent=matches.length+' temas encontrados';
}
document.getElementById('referenceSearch').addEventListener('input',renderReference);
renderReference();


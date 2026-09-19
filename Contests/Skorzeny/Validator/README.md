# Validador de Skorzeny

Este directorio pertenece al problema `Contests/Skorzeny`. El servidor lo localiza mediante `validatorPath` en `execution-settings.json`.

El programa del estudiante recibe una copia completa de `Contests/Skorzeny/DataSet` y se ejecuta una sola vez. Cada `dato*.txt` es un caso de prueba; la salida de todos los casos se concatena en la consola. El validador recibe:

1. el directorio original `DataSet`;
2. el directorio `.Expected`;
3. el archivo con la salida agregada de la consola;
4. el `Program.cs` del estudiante.

El validador calcula el valor óptimo para cada archivo y comprueba que la salida agregada tenga ese valor, que los elementos elegidos sean elementos válidos del archivo y que no se exceda la capacidad. Las filas repetidas se consideran elementos distintos cuando sus valores o pesos son distintos. Por eso no se debe renombrar automáticamente `Malmedy` a `Malmedy2`: en Skorzeny hay dos bloques de elementos y esa repetición forma parte de los datos. Si el enunciado exige ciudades únicas, se debe regenerar el caso completo y sus salidas esperadas, no cambiar solamente la etiqueta.

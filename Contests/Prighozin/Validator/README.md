# Validador de Prighozin

El frontend identifica `inputMode: file` y llama a `POST /validate-files`. El `FrontendHost` copia todo `Contests/Prighozin/DataSet`, ejecuta una vez el programa y entrega la salida agregada a este validador.

Para cada archivo, el validador calcula la mochila con costo `EstimatedWagners * 8` para `Rossgvardia` y `EstimatedWagners * 30` para las demás unidades. Comprueba el valor óptimo, la cabecera de salida, los elementos seleccionados y la capacidad. Las filas repetidas se distinguen por sus tres campos.

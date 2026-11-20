# DeliveryRoutes — Archivos Planos y Grafos

Aplicación de consola en **C# / .NET** que modela el mapa urbano como un grafo dirigido y ponderado, calcula distancias y caminos mínimos con el algoritmo de **Dijkstra**, y construye una ruta válida de recogida y entrega para múltiples paquetes minimizando la distancia total recorrida.

## Cómo ejecutar el programa

Desde la carpeta `DeliveryRoutes`:

### Compilar
```bash
dotnet build

### Ejecutar con argumentos (caso completo desde D)
dotnet run -- city.txt packages.txt D

### Ejecutar con argumentos (ejemplo pequeño desde A)
dotnet run -- city_mini.txt packages_mini.txt A

### Ejecutar en modo interactivo (sin argumentos)
dotnet run

* El programa solicitará por consola la ruta del archivo del mapa, la ruta del archivo de paquetes y la ubicación inicial.

## Estrategia de solución y por qué no se prueban todas las combinaciones
¿Por qué no evaluar todas las combinaciones?
Con 30 paquetes hay 60 paradas (30 recogidas y 30 entregas). Sin restricciones, el número de órdenes posibles sería $60!$, y aun exigiendo que cada paquete se recoja antes de entregarse quedan $\frac{60!}{2^{30}}$ permutaciones válidas (un número de más de 70 dígitos). Evaluar todas las opciones de forma exacta en tiempos razonables es inviable, por lo que se trata de una variante del problema de recogida y entrega (Pickup and Delivery Problem) abordada mediante caminos mínimos exactos y heurísticas de optimización.

### estrategia usada:
1. Validación y modelado (CityFile, PackageFile, RouteValidator, Graph<T>): se cargan el mapa y las tareas ignorando líneas vacías y comentarios (#), validando formatos, distancias positivas, sentidos (Twoway / Oneway), ausencia de calles duplicadas (incluyendo inversas como A, B y B, A), existencia de ubicaciones, integridad de cada paquete (un Pickup y un Delivery) y alcanzabilidad dirigida desde el inicio y entre los puntos requeridos.

2. Distancias y caminos mínimos (ShortestPaths): se aplica Dijkstra con PriorityQueue sobre el grafo dirigido para obtener las distancias mínimas y la secuencia exacta de calles entre ubicaciones, respetando sentidos únicos y calles ciegas.

3. Construcción inicial y mejora local (RoutePlanner): se genera una ruta inicial voraz por vecino más cercano válido y rutas por inserción económica sobre órdenes barajados con semilla fija (Random(42) para reproducibilidad). Sobre cada ruta candidata se aplican movimientos de mejora local (reubicación de pares recogida-entrega, reubicación de paradas individuales e intercambios válidos) aceptando únicamente cambios que mantengan la ruta válida (IsValid) y reduzcan la distancia total.

## Salida del ejemplo pequeño (city_mini.txt, packages_mini.txt, inicio A)
Posicion inicial: A
Paquetes: 3 Ubicaciones: 7 Calles: 7 (1 de un solo sentido)
1. Ir de A a E (550 m): recoger paquete 2.
Camino: A -> B -> E
2. Ir de E a C (350 m): recoger paquete 1.
Camino: E -> B -> C
3. Ir de C a D (200 m): entregar paquete 2.
Camino: C -> D
4. Ir de D a G (520 m): entregar paquete 1 y recoger paquete 3.
Camino: D -> F -> G
5. Ir de G a A (820 m): entregar paquete 3.
Camino: G -> F -> E -> B -> A
Todos los paquetes fueron recogidos y entregados.
Recorrido total: 2.44 km

## Salida del caso completo (city.txt, packages.txt, inicio D)
Posicion inicial: D
Paquetes: 30 Ubicaciones: 100 Calles: 142 (30 de un solo sentido)
1. En D: recoger paquete 18.
2. Ir de D a BE (620 m): recoger paquete 7.
Camino: D -> BT -> BE
3. Ir de BE a X (490 m): recoger paquete 12.
Camino: BE -> E -> X
4. Ir de X a AP (540 m): recoger paquete 5.
Camino: X -> B -> AP
5. Ir de AP a AT (350 m): recoger paquete 27.
Camino: AP -> AT
6. Ir de AT a AQ (200 m): recoger paquete 9.
Camino: AT -> AQ
7. Ir de AQ a AX (170 m): recoger paquete 17.
Camino: AQ -> AX
8. Ir de AX a AM (270 m): recoger paquete 30.
Camino: AX -> AK -> AM
9. Ir de AM a BU (350 m): recoger paquetes 8 y 28.
Camino: AM -> BU
10. Ir de BU a CN (430 m): recoger paquetes 1, 2 y 3.
Camino: BU -> CN
11. Ir de CN a BW (2150 m): entregar paquete 30.
Camino: CN -> BU -> AM -> AK -> S -> K -> N -> BW
12. Ir de BW a R (630 m): recoger paquetes 6 y 21.
Camino: BW -> CL -> R
13. Ir de R a CL (360 m): recoger paquete 14.
Camino: R -> CL
14. Ir de CL a BN (450 m): entregar paquete 7.
Camino: CL -> BN
15. Ir de BN a AI (1130 m): recoger paquete 24.
Camino: BN -> Y -> AV -> CG -> AI
16. Ir de AI a AO (170 m): entregar paquete 3 y recoger paquete 11.
Camino: AI -> AO
17. Ir de AO a BI (1460 m): recoger paquete 13.
Camino: AO -> AI -> CG -> BH -> BL -> CO -> BI
18. Ir de BI a Q (1380 m): recoger paquetes 22 y 29.
Camino: BI -> CO -> AZ -> V -> O -> BY -> Q
19. Ir de Q a AA (1450 m): entregar paquete 12.
Camino: Q -> BJ -> CI -> Z -> AA
20. Ir de AA a CP (1030 m): entregar paquete 9.
Camino: AA -> Z -> CA -> BT -> CP
21. Ir de CP a AD (670 m): entregar paquetes 5 y 13.
Camino: CP -> BT -> P -> CB -> AD
22. Ir de AD a AR (280 m): entregar paquete 8 y recoger paquete 20.
Camino: AD -> AR
23. Ir de AR a BB (150 m): recoger paquete 16.
Camino: AR -> BB
24. Ir de BB a AJ (400 m): entregar paquete 29 y recoger paquetes 23 y 26.
Camino: BB -> AJ
25. Ir de AJ a BE (1660 m): entregar paquete 21.
Camino: AJ -> BB -> AR -> AD -> CB -> P -> BT -> BE
26. Ir de BE a T (440 m): entregar paquetes 24 y 27.
Camino: BE -> T
27. Ir de T a CV (820 m): entregar paquete 28.
Camino: T -> CF -> W -> CV
28. Ir de CV a CU (540 m): recoger paquete 15.
Camino: CV -> BX -> CU
29. Ir de CU a M (1060 m): entregar paquete 1.
Camino: CU -> CV -> W -> CM -> M
30. Ir de M a CS (830 m): recoger paquete 19.
Camino: M -> CM -> AE -> CS
31. Ir de CS a AP (770 m): entregar paquetes 11 y 22.
Camino: CS -> AE -> AP
32. Ir de AP a AT (350 m): entregar paquetes 14 y 15.
Camino: AP -> AT
33. Ir de AT a AQ (200 m): entregar paquete 6.
Camino: AT -> AQ
34. Ir de AQ a BV (800 m): entregar paquete 19.
Camino: AQ -> AX -> F -> BV
35. Ir de BV a AB (470 m): recoger paquete 4.
Camino: BV -> AB
36. Ir de AB a BS (1520 m): recoger paquete 25.
Camino: AB -> BV -> F -> AX -> AQ -> BS
37. Ir de BS a CT (100 m): entregar paquete 4.
Camino: BS -> CT
38. Ir de CT a CR (420 m): entregar paquete 20.
Camino: CT -> CR
39. Ir de CR a CH (680 m): recoger paquete 10.
Camino: CR -> BO -> CJ -> CH
40. Ir de CH a AE (1170 m): entregar paquete 26.
Camino: CH -> CJ -> CT -> BS -> CS -> AE
41. Ir de AE a AC (1290 m): entregar paquete 17.
Camino: AE -> L -> E -> X -> AC
42. Ir de AC a CD (130 m): entregar paquete 25.
Camino: AC -> CD
43. Ir de CD a BF (710 m): entregar paquetes 18 y 23.
Camino: CD -> BA -> AG -> BF
44. Ir de BF a BY (620 m): entregar paquete 16.
Camino: BF -> AG -> BA -> BG -> BY
45. Ir de BY a BM (2040 m): entregar paquetes 2 y 10.
Camino: BY -> O -> I -> AF -> H -> AI -> AS -> BM
Todos los paquetes fueron recogidos y entregados.
Recorrido total: 31.75 km
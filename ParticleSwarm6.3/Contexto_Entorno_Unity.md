# Contexto del Entorno - Optimización de Enjambres en Unity

Este documento sirve como contexto y referencia arquitectónica para crear o integrar un **nuevo algoritmo de enjambre (Swarm/Optimization Algorithm)** utilizando el entorno de Unity ya construido y funcional en nuestro proyecto.

## 1. Topología y Arquitectura del Proyecto

El proyecto visualiza el comportamiento de partículas que buscan minimizar la **Función de Rastrigin**. La escena de Unity ya cuenta con un terreno autogenerado y sistemas de instanciación, por lo que el nuevo algoritmo **sólo debe enfocarse en la matemática del movimiento**, apoyándose estrechamente en las herramientas de "Traducción de Coordenadas" que ya están implementadas.

### Archivos/Sistemas Principales Actuales:
1. `RastriginTerrain.cs`: Es el corazón del entorno. Dibuja visualmente un relieve 3D de la función de Rastrigin usando el `Terrain` de Unity.
2. `spawnSwarm.cs`: Instancia (spawnea) 100+ partículas en posiciones aleatorias de la superficie del terreno al iniciar el juego.
3. `swarmAlgorithm.cs` (Actual): Mánager de Enjambre (Singleon) para controlar pasos y configuraciones globales (variables compartidas, visualización UI).
4. `Particle.cs` (Actual): Lógica local de la partícula individual. Evaluaba Rastrigin y se movía usando PSO clásico.

---

## 2. API Disponible: Interactuando con el Terreno (`RastriginTerrain.cs`)

Para crear un nuevo algoritmo correctamente, es vital entender la diferencia entre **Espacio Matemático** y **Espacio de Unity**. 
* **Espacio Matemático (`mathX`, `mathZ`)**: Son los valores reales de la ecuación. El dominio típicamente va de `-3.0` a `7.0` (o `-5.12` a `5.12`), variables definidas en `RastriginTerrain.Instance.domainMin` y `domainMax`. Las fórmulas y la evaluación (fitness) operan estrictamente en estos valores.
* **Espacio Unity (`transform.position`)**: La topología 3D en la pantalla (cientos de unidades a lo ancho/largo y con alturas Y del terreno). 

El script `RastriginTerrain` expone un **Singleton (`RastriginTerrain.Instance`)** con 2 métodos clave que DEBEN usarse:

```csharp
// 1. Convertir de Coordenadas Matemáticas -> a Posición Física en Unity
// Úsalo para mover visualmente a la partícula en Unity en su Update local.
Vector3 unityPos = RastriginTerrain.Instance.MathToUnitySpace(mathX, mathZ);

// 2. Convertir de Posición Física de Unity -> a Coordenadas Matemáticas
// Úsalo AL NACER (Start) para saber en qué punto [X,Z] matemático arrancó la partícula instanciada.
Vector2 mathPos = RastriginTerrain.Instance.UnityToMathSpace(transform.position);
```

---

## 3. Guía de Implementación para el Nuevo Algoritmo

Si vas a programar un nuevo mánager o una nueva partícula (ej. ACO, ABC, Grey Wolf o una variante del PSO), debes apegarte a las siguientes reglas estandarizadas en este entorno para no romper los fotogramas (FPS) del simulador ni chocar con las colisiones del terreno:

### A) Desacoplar la Matemática del Frame Rate (FPS)
La iteración del algoritmo **nunca debe depender del `Time.deltaTime`** para la escala espacial si actualiza el cálculo cada frame, pues la matemática de este tipo de problemas es en "pasos discretos".

```csharp
// Ejemplo de patrón correcto a implementar en la Partícula (dentro de Update):
updateTimer += Time.deltaTime;
float stepTime = 0.05f / manager.simulationSpeed; // Velocidad de simulación controlable por UI

while (updateTimer >= stepTime) {
    updateTimer -= stepTime;
    // 1. Hacer 1 solo avance puramente matemático aquí (Ej. x = x + nuevaVelocidad)
    // 2. Evaluar el fitness matemático
    MathStepYFitness(); 
}

// Fuera del while, interpolar la posición visual fluida:
Vector3 targetVisual = RastriginTerrain.Instance.MathToUnitySpace(mathX, mathZ);
transform.position = Vector3.Lerp(transform.position, targetVisual, Time.deltaTime * 10f);
```

### B) Restricciones Físicas y de Entorno
1. Al actualizar `mathX` y `mathZ`, siempre se debe comprobar si rebasa los límites e invertir la velocidad o atraparla en: `RastriginTerrain.Instance.domainMin` y `RastriginTerrain.Instance.domainMax`.
2. Omitir físicas (RigidBody): Dado que usamos la interpolación ligada al terreno `MathToUnitySpace`, Unity se encarga automáticamente de proveer el eje 'Y' perfecto del valle o la montaña.

### C) Función Objetivo Activa (Función de Rastrigin)
La meta del nuevo algoritmo siempre es minimizar (llegar al 0). La función obligatoria a evaluar es:
```csharp
float fitness = 20f + (mathX * mathX - 10f * Mathf.Cos(2f * Mathf.PI * mathX)) 
                    + (mathZ * mathZ - 10f * Mathf.Cos(2f * Mathf.PI * mathZ));
```

## 4. Reemplazo de Clases
La IA que tome este documento solo debe reescribir/crear dos clases reemplazando su equivalente, sin tocar nada más del proyecto:
1. `NewAlgorithmManager.cs` (Un MonoBehaviour tipo Singleton que guarda configs globales como la redonda, atrayentes, UI de `OnGUI()`, recuentos de iteraciones y `gbest`).
2. `NewParticle.cs` (Atado dinámicamente o por prefab en `spawnSwarm.cs`, responsable de mover su variable local "mathX/mathZ" observando al Manager).

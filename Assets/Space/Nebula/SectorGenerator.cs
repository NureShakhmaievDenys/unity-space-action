using UnityEngine;
using System.Collections.Generic;

public class SectorGenerator : MonoBehaviour
{
    #region Settings Classes
    [System.Serializable]
    public class SectorSkybox
    {
        public Material skybox;
        public Color sectorColor;
    }

    [System.Serializable]
    public class AsteroidPalette
    {
        public string groupName = "Asteroid Group";
        [Tooltip("Список префабов, которые попадут ИМЕННО в эту папку")]
        public GameObject[] prefabs;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    }
    #endregion

    [Header("Seed & Core")]
    public int seed = 12345;
    
    [Header("Skyboxes & Lighting")]
    public SectorSkybox[] skyboxes;
    public Light starLight;

    [Header("Planets (Фон)")]
    public GameObject planetPrefab;
    public int minPlanets = 0;
    public int maxPlanets = 3;
    [Tooltip("Радиус, на котором генерятся планеты")]
    public float sectorRadius = 40000f; 
    
    [Header("Planet Textures")]
    public Texture2D[] aridPlanets;
    public Texture2D[] barrenPlanets;
    public Texture2D[] dustyPlanets;
    public Texture2D[] grassPlanets;
    public Texture2D[] junglePlanets;
    public Texture2D[] marshPlanets;
    public Texture2D[] martianPlanets;
    public Texture2D[] methanePlanets;
    public Texture2D[] sandyPlanets;
    public Texture2D[] snowyPlanets;
    public Texture2D[] tundraPlanets;
    public Texture2D[] gasGiantPlanets;

    [Header("Asteroids: Optimization (Атлас)")]
    [Tooltip("Материал с галочкой GPU Instancing и твоей текстурой 4x2")]
    public Material masterAsteroidMaterial;
    public int atlasColumns = 4;
    public int atlasRows = 2;
    [Tooltip("Для URP оставь _BaseMap_ST. Для Standard шейдера напиши _MainTex_ST")]
    public string shaderUVPropertyName = "_BaseMap_ST";

    [Header("Asteroids: Eye of the Storm")]
    [Tooltip("Радиус поля астероидов (зона полета)")]
    public float asteroidFieldRadius = 10000f;
    [Tooltip("Жесткий общий лимит астероидов")]
    public int maxAsteroids = 1000;
    [Tooltip("Общее кол-во попыток спавна. Будет разделено между группами.")]
    public int spawnAttempts = 3000;
    [Tooltip("Зона в центре (0,0,0) со 100% плотностью")]
    public float solidCoreRadius = 500f;
    [Tooltip("Кривая падения плотности от ядра к краю")]
    public AnimationCurve densityDropoffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Asteroid Prefabs")]
    public AsteroidPalette[] asteroidPalettes;

    // Служебные переменные
    private int currentAsteroidsCount = 0;
    private Transform planetsContainer;
    private Transform asteroidsContainer;
    private MaterialPropertyBlock propertyBlock;
    private Dictionary<string, Transform> groupContainers = new Dictionary<string, Transform>();

    void Start()
    {
        GenerateSector();
    }

    [ContextMenu("Generate New Sector")]
    public void GenerateSector()
    {
        ClearSector();
        Random.InitState(seed);
        propertyBlock = new MaterialPropertyBlock();
        CreateContainers();

        if (skyboxes != null && skyboxes.Length > 0)
        {
            SectorSkybox sector = skyboxes[Random.Range(0, skyboxes.Length)];
            RenderSettings.skybox = sector.skybox;
            
            if (starLight != null) 
            {
                starLight.color = sector.sectorColor;
                
                // ======= ДОБАВЛЕННАЯ СТРОКА =======
                // Крутим невидимое "Солнце", чтобы свет падал с новой случайной стороны
                // X (высота) от 10 до 80 градусов, Y (поворот) любые 360 градусов
                starLight.transform.rotation = Quaternion.Euler(Random.Range(10f, 80f), Random.Range(0f, 360f), 0f);
            }
            
            GeneratePlanets(sector.sectorColor);
        }

        // ======= 3. ГЕНЕРАЦИЯ АСТЕРОИДОВ (НОВАЯ ЛОГИКА - ГАРАНТИРОВАННАЯ) =======
        
        if (asteroidPalettes != null && asteroidPalettes.Length > 0 && masterAsteroidMaterial != null)
        {
            // Сколько ПОПЫТОК спавна даем каждой группе (например 1000)
            int attemptsPerGroup = spawnAttempts / asteroidPalettes.Length;

            // ЦИКЛ ПО КАЖДОЙ ГРУППЕ В ИНСПЕКТОРЕ
            foreach (var group in asteroidPalettes)
            {
                if (group.prefabs == null || group.prefabs.Length == 0) continue;

                // Для каждой группы выбираем свои 2-3 случайные модели
                List<GameObject> groupModelsForSector = PickLimitedModelsFromSpecificList(group.prefabs, 3);

                // Ищем папку-родителя для этой группы
                if (groupContainers.TryGetValue(group.groupName, out Transform parentFolder))
                {
                    // Генерируем астероиды ТОЛЬКО этой группы ИМЕННО в эту папку
                    GenerateEyeOfTheStormForGroup(groupModelsForSector, parentFolder, group.scaleRange, attemptsPerGroup);
                }
            }

            Debug.Log($"<color=cyan>Сектор сгенерирован!</color> Итого астероидов: {currentAsteroidsCount}.");

            // ======= ПОКА ОСТАВИМ ТАК (ты удалил каллинг, но на будущее задел) =======
            // Мы просто ищем скрипт на сцене, если не найдем - ничего страшного, Unity не вылетит.
          
            // =========================================================================
        }
        else
        {
            Debug.LogError("Назначь Master Asteroid Material и Asteroid Palettes!");
        }
    }

    void ClearSector()
    {
        currentAsteroidsCount = 0;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        groupContainers.Clear();
    }

    void CreateContainers()
    {
        planetsContainer = new GameObject("--- PLANETS ---").transform;
        planetsContainer.SetParent(this.transform);

        asteroidsContainer = new GameObject("--- ASTEROIDS ---").transform;
        asteroidsContainer.SetParent(this.transform);

        groupContainers.Clear();
        if (asteroidPalettes != null)
        {
            foreach (var group in asteroidPalettes)
            {
                if (!string.IsNullOrEmpty(group.groupName) && !groupContainers.ContainsKey(group.groupName))
                {
                    GameObject groupFolder = new GameObject(group.groupName);
                    groupFolder.transform.SetParent(asteroidsContainer);
                    groupContainers.Add(group.groupName, groupFolder.transform);
                }
            }
        }
    }

    // ===================== АСТЕРОИДЫ (НОВАЯ ЛОГИКА) =====================

    // Метод генерирует subset астероидов специально для одной группы
    void GenerateEyeOfTheStormForGroup(List<GameObject> modelsForGroup, Transform parent, Vector2 scaleRange, int attempts)
    {
        float fadeRange = asteroidFieldRadius - solidCoreRadius;

        for (int i = 0; i < attempts; i++)
        {
            if (currentAsteroidsCount >= maxAsteroids) break; // Общий лимит не превышаем

            Vector3 randomPos = Random.insideUnitSphere * asteroidFieldRadius;
            float distFromCenter = randomPos.magnitude;
            float spawnChance = 0f;

            if (distFromCenter <= solidCoreRadius) spawnChance = 1f; 
            else
            {
                float normalizedDist = (distFromCenter - solidCoreRadius) / fadeRange;
                spawnChance = densityDropoffCurve.Evaluate(normalizedDist);
            }

            if (Random.value < spawnChance)
            {
                // Выбираем модель ТОЛЬКО из моделей ЭТОЙ группы
                GameObject proto = modelsForGroup[Random.Range(0, modelsForGroup.Count)];
                
                // Передаем родителя и scale напрямую в метод спавна
                SpawnOptimizedAsteroid(randomPos, proto, parent, scaleRange);
            }
        }
    }

    // Метод спавна упрощен. Он не ищет родителей, он их принимает.
    void SpawnOptimizedAsteroid(Vector3 pos, GameObject proto, Transform parent, Vector2 scaleRange)
    {
        GameObject asteroid = Instantiate(proto, pos, Random.rotation, parent);
        float scale = Random.Range(scaleRange.x, scaleRange.y);
        asteroid.transform.localScale = Vector3.one * scale;

        // --- МАГИЯ АТЛАСА ---
        Renderer r = asteroid.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            int totalTextures = atlasColumns * atlasRows;
            int randomTexIndex = Random.Range(0, totalTextures);

            float scaleX = 1f / atlasColumns;
            float scaleY = 1f / atlasRows;

            int col = randomTexIndex % atlasColumns;
            int row = randomTexIndex / atlasColumns;
            int invertedRow = (atlasRows - 1) - row; 

            float offsetX = col * scaleX;
            float offsetY = invertedRow * scaleY;

            Vector4 uvTransform = new Vector4(scaleX, scaleY, offsetX, offsetY);

            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(shaderUVPropertyName, uvTransform);
            r.SetPropertyBlock(propertyBlock);
        }

        currentAsteroidsCount++;
    }

    // Выбирает limited subset моделей ИЗ КОНКРЕТНОГО СПИСКА (для группы)
    List<GameObject> PickLimitedModelsFromSpecificList(GameObject[] availablePrefabs, int count)
    {
        List<GameObject> tempPool = new List<GameObject>(availablePrefabs);
        List<GameObject> result = new List<GameObject>();

        if (tempPool.Count == 0) return result;

        int finalCount = Mathf.Min(count, tempPool.Count);
        for (int i = 0; i < finalCount; i++)
        {
            int index = Random.Range(0, tempPool.Count);
            result.Add(tempPool[index]);
            tempPool.RemoveAt(index);
        }
        return result;
    }

    // ===================== ПЛАНЕТЫ (Без изменений) =====================

    void GeneratePlanets(Color sectorColor)
    {
        if (planetPrefab == null) return;
        int planetCount = Random.Range(minPlanets, maxPlanets + 1);
        List<Vector3> planetPositions = new List<Vector3>();
        for (int i = 0; i < planetCount; i++)
        {
            Vector3 position;
            int attempts = 0;
            do { position = Random.onUnitSphere * sectorRadius; attempts++;
            } while (IsTooClose(position, planetPositions, 20000f) && attempts < 50);
            planetPositions.Add(position);
            GameObject planet = Instantiate(planetPrefab, position, Quaternion.identity, planetsContainer);
            planet.name = $"Planet_{i}"; SetupPlanet(planet, sectorColor);
        }
    }

    void SetupPlanet(GameObject planet, Color sectorColor)
    {if (planet.GetComponent<PlanetRotation>() == null)
        {
            planet.AddComponent<PlanetRotation>();
        }
        
        Renderer renderer = planet.GetComponentInChildren<Renderer>();
        if (renderer == null) return;
        int type = Random.Range(0, 12); Texture2D texture = GetPlanetTexture(type);
        MaterialPropertyBlock planetBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(planetBlock);
        if (texture != null) planetBlock.SetTexture("_BaseMap", texture);
        renderer.SetPropertyBlock(planetBlock);
        float size = (type == 11) ? Random.Range(15000f, 25000f) : Random.Range(5000f, 12000f);
        planet.transform.localScale = Vector3.one * size;
        planet.transform.rotation = Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(0f, 360f), 0f);
        Transform atmosphere = planet.transform.Find("PlanetAtmosphere");
        if (atmosphere != null) {
            Renderer atmRenderer = atmosphere.GetComponent<Renderer>();
            if (atmRenderer != null) {
                MaterialPropertyBlock atmBlock = new MaterialPropertyBlock();
                atmRenderer.GetPropertyBlock(atmBlock);
                atmBlock.SetColor("_BaseColor", sectorColor);
                atmRenderer.SetPropertyBlock(atmBlock);
            }
        }
    }

    bool IsTooClose(Vector3 pos, List<Vector3> existing, float minDistance)
    {
        foreach (Vector3 p in existing) if (Vector3.Distance(pos, p) < minDistance) return true;
        return false;
    }

    Texture2D GetPlanetTexture(int type)
    {
        return type switch {
            0 => GetRandom(aridPlanets), 1 => GetRandom(barrenPlanets), 2 => GetRandom(dustyPlanets),
            3 => GetRandom(grassPlanets), 4 => GetRandom(junglePlanets), 5 => GetRandom(marshPlanets),
            6 => GetRandom(martianPlanets), 7 => GetRandom(methanePlanets), 8 => GetRandom(sandyPlanets),
            9 => GetRandom(snowyPlanets), 10 => GetRandom(tundraPlanets), 11 => GetRandom(gasGiantPlanets), _ => null
        };
    }

    T GetRandom<T>(T[] array) => (array == null || array.Length == 0) ? default : array[Random.Range(0, array.Length)];
}
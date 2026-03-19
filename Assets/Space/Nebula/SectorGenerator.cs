using UnityEngine;
using System.Collections.Generic;

public class SectorGenerator : MonoBehaviour
{
    #region Settings Classes
    [System.Serializable]
    public class ClusterSettings
    {
        public int minClusters = 3;
        public int maxClusters = 6;
        public int minAsteroids = 20;
        public int maxAsteroids = 50;
        public float clusterRadius = 400f; // Слегка увеличил для лучшего распределения в центре
    }

    [System.Serializable]
    public class SuperClusterSettings
    {
        public int count = 1;
        public int minAsteroids = 80;
        public int maxAsteroids = 150;
        public float radius = 800f;
    }

    [System.Serializable]
    public class SectorSkybox
    {
        public Material skybox;
        public Color sectorColor;
    }

    [System.Serializable]
    public class AsteroidGroup
    {
        public GameObject[] prefabs;
        public float spawnChance;
        public Vector2 scaleRange;
    }
    #endregion

    [Header("Seed & Core")]
    public int seed = 12345;
    public float fieldRadius = 2400f;

    [Header("Skyboxes & Lighting")]
    public SectorSkybox[] skyboxes;
    public Light starLight;

    [Header("Planets")]
    public GameObject planetPrefab;
    public int minPlanets = 0;
    public int maxPlanets = 3;
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

    [Header("Asteroids General")]
    public int maxAsteroids = 500;
    public Texture2D[] asteroidTextures;
    public AsteroidGroup[] asteroidGroups;

    [Header("Asteroid Structures")]
    public ClusterSettings clusters;
    public SuperClusterSettings superClusters;

    // Служебные переменные
    private int currentAsteroids = 0;
    private Transform planetsContainer;
    private Transform asteroidsContainer;

    void Start()
    {
        GenerateSector();
    }

    [ContextMenu("Generate New Sector")]
    public void GenerateSector()
    {
        ClearSector();
        Random.InitState(seed);
        CreateContainers();

        if (skyboxes != null && skyboxes.Length > 0)
        {
            SectorSkybox sector = skyboxes[Random.Range(0, skyboxes.Length)];
            RenderSettings.skybox = sector.skybox;
            if (starLight != null) starLight.color = sector.sectorColor;
            
            GeneratePlanets(sector.sectorColor);
            GenerateAsteroids();
        }
        else
        {
            Debug.LogError("Skyboxes not assigned!");
        }
    }

    void ClearSector()
    {
        currentAsteroids = 0;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    void CreateContainers()
    {
        planetsContainer = new GameObject("--- PLANETS ---").transform;
        planetsContainer.SetParent(this.transform);

        asteroidsContainer = new GameObject("--- ASTEROIDS ---").transform;
        asteroidsContainer.SetParent(this.transform);
    }

    // ===================== ПЛАНЕТЫ =====================

    void GeneratePlanets(Color sectorColor)
    {
        if (planetPrefab == null) return;

        int planetCount = Random.Range(minPlanets, maxPlanets + 1);
        List<Vector3> planetPositions = new List<Vector3>();

        for (int i = 0; i < planetCount; i++)
        {
            Vector3 position;
            int attempts = 0;
            do {
                position = Random.onUnitSphere * sectorRadius;
                attempts++;
            } while (IsTooClose(position, planetPositions, 20000f) && attempts < 50);

            planetPositions.Add(position);
            GameObject planet = Instantiate(planetPrefab, position, Quaternion.identity, planetsContainer);
            planet.name = $"Planet_{i}";
            SetupPlanet(planet, sectorColor);
        }
    }

    void SetupPlanet(GameObject planet, Color sectorColor)
    {
        Renderer renderer = planet.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        int type = Random.Range(0, 12);
        Texture2D texture = GetPlanetTexture(type);
        
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        if (texture != null) propBlock.SetTexture("_BaseMap", texture);
        renderer.SetPropertyBlock(propBlock);

        float size = (type == 11) ? Random.Range(15000f, 25000f) : Random.Range(5000f, 12000f);
        planet.transform.localScale = Vector3.one * size;
        planet.transform.rotation = Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(0f, 360f), 0f);

        Transform atmosphere = planet.transform.Find("PlanetAtmosphere");
        if (atmosphere != null)
        {
            Renderer atmRenderer = atmosphere.GetComponent<Renderer>();
            if (atmRenderer != null) 
            {
                MaterialPropertyBlock atmBlock = new MaterialPropertyBlock();
                atmRenderer.GetPropertyBlock(atmBlock);
                atmBlock.SetColor("_BaseColor", sectorColor);
                atmRenderer.SetPropertyBlock(atmBlock);
            }
        }
    }

    // ===================== АСТЕРОИДЫ =====================

    void GenerateAsteroids()
    {
        if (asteroidGroups == null || asteroidGroups.Length == 0) return;

        Transform clustersParent = new GameObject("Clusters").transform;
        clustersParent.SetParent(asteroidsContainer);

        Transform superParent = new GameObject("Super Clusters").transform;
        superParent.SetParent(asteroidsContainer);

        GenerateClusters(clustersParent);
        GenerateSuperClusters(superParent);
    }

    void GenerateClusters(Transform parent)
    {
        int count = Random.Range(clusters.minClusters, clusters.maxClusters);
        for (int c = 0; c < count; c++)
        {
            Transform clusterFolder = new GameObject($"Cluster_{c}").transform;
            clusterFolder.SetParent(parent);

            // Центр кластера немного смещен от 0,0,0 чтобы они не слипались в одну точку
            Vector3 center = Random.insideUnitSphere * clusters.clusterRadius;
            int amount = Random.Range(clusters.minAsteroids, clusters.maxAsteroids);

            for (int i = 0; i < amount; i++)
            {
                Vector3 pos = center + Random.insideUnitSphere * clusters.clusterRadius;
                if (ValidPosition(pos) && PassNoise(pos)) SpawnAsteroid(pos, clusterFolder);
            }
        }
    }

    void GenerateSuperClusters(Transform parent)
    {
        for (int c = 0; c < superClusters.count; c++)
        {
            Transform folder = new GameObject($"SuperCluster_{c}").transform;
            folder.SetParent(parent);

            // Суперкластер всегда в самом центре
            Vector3 center = Vector3.zero;
            int amount = Random.Range(superClusters.minAsteroids, superClusters.maxAsteroids);

            for (int i = 0; i < amount; i++)
            {
                Vector3 pos = center + Random.insideUnitSphere * superClusters.radius;
                if (ValidPosition(pos) && PassNoise(pos)) SpawnAsteroid(pos, folder);
            }
        }
    }

    void SpawnAsteroid(Vector3 pos, Transform parent)
    {
        if (currentAsteroids >= maxAsteroids) return;

        var asteroidData = GetRandomAsteroidData();
        if (asteroidData.prefab == null) return;

        GameObject asteroid = Instantiate(asteroidData.prefab, pos, Random.rotation, parent);
        asteroid.transform.localScale = Vector3.one * asteroidData.scale;

        Renderer r = asteroid.GetComponentInChildren<Renderer>();
        if (r != null && asteroidTextures.Length > 0)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetTexture("_BaseMap", asteroidTextures[Random.Range(0, asteroidTextures.Length)]);
            r.SetPropertyBlock(block);
        }

        currentAsteroids++;
    }

    // ===================== ВСПОМОГАТЕЛЬНОЕ =====================

    bool ValidPosition(Vector3 pos) => pos.magnitude <= fieldRadius * 1.5f && currentAsteroids < maxAsteroids;

    bool PassNoise(Vector3 pos)
    {
        // НОВОВВЕДЕНИЕ: Если астероид в радиусе 300 от центра, он появляется со 100% шансом (игнорирует пустоты шума)
        if (pos.magnitude < 300f) return true;

        float scale = 0.002f;
        float offset = 100000f; 
        float noise = Mathf.PerlinNoise((pos.x + offset) * scale + seed, (pos.z + offset) * scale + seed);
        return noise > 0.4f;
    }

    bool IsTooClose(Vector3 pos, List<Vector3> existing, float minDistance)
    {
        foreach (Vector3 p in existing) if (Vector3.Distance(pos, p) < minDistance) return true;
        return false;
    }

    (GameObject prefab, float scale) GetRandomAsteroidData()
    {
        float total = 0f;
        foreach (var g in asteroidGroups) total += g.spawnChance;
        
        float rand = Random.Range(0, total);
        foreach (var g in asteroidGroups)
        {
            if (rand < g.spawnChance) 
            {
                GameObject prefab = GetRandom(g.prefabs);
                float scale = Random.Range(g.scaleRange.x, g.scaleRange.y);
                return (prefab, scale);
            }
            rand -= g.spawnChance;
        }
        return (null, 100f);
    }

    Texture2D GetPlanetTexture(int type)
    {
        return type switch {
            0 => GetRandom(aridPlanets), 1 => GetRandom(barrenPlanets), 2 => GetRandom(dustyPlanets),
            3 => GetRandom(grassPlanets), 4 => GetRandom(junglePlanets), 5 => GetRandom(marshPlanets),
            6 => GetRandom(martianPlanets), 7 => GetRandom(methanePlanets), 8 => GetRandom(sandyPlanets),
            9 => GetRandom(snowyPlanets), 10 => GetRandom(tundraPlanets), 11 => GetRandom(gasGiantPlanets),
            _ => null
        };
    }

    T GetRandom<T>(T[] array) => (array == null || array.Length == 0) ? default : array[Random.Range(0, array.Length)];
}
using UnityEngine;
using System.Collections.Generic;

public class SectorGenerator : MonoBehaviour
{
    [System.Serializable]
    public class SectorSkybox
    {
        public Material skybox;
        public Color sectorColor;
    }

    [Header("Seed")]
    public int seed = 12345;

    [Header("Skyboxes")]
    public SectorSkybox[] skyboxes;

    [Header("Planet Prefab")]
    public GameObject planetPrefab;

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

    [Header("Lighting")]
    public Light starLight;

    [Header("Planet Settings")]
    public int minPlanets = 0;
    public int maxPlanets = 3;
    public float sectorRadius = 40000f;

    void Start()
    {
        GenerateSector();
    }

    void GenerateSector()
    {
        Random.InitState(seed);

        if (skyboxes == null || skyboxes.Length == 0)
        {
            Debug.LogWarning("No skyboxes assigned!");
            return;
        }

        SectorSkybox sector = skyboxes[Random.Range(0, skyboxes.Length)];

        RenderSettings.skybox = sector.skybox;

        if (starLight != null)
            starLight.color = sector.sectorColor;

        GeneratePlanets(sector.sectorColor);
    }

    void GeneratePlanets(Color sectorColor)
    {
        if (planetPrefab == null)
        {
            Debug.LogWarning("Planet prefab missing!");
            return;
        }

        int planetCount = Random.Range(minPlanets, maxPlanets + 1);

        List<Vector3> planetPositions = new List<Vector3>();

        float minDistance = 30000f;

        for (int i = 0; i < planetCount; i++)
        {
            Vector3 position;
            int attempts = 0;

            do
            {
                position = Random.onUnitSphere * sectorRadius;
                attempts++;

            } while (IsTooClose(position, planetPositions, minDistance) && attempts < 30);

            planetPositions.Add(position);

            GameObject planet = Instantiate(
                planetPrefab,
                position,
                Quaternion.identity
            );

            SetupPlanet(planet, sectorColor);
        }
    }

    bool IsTooClose(Vector3 pos, List<Vector3> existing, float minDistance)
    {
        foreach (Vector3 p in existing)
        {
            if (Vector3.Distance(pos, p) < minDistance)
                return true;
        }

        return false;
    }

    void SetupPlanet(GameObject planet, Color sectorColor)
    {
        Renderer renderer = planet.GetComponentInChildren<Renderer>();

        if (renderer == null)
            return;

        int type = Random.Range(0, 12);

        Texture2D texture = GetPlanetTexture(type);

        Material mat = new Material(renderer.sharedMaterial);

        if (texture != null)
            mat.SetTexture("_BaseMap", texture);

        renderer.material = mat;

        float size = Random.Range(5000f, 12000f);

        if (type == 11)
            size = Random.Range(15000f, 25000f);

        planet.transform.localScale = Vector3.one * size;

        float tilt = Random.Range(-35f, 35f);

        planet.transform.rotation =
            Quaternion.Euler(tilt, Random.Range(0f, 360f), 0f);

        PlanetRotation rotation = planet.AddComponent<PlanetRotation>();

        rotation.rotationSpeed = Random.Range(1f, 5f);

        if (type == 11)
            rotation.rotationSpeed = Random.Range(4f, 10f);

        Transform atmosphere = planet.transform.Find("PlanetAtmosphere");

        if (atmosphere != null)
        {
            Renderer atmRenderer = atmosphere.GetComponent<Renderer>();

            if (atmRenderer != null)
                atmRenderer.material.SetColor("_BaseColor", sectorColor);
        }
    }

    Texture2D GetPlanetTexture(int type)
    {
        switch (type)
        {
            case 0: return GetRandom(aridPlanets);
            case 1: return GetRandom(barrenPlanets);
            case 2: return GetRandom(dustyPlanets);
            case 3: return GetRandom(grassPlanets);
            case 4: return GetRandom(junglePlanets);
            case 5: return GetRandom(marshPlanets);
            case 6: return GetRandom(martianPlanets);
            case 7: return GetRandom(methanePlanets);
            case 8: return GetRandom(sandyPlanets);
            case 9: return GetRandom(snowyPlanets);
            case 10: return GetRandom(tundraPlanets);
            case 11: return GetRandom(gasGiantPlanets);
        }

        return null;
    }

    Texture2D GetRandom(Texture2D[] textures)
    {
        if (textures == null || textures.Length == 0)
            return null;

        return textures[Random.Range(0, textures.Length)];
    }
}
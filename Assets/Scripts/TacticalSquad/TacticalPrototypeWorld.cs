using System.Collections.Generic;
using UnityEngine;

namespace Sipoga.Tactics
{
    public sealed class TacticalPrototypeWorld : MonoBehaviour
    {
        private sealed class SurfaceVisual
        {
            public GameObject SolidPanel;
            public readonly List<GameObject> Slats = new List<GameObject>();
        }

        private readonly Dictionary<string, SurfaceVisual> _surfaceVisuals =
            new Dictionary<string, SurfaceVisual>();

        public static TacticalPrototypeWorld Build(TacticalScenarioDefinition scenario)
        {
            GameObject root = new GameObject("Glasshouse Tactical Environment");
            TacticalPrototypeWorld world = root.AddComponent<TacticalPrototypeWorld>();
            world.BuildEnvironment(scenario);
            return world;
        }

        public void UpdateSurface(string surfaceId, TacticalSurfaceState state)
        {
            SurfaceVisual visual;
            if (!_surfaceVisuals.TryGetValue(surfaceId, out visual))
            {
                return;
            }

            bool showPanel = state == TacticalSurfaceState.Sealed;
            bool showSlats = state == TacticalSurfaceState.Permeable;
            visual.SolidPanel.SetActive(showPanel);
            for (int i = 0; i < visual.Slats.Count; i++)
            {
                visual.Slats[i].SetActive(showSlats);
            }
        }

        private void BuildEnvironment(TacticalScenarioDefinition scenario)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.27f);
            RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.03f, 0.04f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.055f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 60f;

            Color slab = new Color(0.055f, 0.065f, 0.078f);
            Color roomA = new Color(0.11f, 0.14f, 0.16f);
            Color roomB = new Color(0.13f, 0.12f, 0.15f);
            Color roomC = new Color(0.09f, 0.15f, 0.15f);
            Color wall = new Color(0.20f, 0.23f, 0.26f);
            Color wallEdge = new Color(0.34f, 0.37f, 0.40f);
            Color platform = new Color(0.14f, 0.16f, 0.19f);
            Color railing = new Color(0.38f, 0.42f, 0.46f);
            Color cover = new Color(0.24f, 0.27f, 0.30f);

            TacticalPrototypeVisual.CreateBlock(
                "Foundation",
                new Vector3(0f, -0.2f, 0.7f),
                new Vector3(21.5f, 0.35f, 15.8f),
                slab,
                transform,
                true);

            CreateRoomFloor("Archive floor", new Vector3(-5.4f, 0.0f, -3.3f), new Vector3(8.0f, 0.10f, 5.8f), roomA);
            CreateRoomFloor("Packing floor", new Vector3(3.4f, 0.0f, -3.3f), new Vector3(8.0f, 0.10f, 5.8f), roomB);
            CreateRoomFloor("Service floor", new Vector3(-0.3f, 0.01f, 2.6f), new Vector3(5.1f, 0.11f, 5.7f), roomC);
            CreateRoomFloor("Cold floor", new Vector3(-6.0f, 0.01f, 3.3f), new Vector3(5.5f, 0.11f, 5.4f), roomA);
            CreateRoomFloor("Crimson floor", new Vector3(6.2f, 0.01f, 3.4f), new Vector3(5.1f, 0.11f, 5.3f), roomB);

            // Outer shell. Door-sized gaps make the topology legible from the tactical camera.
            CreateWall("North west shell", new Vector3(-6.4f, 1.15f, 8.15f), new Vector3(7.5f, 2.3f, 0.22f), wall);
            CreateWall("North east shell", new Vector3(6.5f, 1.15f, 8.15f), new Vector3(7.2f, 2.3f, 0.22f), wall);
            CreateWall("West shell", new Vector3(-10.65f, 1.15f, 0.8f), new Vector3(0.22f, 2.3f, 14.7f), wall);
            CreateWall("East shell", new Vector3(10.65f, 1.15f, 0.8f), new Vector3(0.22f, 2.3f, 14.7f), wall);
            CreateWall("South west shell", new Vector3(-6.3f, 1.15f, -6.55f), new Vector3(7.7f, 2.3f, 0.22f), wall);
            CreateWall("South east shell", new Vector3(6.2f, 1.15f, -6.55f), new Vector3(7.9f, 2.3f, 0.22f), wall);

            // Interior partitions and thresholds.
            CreateWall("Archive Packing divider south", new Vector3(-1.15f, 1.05f, -4.6f), new Vector3(0.20f, 2.1f, 3.7f), wallEdge);
            CreateWall("Archive Packing divider north", new Vector3(-1.15f, 1.05f, -1.0f), new Vector3(0.20f, 2.1f, 1.2f), wallEdge);
            CreateWall("Archive north partition", new Vector3(-6.2f, 1.05f, -0.35f), new Vector3(6.9f, 2.1f, 0.20f), wall);
            CreateWall("Packing north partition", new Vector3(4.2f, 1.05f, -0.35f), new Vector3(5.6f, 2.1f, 0.20f), wall);
            CreateWall("Cold Service divider", new Vector3(-3.15f, 1.05f, 3.1f), new Vector3(0.20f, 2.1f, 5.2f), wallEdge);
            CreateWall("Service Crimson divider", new Vector3(2.55f, 1.05f, 4.6f), new Vector3(0.20f, 2.1f, 2.3f), wallEdge);

            // Tactical cover that makes the important positions readable.
            TacticalPrototypeVisual.CreateBlock("Archive shelves", new Vector3(-6.5f, 0.75f, -2.0f), new Vector3(2.4f, 1.5f, 0.75f), cover, transform, true);
            TacticalPrototypeVisual.CreateBlock("Packing machinery", new Vector3(3.7f, 0.65f, -2.8f), new Vector3(2.0f, 1.3f, 1.3f), cover, transform, true);
            TacticalPrototypeVisual.CreateBlock("Service desk", new Vector3(-0.4f, 0.55f, 1.4f), new Vector3(2.0f, 1.1f, 0.75f), cover, transform, true);
            TacticalPrototypeVisual.CreateBlock("Cold pallets", new Vector3(-6.7f, 0.55f, 4.0f), new Vector3(1.8f, 1.1f, 1.7f), cover, transform, true);
            TacticalPrototypeVisual.CreateBlock("Crimson cover", new Vector3(5.5f, 0.65f, 2.1f), new Vector3(1.8f, 1.3f, 0.8f), cover, transform, true);

            BuildUpperFloor(platform, railing);
            BuildStaircase(platform, wallEdge);
            BuildObjectives();
            BuildSurfaceVisuals(scenario);
            BuildLabels();
            BuildLighting();
        }

        private void BuildUpperFloor(Color platform, Color railing)
        {
            TacticalPrototypeVisual.CreateBlock(
                "Overwatch catwalk",
                new Vector3(-5.2f, 3.18f, 0.4f),
                new Vector3(7.3f, 0.30f, 6.2f),
                platform,
                transform,
                true);
            TacticalPrototypeVisual.CreateBlock(
                "Gallery catwalk",
                new Vector3(4.6f, 3.18f, -1.5f),
                new Vector3(7.2f, 0.30f, 5.2f),
                platform,
                transform,
                true);
            TacticalPrototypeVisual.CreateBlock(
                "Upper bridge",
                new Vector3(-0.1f, 3.18f, -1.7f),
                new Vector3(3.1f, 0.30f, 1.35f),
                platform,
                transform,
                true);

            // Rails are low enough that every dependency remains visible in the map view.
            TacticalPrototypeVisual.CreateBlock("Overwatch rail north", new Vector3(-5.2f, 3.75f, 3.45f), new Vector3(7.3f, 1.0f, 0.12f), railing, transform, false);
            TacticalPrototypeVisual.CreateBlock("Overwatch rail west", new Vector3(-8.8f, 3.75f, 0.4f), new Vector3(0.12f, 1.0f, 6.2f), railing, transform, false);
            TacticalPrototypeVisual.CreateBlock("Gallery rail east", new Vector3(8.15f, 3.75f, -1.5f), new Vector3(0.12f, 1.0f, 5.2f), railing, transform, false);
            TacticalPrototypeVisual.CreateBlock("Gallery rail south", new Vector3(4.6f, 3.75f, -4.05f), new Vector3(7.2f, 1.0f, 0.12f), railing, transform, false);

            // Hatch marker on the catwalk and a projected target below it.
            TacticalPrototypeVisual.CreateBlock(
                "Cold hatch opening",
                new Vector3(-5.2f, 3.36f, 2.75f),
                new Vector3(1.6f, 0.06f, 1.6f),
                new Color(0.02f, 0.025f, 0.03f),
                transform,
                false);
            TacticalPrototypeVisual.CreateBlock(
                "Cold hatch rim north",
                new Vector3(-5.2f, 3.40f, 3.57f),
                new Vector3(1.9f, 0.12f, 0.12f),
                new Color(0.65f, 0.75f, 0.80f),
                transform,
                false);
            TacticalPrototypeVisual.CreateBlock(
                "Cold hatch rim south",
                new Vector3(-5.2f, 3.40f, 1.93f),
                new Vector3(1.9f, 0.12f, 0.12f),
                new Color(0.65f, 0.75f, 0.80f),
                transform,
                false);
        }

        private void BuildStaircase(Color platform, Color edge)
        {
            const int stepCount = 11;
            for (int i = 0; i < stepCount; i++)
            {
                float t = i / (float)(stepCount - 1);
                float y = 0.12f + t * 3.02f;
                float z = 6.7f - t * 4.7f;
                TacticalPrototypeVisual.CreateBlock(
                    "Crimson stair " + i,
                    new Vector3(7.2f, y, z),
                    new Vector3(2.2f, 0.24f, 0.55f),
                    i % 2 == 0 ? platform : edge,
                    transform,
                    true);
            }
        }

        private void BuildObjectives()
        {
            Color objectiveA = new Color(0.25f, 0.82f, 0.95f);
            Color objectiveB = new Color(0.93f, 0.42f, 0.66f);
            TacticalPrototypeVisual.CreateCylinder(
                "Objective A",
                new Vector3(-4.1f, 0.18f, -4.2f),
                new Vector3(0.8f, 0.12f, 0.8f),
                objectiveA,
                transform,
                false);
            TacticalPrototypeVisual.CreateCylinder(
                "Objective B",
                new Vector3(2.4f, 0.18f, -4.2f),
                new Vector3(0.8f, 0.12f, 0.8f),
                objectiveB,
                transform,
                false);
            TacticalPrototypeVisual.CreateWorldLabel("A", new Vector3(-4.1f, 0.65f, -4.2f), 0.14f, objectiveA, transform);
            TacticalPrototypeVisual.CreateWorldLabel("B", new Vector3(2.4f, 0.65f, -4.2f), 0.14f, objectiveB, transform);
        }

        private void BuildSurfaceVisuals(TacticalScenarioDefinition scenario)
        {
            for (int surfaceIndex = 0; surfaceIndex < scenario.Surfaces.Count; surfaceIndex++)
            {
                TacticalSurfaceDefinition surface = scenario.Surfaces[surfaceIndex];
                GameObject parent = new GameObject(surface.Label);
                parent.transform.SetParent(transform, false);
                parent.transform.position = surface.WorldPosition;

                SurfaceVisual visual = new SurfaceVisual();
                visual.SolidPanel = TacticalPrototypeVisual.CreateBlock(
                    "Sealed panel",
                    surface.WorldPosition,
                    surface.WorldScale,
                    new Color(0.88f, 0.30f, 0.32f),
                    parent.transform,
                    false);
                // CreateBlock uses world coordinates, so restore parenting without preserving a duplicate offset.
                visual.SolidPanel.transform.SetParent(parent.transform, true);

                const int slatCount = 8;
                for (int i = 0; i < slatCount; i++)
                {
                    float t = slatCount == 1 ? 0.5f : i / (float)(slatCount - 1);
                    float localZ = Mathf.Lerp(-surface.WorldScale.z * 0.46f, surface.WorldScale.z * 0.46f, t);
                    GameObject slat = TacticalPrototypeVisual.CreateBlock(
                        "Permeable slat " + i,
                        surface.WorldPosition + new Vector3(0f, 0f, localZ),
                        new Vector3(surface.WorldScale.x, surface.WorldScale.y, 0.18f),
                        new Color(0.28f, 0.82f, 0.82f),
                        parent.transform,
                        false);
                    slat.transform.SetParent(parent.transform, true);
                    visual.Slats.Add(slat);
                }

                _surfaceVisuals[surface.Id] = visual;
                UpdateSurface(surface.Id, surface.DefaultState);
            }
        }

        private void BuildLabels()
        {
            Color label = new Color(0.70f, 0.76f, 0.80f);
            TacticalPrototypeVisual.CreateWorldLabel("ARCHIVE", new Vector3(-5.4f, 0.45f, -5.0f), 0.085f, label, transform);
            TacticalPrototypeVisual.CreateWorldLabel("PACKING", new Vector3(3.5f, 0.45f, -5.0f), 0.085f, label, transform);
            TacticalPrototypeVisual.CreateWorldLabel("SERVICE", new Vector3(-0.3f, 0.45f, 4.6f), 0.075f, label, transform);
            TacticalPrototypeVisual.CreateWorldLabel("COLD STORE", new Vector3(-6.0f, 0.45f, 6.0f), 0.075f, label, transform);
            TacticalPrototypeVisual.CreateWorldLabel("CRIMSON", new Vector3(6.2f, 0.45f, 6.2f), 0.075f, new Color(1.0f, 0.42f, 0.48f), transform);
            TacticalPrototypeVisual.CreateWorldLabel("OVERWATCH", new Vector3(-6.2f, 4.0f, -1.5f), 0.075f, label, transform);
            TacticalPrototypeVisual.CreateWorldLabel("GALLERY", new Vector3(5.3f, 4.0f, -3.2f), 0.075f, label, transform);
        }

        private void BuildLighting()
        {
            GameObject sunObject = new GameObject("Tactical key light");
            sunObject.transform.SetParent(transform, false);
            Light sun = sunObject.AddComponent<Light>();
            TacticalPrototypeVisual.ConfigurePipelineLight(sunObject);
            sun.type = LightType.Directional;
            sun.color = new Color(0.86f, 0.93f, 1.0f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

            CreatePointLight("Archive light", new Vector3(-5f, 5.8f, -2.5f), new Color(0.32f, 0.70f, 1.0f), 6f, 18f);
            CreatePointLight("Packing light", new Vector3(4f, 5.8f, -2.0f), new Color(1.0f, 0.55f, 0.35f), 5f, 17f);
            CreatePointLight("Service light", new Vector3(0f, 4.8f, 4.3f), new Color(0.35f, 1.0f, 0.80f), 4f, 15f);
        }

        private void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            TacticalPrototypeVisual.ConfigurePipelineLight(lightObject);
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private void CreateRoomFloor(string name, Vector3 position, Vector3 scale, Color color)
        {
            TacticalPrototypeVisual.CreateBlock(name, position, scale, color, transform, true);
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale, Color color)
        {
            TacticalPrototypeVisual.CreateBlock(name, position, scale, color, transform, true);
        }
    }
}

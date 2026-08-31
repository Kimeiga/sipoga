using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sipoga.Tactics
{
    public sealed class TacticalPrototypeBootstrap : MonoBehaviour
    {
        private const string PrototypeSceneName = "TacticalSquadPrototype";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LaunchPrototypeScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != PrototypeSceneName)
            {
                return;
            }

            if (FindObjectOfType<TacticalPrototypeBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject("Sipoga Tactical Prototype Bootstrap");
            bootstrapObject.AddComponent<TacticalPrototypeBootstrap>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            BuildPrototype();
        }

        private void BuildPrototype()
        {
            TacticalScenarioDefinition scenario = GlasshouseScenario.Create();
            List<string> validationErrors = TacticalScenarioValidator.Validate(scenario);
            for (int i = 0; i < validationErrors.Count; i++)
            {
                Debug.LogError("[Sipoga Tactics] " + validationErrors[i]);
            }

            TacticalPrototypeWorld world = TacticalPrototypeWorld.Build(scenario);

            GameObject directorObject = new GameObject("Tactical Squad Director");
            TacticalSquadDirector director = directorObject.AddComponent<TacticalSquadDirector>();
            director.Initialize(scenario, world);

            GameObject squadRoot = new GameObject("Six Operator Squad");
            for (int i = 0; i < scenario.Units.Count; i++)
            {
                TacticalUnitPlan plan = scenario.Units[i];
                GameObject unitObject = new GameObject(plan.Callsign);
                unitObject.transform.SetParent(squadRoot.transform, false);
                TacticalUnitAgent agent = unitObject.AddComponent<TacticalUnitAgent>();
                agent.Initialize(director, plan, director.Engine.GetPosition(plan.HomePositionId));
                director.RegisterAgent(agent);
            }

            GameObject cameraObject = new GameObject("Tactical Prototype Camera");
            cameraObject.AddComponent<Camera>();
            TacticalPrototypeCamera cameraController = cameraObject.AddComponent<TacticalPrototypeCamera>();
            cameraController.Initialize();
            director.SetCameraController(cameraController);

            GameObject overlayObject = new GameObject("Tactical Dependency Overlay");
            TacticalOverlayRenderer overlay = overlayObject.AddComponent<TacticalOverlayRenderer>();
            overlay.Initialize(director);
            TacticalThreatVisualizer threatVisualizer = overlayObject.AddComponent<TacticalThreatVisualizer>();
            threatVisualizer.Initialize(director);

            GameObject hudObject = new GameObject("Tactical Explanation HUD");
            TacticalPrototypeHud hud = hudObject.AddComponent<TacticalPrototypeHud>();
            hud.Initialize(director);
            TacticalCommandHud commandHud = hudObject.AddComponent<TacticalCommandHud>();
            commandHud.Initialize(director);

            director.ResetCurrentPlaybook();
            director.SelectUnit("alpha");
        }
    }
}

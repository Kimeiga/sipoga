using System.Collections.Generic;
using UnityEngine;

namespace Sipoga.Tactics
{
    /// <summary>
    /// Turns abstract route pressure into visible attacking bodies. The tokens do
    /// not make tactical decisions; they expose the attacker's current weight and
    /// whether a defended route is holding, being repaired, or collapsing.
    /// </summary>
    public sealed class TacticalThreatVisualizer : MonoBehaviour
    {
        private sealed class ThreatLane
        {
            public TacticalRouteDefinition Route;
            public readonly List<GameObject> Tokens = new List<GameObject>();
            public float Progress;
        }

        private readonly Dictionary<string, ThreatLane> _lanes =
            new Dictionary<string, ThreatLane>();
        private TacticalSquadDirector _director;
        private Material _holdingMaterial;
        private Material _advancingMaterial;
        private Material _repairedMaterial;

        public void Initialize(TacticalSquadDirector director)
        {
            _director = director;
            _holdingMaterial = TacticalPrototypeVisual.GetMaterial(
                new Color(0.72f, 0.20f, 0.24f),
                true);
            _advancingMaterial = TacticalPrototypeVisual.GetMaterial(
                new Color(1.00f, 0.16f, 0.22f),
                true);
            _repairedMaterial = TacticalPrototypeVisual.GetMaterial(
                new Color(1.00f, 0.53f, 0.16f),
                true);
            BuildLanes();
        }

        private void BuildLanes()
        {
            Transform root = new GameObject("Attacker pressure tokens").transform;
            root.SetParent(transform, false);

            for (int routeIndex = 0; routeIndex < _director.Scenario.Routes.Count; routeIndex++)
            {
                TacticalRouteDefinition route = _director.Scenario.Routes[routeIndex];
                ThreatLane lane = new ThreatLane();
                lane.Route = route;
                lane.Progress = 0f;

                for (int tokenIndex = 0; tokenIndex < 8; tokenIndex++)
                {
                    GameObject token = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    token.name = "Attacker " + route.Label + " " + (tokenIndex + 1);
                    token.transform.SetParent(root, false);
                    token.transform.localScale = new Vector3(0.25f, 0.34f, 0.25f);
                    Collider collider = token.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Destroy(collider);
                    }

                    Renderer renderer = token.GetComponent<Renderer>();
                    renderer.sharedMaterial = _holdingMaterial;
                    token.SetActive(false);
                    lane.Tokens.Add(token);
                }

                _lanes.Add(route.Id, lane);
            }
        }

        private void Update()
        {
            if (_director == null || _director.Evaluation == null)
            {
                return;
            }

            if (_director.Phase == TacticalPrototypePhase.Planning)
            {
                ResetLanes();
                return;
            }

            float deltaTime = Time.deltaTime;
            foreach (KeyValuePair<string, ThreatLane> pair in _lanes)
            {
                TacticalRouteRuntimeState routeState = _director.Evaluation.GetRoute(pair.Key);
                UpdateLane(pair.Value, routeState, deltaTime);
            }
        }

        private void UpdateLane(
            ThreatLane lane,
            TacticalRouteRuntimeState routeState,
            float deltaTime)
        {
            if (routeState == null)
            {
                SetTokenCount(lane, 0);
                return;
            }

            int visibleCount = Mathf.Clamp(routeState.Pressure, 0, lane.Tokens.Count);
            SetTokenCount(lane, visibleCount);
            if (visibleCount <= 0)
            {
                lane.Progress = Mathf.MoveTowards(lane.Progress, 0f, deltaTime * 0.8f);
                return;
            }

            float heldLimit = Mathf.Clamp(0.18f + routeState.Pressure * 0.025f, 0.22f, 0.45f);
            float targetProgress;
            float speed;
            if (!routeState.IsSecured && !routeState.IsBeingRepaired)
            {
                targetProgress = 0.96f;
                speed = 0.055f + routeState.Pressure * 0.014f;
            }
            else if (routeState.IsBeingRepaired)
            {
                targetProgress = 0.68f;
                speed = 0.045f + routeState.Pressure * 0.010f;
            }
            else
            {
                targetProgress = heldLimit;
                speed = routeState.IsFlexHolding ? 0.22f : 0.14f;
            }

            lane.Progress = Mathf.MoveTowards(lane.Progress, targetProgress, speed * deltaTime);
            Vector3 direction = lane.Route.End - lane.Route.Start;
            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;
            Quaternion rotation = flatDirection.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(flatDirection, Vector3.up)
                : Quaternion.identity;
            Material material = routeState.IsFlexHolding
                ? _repairedMaterial
                : routeState.IsSecured
                    ? _holdingMaterial
                    : _advancingMaterial;

            for (int i = 0; i < visibleCount; i++)
            {
                GameObject token = lane.Tokens[i];
                float spacing = 0.065f;
                float tokenProgress = Mathf.Clamp01(lane.Progress - i * spacing);
                float side = ((i % 3) - 1) * 0.26f;
                Vector3 lateral = Vector3.Cross(Vector3.up, flatDirection) * side;
                Vector3 position = Vector3.Lerp(
                    lane.Route.Start,
                    lane.Route.End,
                    tokenProgress);
                position += lateral + Vector3.up * 0.48f;
                token.transform.position = position;
                token.transform.rotation = rotation;
                Renderer renderer = token.GetComponent<Renderer>();
                renderer.sharedMaterial = material;

                float pulse = 1f + Mathf.Sin(
                    Time.time * 7f + i * 1.3f) *
                    (!routeState.IsSecured ? 0.10f : 0.035f);
                token.transform.localScale = new Vector3(0.25f, 0.34f, 0.25f) * pulse;
            }
        }

        private static void SetTokenCount(ThreatLane lane, int visibleCount)
        {
            for (int i = 0; i < lane.Tokens.Count; i++)
            {
                lane.Tokens[i].SetActive(i < visibleCount);
            }
        }

        private void ResetLanes()
        {
            foreach (KeyValuePair<string, ThreatLane> pair in _lanes)
            {
                pair.Value.Progress = 0f;
                SetTokenCount(pair.Value, 0);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Sipoga.Tactics
{
    public sealed class TacticalOverlayRenderer : MonoBehaviour
    {
        private sealed class RouteVisual
        {
            public Renderer Band;
            public TextMesh Label;
        }

        private TacticalSquadDirector _director;
        private TacticalScenarioDefinition _scenario;
        private readonly Dictionary<string, RouteVisual> _routeVisuals =
            new Dictionary<string, RouteVisual>();
        private readonly Dictionary<string, LineRenderer> _coverageLines =
            new Dictionary<string, LineRenderer>();
        private LineRenderer _flexDirectiveLine;
        private GameObject _assignedMarker;
        private GameObject _fallbackMarker;
        private GameObject _repairMarker;

        private readonly Color _securedColor = new Color(0.20f, 0.90f, 0.66f);
        private readonly Color _repairedColor = new Color(0.96f, 0.86f, 0.25f);
        private readonly Color _repairingColor = new Color(1.00f, 0.58f, 0.20f);
        private readonly Color _brokenColor = new Color(1.00f, 0.20f, 0.28f);
        private readonly Color _inactiveLineColor = new Color(1.00f, 0.20f, 0.28f);

        public void Initialize(TacticalSquadDirector director)
        {
            _director = director;
            _scenario = director.Scenario;
            BuildRouteVisuals();
            BuildCoverageLines();
            BuildSelectionMarkers();
        }

        private void Update()
        {
            if (_director == null || _director.Evaluation == null)
            {
                return;
            }

            UpdateRoutes();
            UpdateCoverageLines();
            UpdateFlexDirective();
            UpdateSelectionMarkers();
        }

        private void BuildRouteVisuals()
        {
            Transform routeRoot = new GameObject("Route state overlays").transform;
            routeRoot.SetParent(transform, false);

            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                TacticalRouteDefinition route = _scenario.Routes[i];
                Vector3 delta = route.End - route.Start;
                float length = delta.magnitude;
                GameObject band = TacticalPrototypeVisual.CreateBlock(
                    route.Label + " route band",
                    route.Midpoint,
                    new Vector3(0.42f, 0.045f, length),
                    _securedColor,
                    routeRoot,
                    false);
                band.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);

                TextMesh label = TacticalPrototypeVisual.CreateWorldLabel(
                    route.Label,
                    route.Midpoint + Vector3.up * 0.58f,
                    0.047f,
                    Color.white,
                    routeRoot);

                RouteVisual visual = new RouteVisual();
                visual.Band = band.GetComponent<Renderer>();
                visual.Label = label;
                _routeVisuals.Add(route.Id, visual);
            }
        }

        private void BuildCoverageLines()
        {
            Transform lineRoot = new GameObject("Promise lines").transform;
            lineRoot.SetParent(transform, false);
            for (int i = 0; i < _scenario.CoverageRules.Count; i++)
            {
                TacticalCoverageRule rule = _scenario.CoverageRules[i];
                TacticalUnitPlan plan = _director.Engine.GetUnitPlan(rule.UnitId);
                LineRenderer line = TacticalPrototypeVisual.CreateLine(
                    "Promise " + rule.Id,
                    0.035f,
                    plan.DisplayColor,
                    lineRoot);
                _coverageLines.Add(rule.Id, line);
            }

            _flexDirectiveLine = TacticalPrototypeVisual.CreateLine(
                "Flex reassignment",
                0.075f,
                _repairingColor,
                lineRoot);
            _flexDirectiveLine.enabled = false;
        }

        private void BuildSelectionMarkers()
        {
            _assignedMarker = TacticalPrototypeVisual.CreateCylinder(
                "Selected assignment marker",
                Vector3.zero,
                new Vector3(0.42f, 0.025f, 0.42f),
                Color.white,
                transform,
                false);
            _fallbackMarker = TacticalPrototypeVisual.CreateCylinder(
                "Selected fallback marker",
                Vector3.zero,
                new Vector3(0.34f, 0.020f, 0.34f),
                new Color(0.52f, 0.58f, 0.64f),
                transform,
                false);
            _repairMarker = TacticalPrototypeVisual.CreateCylinder(
                "Flex repair marker",
                Vector3.zero,
                new Vector3(0.52f, 0.025f, 0.52f),
                _repairingColor,
                transform,
                false);
        }

        private void UpdateRoutes()
        {
            for (int i = 0; i < _director.Evaluation.Routes.Count; i++)
            {
                TacticalRouteRuntimeState routeState = _director.Evaluation.Routes[i];
                RouteVisual visual = _routeVisuals[routeState.RouteId];
                Color color;
                if (routeState.IsSecured)
                {
                    color = routeState.IsFlexHolding ? _repairedColor : _securedColor;
                }
                else
                {
                    color = routeState.IsBeingRepaired ? _repairingColor : _brokenColor;
                }

                visual.Band.sharedMaterial = TacticalPrototypeVisual.GetMaterial(color, true);
                TacticalRouteDefinition route = _director.Engine.GetRouteDefinition(routeState.RouteId);
                visual.Label.color = color;
                visual.Label.text =
                    route.Label + "\n" +
                    routeState.Coverage + "/" + routeState.RequiredCoverage + "  " +
                    routeState.StatusLabel +
                    (routeState.Pressure > 0 ? "  PRESSURE " + routeState.Pressure : string.Empty);
            }
        }

        private void UpdateCoverageLines()
        {
            for (int i = 0; i < _scenario.CoverageRules.Count; i++)
            {
                TacticalCoverageRule rule = _scenario.CoverageRules[i];
                LineRenderer line = _coverageLines[rule.Id];
                TacticalUnitAgent agent = _director.GetAgent(rule.UnitId);
                TacticalRouteDefinition route = _director.Engine.GetRouteDefinition(rule.RouteId);
                if (agent == null || route == null)
                {
                    line.enabled = false;
                    continue;
                }

                bool active = _director.Engine.IsCoverageRuleActive(rule);
                bool selectedAndBroken =
                    _director.SelectedUnitId == rule.UnitId &&
                    !active &&
                    !agent.IsDown;
                line.enabled = active || selectedAndBroken;
                if (!line.enabled)
                {
                    continue;
                }

                Color color = active ? agent.Plan.DisplayColor : _inactiveLineColor;
                line.sharedMaterial = TacticalPrototypeVisual.GetMaterial(color, true);
                float width = _director.SelectedUnitId == rule.UnitId ? 0.075f : 0.035f;
                line.startWidth = width;
                line.endWidth = width;
                line.SetPosition(0, agent.transform.position + Vector3.up * 0.80f);
                line.SetPosition(1, route.Midpoint + Vector3.up * 0.18f);
            }
        }

        private void UpdateFlexDirective()
        {
            TacticalFlexDirective directive = _director.Evaluation.FlexDirective;
            TacticalUnitAgent flex = _director.GetAgent(GlasshouseScenario.FlexUnitId);
            if (flex == null || !directive.IsActive || flex.IsDown)
            {
                _flexDirectiveLine.enabled = false;
                _repairMarker.SetActive(false);
                return;
            }

            TacticalPositionDefinition target = _director.Engine.GetPosition(directive.PositionId);
            TacticalRouteRuntimeState routeState = _director.Evaluation.GetRoute(directive.RouteId);
            if (target == null || routeState == null)
            {
                _flexDirectiveLine.enabled = false;
                _repairMarker.SetActive(false);
                return;
            }

            Color color = routeState.IsFlexHolding ? _repairedColor : _repairingColor;
            _flexDirectiveLine.enabled = true;
            _flexDirectiveLine.sharedMaterial = TacticalPrototypeVisual.GetMaterial(color, true);
            _flexDirectiveLine.startWidth = 0.08f;
            _flexDirectiveLine.endWidth = 0.08f;
            _flexDirectiveLine.SetPosition(0, flex.transform.position + Vector3.up * 0.95f);
            _flexDirectiveLine.SetPosition(1, target.WorldPosition + Vector3.up * 0.18f);

            _repairMarker.SetActive(true);
            _repairMarker.transform.position = target.WorldPosition + Vector3.down * 0.92f;
            Renderer markerRenderer = _repairMarker.GetComponent<Renderer>();
            markerRenderer.sharedMaterial = TacticalPrototypeVisual.GetMaterial(color, true);
        }

        private void UpdateSelectionMarkers()
        {
            TacticalUnitPlan plan = _director.Engine.GetUnitPlan(_director.SelectedUnitId);
            if (plan == null)
            {
                _assignedMarker.SetActive(false);
                _fallbackMarker.SetActive(false);
                return;
            }

            string assignedPositionId = plan.HomePositionId;
            if (plan.IsFlex && _director.Evaluation.FlexDirective.IsActive)
            {
                assignedPositionId = _director.Evaluation.FlexDirective.PositionId;
            }

            TacticalPositionDefinition assigned = _director.Engine.GetPosition(assignedPositionId);
            TacticalPositionDefinition fallback = _director.Engine.GetPosition(plan.FallbackPositionId);
            _assignedMarker.SetActive(assigned != null);
            _fallbackMarker.SetActive(fallback != null);
            if (assigned != null)
            {
                _assignedMarker.transform.position = assigned.WorldPosition + Vector3.down * 0.91f;
                _assignedMarker.GetComponent<Renderer>().sharedMaterial =
                    TacticalPrototypeVisual.GetMaterial(plan.DisplayColor, true);
            }

            if (fallback != null)
            {
                _fallbackMarker.transform.position = fallback.WorldPosition + Vector3.down * 0.92f;
            }
        }
    }
}

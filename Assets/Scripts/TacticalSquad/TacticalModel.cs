using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sipoga.Tactics
{
    public enum TacticalSurfaceState
    {
        Permeable,
        Open,
        Sealed
    }

    public enum TacticalUnitCondition
    {
        Active,
        Down
    }

    public enum TacticalResponsibilityKind
    {
        HoldAngle,
        DenyRoute,
        DenySurface,
        Crossfire,
        Delay,
        Reserve
    }

    public enum TacticalAttackAction
    {
        Log,
        SetPressure,
        SetSurfaceState,
        SetUnitDown
    }

    [Serializable]
    public sealed class TacticalPositionDefinition
    {
        public string Id;
        public string Label;
        public Vector3 WorldPosition;
        public int Floor;

        public TacticalPositionDefinition(string id, string label, Vector3 worldPosition, int floor)
        {
            Id = id;
            Label = label;
            WorldPosition = worldPosition;
            Floor = floor;
        }
    }

    [Serializable]
    public sealed class TacticalRouteDefinition
    {
        public string Id;
        public string Label;
        public string Description;
        public int RequiredCoverage;
        public Vector3 Start;
        public Vector3 End;

        public TacticalRouteDefinition(
            string id,
            string label,
            string description,
            int requiredCoverage,
            Vector3 start,
            Vector3 end)
        {
            Id = id;
            Label = label;
            Description = description;
            RequiredCoverage = requiredCoverage;
            Start = start;
            End = end;
        }

        public Vector3 Midpoint
        {
            get { return Vector3.Lerp(Start, End, 0.5f); }
        }
    }

    [Serializable]
    public sealed class TacticalSurfaceDefinition
    {
        public string Id;
        public string Label;
        public TacticalSurfaceState DefaultState;
        public Vector3 WorldPosition;
        public Vector3 WorldScale;

        public TacticalSurfaceDefinition(
            string id,
            string label,
            TacticalSurfaceState defaultState,
            Vector3 worldPosition,
            Vector3 worldScale)
        {
            Id = id;
            Label = label;
            DefaultState = defaultState;
            WorldPosition = worldPosition;
            WorldScale = worldScale;
        }
    }

    [Serializable]
    public sealed class TacticalUnitPlan
    {
        public string UnitId;
        public string Callsign;
        public string RoleLabel;
        public TacticalResponsibilityKind ResponsibilityKind;
        public string HomePositionId;
        public string FallbackPositionId;
        public string ExecutePositionId;
        public bool IsFlex;
        public Color DisplayColor;
        public string WhatText;
        public string WhySafeText;
        public string BreakText;
        public string FallbackText;

        public TacticalUnitPlan(
            string unitId,
            string callsign,
            string roleLabel,
            TacticalResponsibilityKind responsibilityKind,
            string homePositionId,
            string fallbackPositionId,
            string executePositionId,
            bool isFlex,
            Color displayColor,
            string whatText,
            string whySafeText,
            string breakText,
            string fallbackText)
        {
            UnitId = unitId;
            Callsign = callsign;
            RoleLabel = roleLabel;
            ResponsibilityKind = responsibilityKind;
            HomePositionId = homePositionId;
            FallbackPositionId = fallbackPositionId;
            ExecutePositionId = executePositionId;
            IsFlex = isFlex;
            DisplayColor = displayColor;
            WhatText = whatText;
            WhySafeText = whySafeText;
            BreakText = breakText;
            FallbackText = fallbackText;
        }
    }

    [Serializable]
    public sealed class TacticalCoverageRule
    {
        public string Id;
        public string UnitId;
        public string RouteId;
        public string PositionId;
        public string RequiredSurfaceId;
        public TacticalSurfaceState[] AllowedSurfaceStates;
        public string Explanation;

        public TacticalCoverageRule(
            string id,
            string unitId,
            string routeId,
            string positionId,
            string explanation)
            : this(
                id,
                unitId,
                routeId,
                positionId,
                string.Empty,
                new TacticalSurfaceState[0],
                explanation)
        {
        }

        public TacticalCoverageRule(
            string id,
            string unitId,
            string routeId,
            string positionId,
            string requiredSurfaceId,
            TacticalSurfaceState[] allowedSurfaceStates,
            string explanation)
        {
            Id = id;
            UnitId = unitId;
            RouteId = routeId;
            PositionId = positionId;
            RequiredSurfaceId = requiredSurfaceId;
            AllowedSurfaceStates = allowedSurfaceStates ?? new TacticalSurfaceState[0];
            Explanation = explanation;
        }
    }

    [Serializable]
    public sealed class TacticalFlexOption
    {
        public string RouteId;
        public string PositionId;
        public string ResponsibilityLabel;
        public int Priority;
        public string Explanation;

        public TacticalFlexOption(
            string routeId,
            string positionId,
            string responsibilityLabel,
            int priority,
            string explanation)
        {
            RouteId = routeId;
            PositionId = positionId;
            ResponsibilityLabel = responsibilityLabel;
            Priority = priority;
            Explanation = explanation;
        }
    }

    [Serializable]
    public sealed class TacticalAttackStep
    {
        public float Time;
        public TacticalAttackAction Action;
        public string TargetId;
        public int IntValue;
        public TacticalSurfaceState SurfaceState;
        public bool BoolValue;
        public string Message;

        public TacticalAttackStep(
            float time,
            TacticalAttackAction action,
            string targetId,
            int intValue,
            TacticalSurfaceState surfaceState,
            bool boolValue,
            string message)
        {
            Time = time;
            Action = action;
            TargetId = targetId;
            IntValue = intValue;
            SurfaceState = surfaceState;
            BoolValue = boolValue;
            Message = message;
        }

        public static TacticalAttackStep Log(float time, string message)
        {
            return new TacticalAttackStep(
                time,
                TacticalAttackAction.Log,
                string.Empty,
                0,
                TacticalSurfaceState.Permeable,
                false,
                message);
        }

        public static TacticalAttackStep Pressure(float time, string routeId, int pressure, string message)
        {
            return new TacticalAttackStep(
                time,
                TacticalAttackAction.SetPressure,
                routeId,
                pressure,
                TacticalSurfaceState.Permeable,
                false,
                message);
        }

        public static TacticalAttackStep Surface(
            float time,
            string surfaceId,
            TacticalSurfaceState state,
            string message)
        {
            return new TacticalAttackStep(
                time,
                TacticalAttackAction.SetSurfaceState,
                surfaceId,
                0,
                state,
                false,
                message);
        }

        public static TacticalAttackStep UnitDown(float time, string unitId, bool down, string message)
        {
            return new TacticalAttackStep(
                time,
                TacticalAttackAction.SetUnitDown,
                unitId,
                0,
                TacticalSurfaceState.Permeable,
                down,
                message);
        }
    }

    [Serializable]
    public sealed class TacticalAttackPlaybook
    {
        public string Id;
        public string Name;
        public string Summary;
        public string Lesson;
        public float Duration;
        public List<TacticalAttackStep> Steps = new List<TacticalAttackStep>();

        public TacticalAttackPlaybook(
            string id,
            string name,
            string summary,
            string lesson,
            float duration)
        {
            Id = id;
            Name = name;
            Summary = summary;
            Lesson = lesson;
            Duration = duration;
        }
    }

    [Serializable]
    public sealed class TacticalScenarioDefinition
    {
        public string Id;
        public string DisplayName;
        public string Revision;
        public string Description;
        public List<TacticalPositionDefinition> Positions = new List<TacticalPositionDefinition>();
        public List<TacticalRouteDefinition> Routes = new List<TacticalRouteDefinition>();
        public List<TacticalSurfaceDefinition> Surfaces = new List<TacticalSurfaceDefinition>();
        public List<TacticalUnitPlan> Units = new List<TacticalUnitPlan>();
        public List<TacticalCoverageRule> CoverageRules = new List<TacticalCoverageRule>();
        public List<TacticalFlexOption> FlexOptions = new List<TacticalFlexOption>();
        public List<TacticalAttackPlaybook> AttackPlaybooks = new List<TacticalAttackPlaybook>();

        public TacticalScenarioDefinition(string id, string displayName, string revision, string description)
        {
            Id = id;
            DisplayName = displayName;
            Revision = revision;
            Description = description;
        }
    }

    public sealed class TacticalUnitRuntimeState
    {
        public string UnitId;
        public string PositionId;
        public TacticalUnitCondition Condition;
        public bool IsInTransit;

        public TacticalUnitRuntimeState(string unitId, string positionId)
        {
            UnitId = unitId;
            PositionId = positionId;
            Condition = TacticalUnitCondition.Active;
            IsInTransit = false;
        }
    }

    public sealed class TacticalRouteRuntimeState
    {
        public string RouteId;
        public int BaselineCoverage;
        public int Coverage;
        public int RequiredCoverage;
        public int Pressure;
        public bool IsBeingRepaired;
        public bool IsFlexHolding;
        public bool IsSecured;
        public List<string> Contributors = new List<string>();

        public string StatusLabel
        {
            get
            {
                if (IsSecured)
                {
                    return IsFlexHolding ? "REPAIRED" : "SECURED";
                }

                return IsBeingRepaired ? "REPAIRING" : "BROKEN";
            }
        }
    }

    public sealed class TacticalFlexDirective
    {
        public bool IsActive;
        public string UnitId;
        public string RouteId;
        public string PositionId;
        public string ResponsibilityLabel;
        public string Explanation;

        public static TacticalFlexDirective ReturnToReserve(string unitId, string positionId)
        {
            TacticalFlexDirective directive = new TacticalFlexDirective();
            directive.IsActive = false;
            directive.UnitId = unitId;
            directive.RouteId = string.Empty;
            directive.PositionId = positionId;
            directive.ResponsibilityLabel = "Reserve";
            directive.Explanation = "All baseline promises are intact. Return to reserve and wait for the first failure.";
            return directive;
        }
    }

    public sealed class TacticalExplanation
    {
        public string What;
        public string WhySafe;
        public string WhatBreaksIt;
        public string Fallback;
    }

    public sealed class TacticalEvaluation
    {
        public readonly List<TacticalRouteRuntimeState> Routes;
        public readonly TacticalFlexDirective FlexDirective;

        public TacticalEvaluation(
            List<TacticalRouteRuntimeState> routes,
            TacticalFlexDirective flexDirective)
        {
            Routes = routes;
            FlexDirective = flexDirective;
        }

        public TacticalRouteRuntimeState GetRoute(string routeId)
        {
            for (int i = 0; i < Routes.Count; i++)
            {
                if (Routes[i].RouteId == routeId)
                {
                    return Routes[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Pure tactical state evaluator. It deliberately knows nothing about cameras,
    /// animation, navigation, rendering, or weapons. That lets the same authored
    /// strategy drive gameplay, overlays, explanations, tests, and replays.
    /// </summary>
    public sealed class TacticalStateEngine
    {
        private readonly TacticalScenarioDefinition _scenario;
        private readonly Dictionary<string, TacticalPositionDefinition> _positions =
            new Dictionary<string, TacticalPositionDefinition>();
        private readonly Dictionary<string, TacticalRouteDefinition> _routes =
            new Dictionary<string, TacticalRouteDefinition>();
        private readonly Dictionary<string, TacticalSurfaceDefinition> _surfaceDefinitions =
            new Dictionary<string, TacticalSurfaceDefinition>();
        private readonly Dictionary<string, TacticalUnitPlan> _unitPlans =
            new Dictionary<string, TacticalUnitPlan>();
        private readonly Dictionary<string, TacticalUnitRuntimeState> _units =
            new Dictionary<string, TacticalUnitRuntimeState>();
        private readonly Dictionary<string, TacticalSurfaceState> _surfaces =
            new Dictionary<string, TacticalSurfaceState>();
        private readonly Dictionary<string, int> _routePressure =
            new Dictionary<string, int>();

        private string _lastFlexRouteId = string.Empty;

        public TacticalStateEngine(TacticalScenarioDefinition scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException("scenario");
            }

            _scenario = scenario;
            IndexScenario();
            Reset();
        }

        public TacticalScenarioDefinition Scenario
        {
            get { return _scenario; }
        }

        public void Reset()
        {
            _units.Clear();
            for (int i = 0; i < _scenario.Units.Count; i++)
            {
                TacticalUnitPlan plan = _scenario.Units[i];
                _units.Add(plan.UnitId, new TacticalUnitRuntimeState(plan.UnitId, plan.HomePositionId));
            }

            _surfaces.Clear();
            for (int i = 0; i < _scenario.Surfaces.Count; i++)
            {
                TacticalSurfaceDefinition surface = _scenario.Surfaces[i];
                _surfaces.Add(surface.Id, surface.DefaultState);
            }

            _routePressure.Clear();
            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                _routePressure.Add(_scenario.Routes[i].Id, 0);
            }

            _lastFlexRouteId = string.Empty;
        }

        public TacticalUnitRuntimeState GetUnitState(string unitId)
        {
            TacticalUnitRuntimeState state;
            return _units.TryGetValue(unitId, out state) ? state : null;
        }

        public TacticalUnitPlan GetUnitPlan(string unitId)
        {
            TacticalUnitPlan plan;
            return _unitPlans.TryGetValue(unitId, out plan) ? plan : null;
        }

        public TacticalPositionDefinition GetPosition(string positionId)
        {
            TacticalPositionDefinition position;
            return _positions.TryGetValue(positionId, out position) ? position : null;
        }

        public TacticalRouteDefinition GetRouteDefinition(string routeId)
        {
            TacticalRouteDefinition route;
            return _routes.TryGetValue(routeId, out route) ? route : null;
        }

        public TacticalSurfaceState GetSurfaceState(string surfaceId)
        {
            TacticalSurfaceState state;
            if (!_surfaces.TryGetValue(surfaceId, out state))
            {
                throw new ArgumentException("Unknown tactical surface: " + surfaceId);
            }

            return state;
        }

        public int GetRoutePressure(string routeId)
        {
            int pressure;
            return _routePressure.TryGetValue(routeId, out pressure) ? pressure : 0;
        }

        public void SetUnitPosition(string unitId, string positionId)
        {
            TacticalUnitRuntimeState unit = RequireUnit(unitId);
            RequirePosition(positionId);
            unit.PositionId = positionId;
            unit.IsInTransit = false;
        }

        public void MarkUnitInTransit(string unitId)
        {
            TacticalUnitRuntimeState unit = RequireUnit(unitId);
            unit.PositionId = string.Empty;
            unit.IsInTransit = true;
        }

        public void SetUnitDown(string unitId, bool down)
        {
            TacticalUnitRuntimeState unit = RequireUnit(unitId);
            unit.Condition = down ? TacticalUnitCondition.Down : TacticalUnitCondition.Active;
        }

        public void SetSurfaceState(string surfaceId, TacticalSurfaceState state)
        {
            if (!_surfaces.ContainsKey(surfaceId))
            {
                throw new ArgumentException("Unknown tactical surface: " + surfaceId);
            }

            _surfaces[surfaceId] = state;
        }

        public void SetRoutePressure(string routeId, int pressure)
        {
            if (!_routePressure.ContainsKey(routeId))
            {
                throw new ArgumentException("Unknown tactical route: " + routeId);
            }

            _routePressure[routeId] = Mathf.Max(0, pressure);
        }

        public TacticalEvaluation Evaluate()
        {
            Dictionary<string, int> baseline = new Dictionary<string, int>();
            Dictionary<string, List<string>> contributors = new Dictionary<string, List<string>>();

            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                string routeId = _scenario.Routes[i].Id;
                baseline[routeId] = 0;
                contributors[routeId] = new List<string>();
            }

            for (int i = 0; i < _scenario.CoverageRules.Count; i++)
            {
                TacticalCoverageRule rule = _scenario.CoverageRules[i];
                if (!IsCoverageRuleActive(rule))
                {
                    continue;
                }

                baseline[rule.RouteId] = baseline[rule.RouteId] + 1;
                contributors[rule.RouteId].Add(rule.UnitId);
            }

            TacticalUnitPlan flexPlan = FindFlexPlan();
            TacticalFlexDirective directive = BuildFlexDirective(flexPlan, baseline);

            string physicallyHeldFlexRoute = string.Empty;
            if (flexPlan != null)
            {
                TacticalUnitRuntimeState flexState = _units[flexPlan.UnitId];
                if (flexState.Condition == TacticalUnitCondition.Active && !flexState.IsInTransit)
                {
                    for (int i = 0; i < _scenario.FlexOptions.Count; i++)
                    {
                        TacticalFlexOption option = _scenario.FlexOptions[i];
                        if (flexState.PositionId == option.PositionId)
                        {
                            physicallyHeldFlexRoute = option.RouteId;
                            break;
                        }
                    }
                }
            }

            List<TacticalRouteRuntimeState> routeStates = new List<TacticalRouteRuntimeState>();
            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                TacticalRouteDefinition route = _scenario.Routes[i];
                TacticalRouteRuntimeState routeState = new TacticalRouteRuntimeState();
                routeState.RouteId = route.Id;
                routeState.BaselineCoverage = baseline[route.Id];
                routeState.Coverage = routeState.BaselineCoverage;
                routeState.RequiredCoverage = route.RequiredCoverage;
                routeState.Pressure = GetRoutePressure(route.Id);
                routeState.Contributors.AddRange(contributors[route.Id]);

                if (flexPlan != null && physicallyHeldFlexRoute == route.Id)
                {
                    routeState.Coverage += 1;
                    routeState.IsFlexHolding = true;
                    routeState.Contributors.Add(flexPlan.UnitId);
                }

                routeState.IsBeingRepaired =
                    directive.IsActive &&
                    directive.RouteId == route.Id &&
                    !routeState.IsFlexHolding;
                routeState.IsSecured = routeState.Coverage >= routeState.RequiredCoverage;
                routeStates.Add(routeState);
            }

            return new TacticalEvaluation(routeStates, directive);
        }

        public bool IsCoverageRuleActive(TacticalCoverageRule rule)
        {
            TacticalUnitRuntimeState unit;
            if (!_units.TryGetValue(rule.UnitId, out unit))
            {
                return false;
            }

            if (unit.Condition != TacticalUnitCondition.Active || unit.IsInTransit)
            {
                return false;
            }

            if (unit.PositionId != rule.PositionId)
            {
                return false;
            }

            if (string.IsNullOrEmpty(rule.RequiredSurfaceId))
            {
                return true;
            }

            TacticalSurfaceState state;
            if (!_surfaces.TryGetValue(rule.RequiredSurfaceId, out state))
            {
                return false;
            }

            for (int i = 0; i < rule.AllowedSurfaceStates.Length; i++)
            {
                if (rule.AllowedSurfaceStates[i] == state)
                {
                    return true;
                }
            }

            return false;
        }

        public TacticalExplanation GetExplanation(string unitId, TacticalEvaluation evaluation)
        {
            TacticalUnitPlan plan = GetUnitPlan(unitId);
            TacticalUnitRuntimeState state = GetUnitState(unitId);
            if (plan == null || state == null)
            {
                return new TacticalExplanation
                {
                    What = "Unknown unit.",
                    WhySafe = string.Empty,
                    WhatBreaksIt = string.Empty,
                    Fallback = string.Empty
                };
            }

            if (state.Condition == TacticalUnitCondition.Down)
            {
                return new TacticalExplanation
                {
                    What = "DOWN. This operator's promise is no longer being kept.",
                    WhySafe = "Teammates must absorb or replace the missing responsibility.",
                    WhatBreaksIt = "The route remains weak until a flex operator physically reaches a repair position.",
                    Fallback = plan.FallbackText
                };
            }

            if (plan.IsFlex)
            {
                TacticalFlexDirective flex = evaluation.FlexDirective;
                if (flex.IsActive)
                {
                    TacticalRouteRuntimeState route = evaluation.GetRoute(flex.RouteId);
                    string progress = route != null && route.IsFlexHolding
                        ? "The repair is active."
                        : "Move to the marked backup position to make the repair real.";

                    return new TacticalExplanation
                    {
                        What = flex.ResponsibilityLabel + ". " + progress,
                        WhySafe = flex.Explanation,
                        WhatBreaksIt = "A second simultaneous failure can exceed one reserve operator's capacity.",
                        Fallback = plan.FallbackText
                    };
                }

                return new TacticalExplanation
                {
                    What = "Remain in reserve and replace the first broken promise.",
                    WhySafe = "All baseline responsibilities currently meet their required coverage.",
                    WhatBreaksIt = "A teammate goes down, leaves position, or loses a required sightline.",
                    Fallback = plan.FallbackText
                };
            }

            string prefix = state.IsInTransit
                ? "MOVING. Coverage from this operator is temporarily absent. "
                : string.Empty;

            return new TacticalExplanation
            {
                What = prefix + plan.WhatText,
                WhySafe = plan.WhySafeText,
                WhatBreaksIt = plan.BreakText,
                Fallback = plan.FallbackText
            };
        }

        public string GetCurrentResponsibilityLabel(string unitId, TacticalEvaluation evaluation)
        {
            TacticalUnitPlan plan = GetUnitPlan(unitId);
            if (plan == null)
            {
                return "Unknown";
            }

            TacticalUnitRuntimeState state = GetUnitState(unitId);
            if (state != null && state.Condition == TacticalUnitCondition.Down)
            {
                return "Promise broken";
            }

            if (plan.IsFlex && evaluation != null && evaluation.FlexDirective.IsActive)
            {
                return evaluation.FlexDirective.ResponsibilityLabel;
            }

            return plan.RoleLabel;
        }

        public string GetPositionLabel(string unitId)
        {
            TacticalUnitRuntimeState unit = GetUnitState(unitId);
            if (unit == null)
            {
                return "Unknown";
            }

            if (unit.IsInTransit || string.IsNullOrEmpty(unit.PositionId))
            {
                return "In transit";
            }

            TacticalPositionDefinition position = GetPosition(unit.PositionId);
            return position != null ? position.Label : unit.PositionId;
        }

        public List<string> GetActiveRulesForUnit(string unitId)
        {
            List<string> ruleIds = new List<string>();
            for (int i = 0; i < _scenario.CoverageRules.Count; i++)
            {
                TacticalCoverageRule rule = _scenario.CoverageRules[i];
                if (rule.UnitId == unitId && IsCoverageRuleActive(rule))
                {
                    ruleIds.Add(rule.Id);
                }
            }

            return ruleIds;
        }

        public TacticalPositionDefinition FindNearestPosition(Vector3 worldPosition, int floor, float maxDistance)
        {
            TacticalPositionDefinition best = null;
            float bestDistance = maxDistance;
            for (int i = 0; i < _scenario.Positions.Count; i++)
            {
                TacticalPositionDefinition candidate = _scenario.Positions[i];
                if (candidate.Floor != floor)
                {
                    continue;
                }

                float distance = Vector3.Distance(worldPosition, candidate.WorldPosition);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private TacticalFlexDirective BuildFlexDirective(
            TacticalUnitPlan flexPlan,
            Dictionary<string, int> baseline)
        {
            if (flexPlan == null)
            {
                return TacticalFlexDirective.ReturnToReserve(string.Empty, string.Empty);
            }

            TacticalUnitRuntimeState flexState = _units[flexPlan.UnitId];
            if (flexState.Condition != TacticalUnitCondition.Active)
            {
                TacticalFlexDirective unavailable = TacticalFlexDirective.ReturnToReserve(
                    flexPlan.UnitId,
                    flexPlan.HomePositionId);
                unavailable.Explanation = "The reserve operator is down and cannot repair any broken promise.";
                return unavailable;
            }

            TacticalFlexOption best = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < _scenario.FlexOptions.Count; i++)
            {
                TacticalFlexOption option = _scenario.FlexOptions[i];
                TacticalRouteDefinition route = _routes[option.RouteId];
                int deficit = route.RequiredCoverage - baseline[route.Id];
                if (deficit <= 0)
                {
                    continue;
                }

                int pressure = GetRoutePressure(route.Id);
                int score = pressure * 100 + deficit * 10 + option.Priority;
                if (option.RouteId == _lastFlexRouteId)
                {
                    score += 1;
                }

                if (!flexState.IsInTransit && flexState.PositionId == option.PositionId)
                {
                    score += 2;
                }

                if (best == null || score > bestScore)
                {
                    best = option;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                return TacticalFlexDirective.ReturnToReserve(flexPlan.UnitId, flexPlan.HomePositionId);
            }

            _lastFlexRouteId = best.RouteId;
            TacticalFlexDirective directive = new TacticalFlexDirective();
            directive.IsActive = true;
            directive.UnitId = flexPlan.UnitId;
            directive.RouteId = best.RouteId;
            directive.PositionId = best.PositionId;
            directive.ResponsibilityLabel = best.ResponsibilityLabel;
            directive.Explanation = best.Explanation;
            return directive;
        }

        private TacticalUnitPlan FindFlexPlan()
        {
            for (int i = 0; i < _scenario.Units.Count; i++)
            {
                if (_scenario.Units[i].IsFlex)
                {
                    return _scenario.Units[i];
                }
            }

            return null;
        }

        private TacticalUnitRuntimeState RequireUnit(string unitId)
        {
            TacticalUnitRuntimeState unit;
            if (!_units.TryGetValue(unitId, out unit))
            {
                throw new ArgumentException("Unknown tactical unit: " + unitId);
            }

            return unit;
        }

        private TacticalPositionDefinition RequirePosition(string positionId)
        {
            TacticalPositionDefinition position;
            if (!_positions.TryGetValue(positionId, out position))
            {
                throw new ArgumentException("Unknown tactical position: " + positionId);
            }

            return position;
        }

        private void IndexScenario()
        {
            for (int i = 0; i < _scenario.Positions.Count; i++)
            {
                _positions.Add(_scenario.Positions[i].Id, _scenario.Positions[i]);
            }

            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                _routes.Add(_scenario.Routes[i].Id, _scenario.Routes[i]);
            }

            for (int i = 0; i < _scenario.Surfaces.Count; i++)
            {
                _surfaceDefinitions.Add(_scenario.Surfaces[i].Id, _scenario.Surfaces[i]);
            }

            for (int i = 0; i < _scenario.Units.Count; i++)
            {
                _unitPlans.Add(_scenario.Units[i].UnitId, _scenario.Units[i]);
            }
        }
    }

    public static class TacticalScenarioValidator
    {
        public static List<string> Validate(TacticalScenarioDefinition scenario)
        {
            List<string> errors = new List<string>();
            if (scenario == null)
            {
                errors.Add("Scenario is null.");
                return errors;
            }

            HashSet<string> positions = CollectIds(
                scenario.Positions,
                delegate(TacticalPositionDefinition value) { return value.Id; },
                "position",
                errors);
            HashSet<string> routes = CollectIds(
                scenario.Routes,
                delegate(TacticalRouteDefinition value) { return value.Id; },
                "route",
                errors);
            HashSet<string> surfaces = CollectIds(
                scenario.Surfaces,
                delegate(TacticalSurfaceDefinition value) { return value.Id; },
                "surface",
                errors);
            HashSet<string> units = CollectIds(
                scenario.Units,
                delegate(TacticalUnitPlan value) { return value.UnitId; },
                "unit",
                errors);

            int flexCount = 0;
            for (int i = 0; i < scenario.Units.Count; i++)
            {
                TacticalUnitPlan unit = scenario.Units[i];
                RequireReference(positions, unit.HomePositionId, "Unit " + unit.UnitId + " home", errors);
                RequireReference(positions, unit.FallbackPositionId, "Unit " + unit.UnitId + " fallback", errors);
                RequireReference(positions, unit.ExecutePositionId, "Unit " + unit.UnitId + " execute", errors);
                if (unit.IsFlex)
                {
                    flexCount++;
                }
            }

            if (flexCount != 1)
            {
                errors.Add("Scenario must define exactly one flex unit; found " + flexCount + ".");
            }

            HashSet<string> ruleIds = new HashSet<string>();
            for (int i = 0; i < scenario.CoverageRules.Count; i++)
            {
                TacticalCoverageRule rule = scenario.CoverageRules[i];
                if (!ruleIds.Add(rule.Id))
                {
                    errors.Add("Duplicate coverage rule id: " + rule.Id + ".");
                }

                RequireReference(units, rule.UnitId, "Coverage rule " + rule.Id + " unit", errors);
                RequireReference(routes, rule.RouteId, "Coverage rule " + rule.Id + " route", errors);
                RequireReference(positions, rule.PositionId, "Coverage rule " + rule.Id + " position", errors);
                if (!string.IsNullOrEmpty(rule.RequiredSurfaceId))
                {
                    RequireReference(
                        surfaces,
                        rule.RequiredSurfaceId,
                        "Coverage rule " + rule.Id + " surface",
                        errors);
                    if (rule.AllowedSurfaceStates == null || rule.AllowedSurfaceStates.Length == 0)
                    {
                        errors.Add("Coverage rule " + rule.Id + " requires a surface but allows no states.");
                    }
                }
            }

            for (int i = 0; i < scenario.FlexOptions.Count; i++)
            {
                TacticalFlexOption option = scenario.FlexOptions[i];
                RequireReference(routes, option.RouteId, "Flex option route", errors);
                RequireReference(positions, option.PositionId, "Flex option position", errors);
            }

            for (int i = 0; i < scenario.Routes.Count; i++)
            {
                if (scenario.Routes[i].RequiredCoverage <= 0)
                {
                    errors.Add("Route " + scenario.Routes[i].Id + " must require at least one promise.");
                }
            }

            for (int i = 0; i < scenario.AttackPlaybooks.Count; i++)
            {
                TacticalAttackPlaybook playbook = scenario.AttackPlaybooks[i];
                float previousTime = -1f;
                for (int stepIndex = 0; stepIndex < playbook.Steps.Count; stepIndex++)
                {
                    TacticalAttackStep step = playbook.Steps[stepIndex];
                    if (step.Time < previousTime)
                    {
                        errors.Add("Playbook " + playbook.Id + " steps are not sorted by time.");
                    }

                    previousTime = step.Time;
                    if (step.Action == TacticalAttackAction.SetPressure)
                    {
                        RequireReference(routes, step.TargetId, "Playbook pressure target", errors);
                    }
                    else if (step.Action == TacticalAttackAction.SetSurfaceState)
                    {
                        RequireReference(surfaces, step.TargetId, "Playbook surface target", errors);
                    }
                    else if (step.Action == TacticalAttackAction.SetUnitDown)
                    {
                        RequireReference(units, step.TargetId, "Playbook unit target", errors);
                    }
                }
            }

            return errors;
        }

        private static HashSet<string> CollectIds<T>(
            List<T> values,
            Func<T, string> idSelector,
            string kind,
            List<string> errors)
        {
            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < values.Count; i++)
            {
                string id = idSelector(values[i]);
                if (string.IsNullOrEmpty(id))
                {
                    errors.Add("A " + kind + " has an empty id.");
                }
                else if (!ids.Add(id))
                {
                    errors.Add("Duplicate " + kind + " id: " + id + ".");
                }
            }

            return ids;
        }

        private static void RequireReference(
            HashSet<string> ids,
            string id,
            string context,
            List<string> errors)
        {
            if (string.IsNullOrEmpty(id) || !ids.Contains(id))
            {
                errors.Add(context + " references unknown id '" + id + "'.");
            }
        }
    }
}

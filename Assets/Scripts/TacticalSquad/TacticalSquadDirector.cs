using System.Collections.Generic;
using UnityEngine;

namespace Sipoga.Tactics
{
    public enum TacticalPrototypePhase
    {
        Planning,
        Execution,
        AfterAction
    }

    public sealed class TacticalEventRecord
    {
        public float Time;
        public string Text;

        public TacticalEventRecord(float time, string text)
        {
            Time = time;
            Text = text;
        }
    }

    public sealed class TacticalSquadDirector : MonoBehaviour
    {
        private readonly Dictionary<string, TacticalUnitAgent> _agents =
            new Dictionary<string, TacticalUnitAgent>();
        private readonly List<TacticalEventRecord> _events =
            new List<TacticalEventRecord>();
        private readonly Dictionary<string, string> _lastRouteStatus =
            new Dictionary<string, string>();

        private TacticalScenarioDefinition _scenario;
        private TacticalPrototypeWorld _world;
        private TacticalPrototypeCamera _cameraController;
        private TacticalStateEngine _engine;
        private TacticalEvaluation _evaluation;
        private TacticalMissionScorer _missionScorer;
        private TacticalPrototypePhase _phase = TacticalPrototypePhase.Planning;
        private TacticalControlMode _controlMode = TacticalControlMode.Guided;
        private int _playbookIndex;
        private int _nextAttackStep;
        private float _executionTime;
        private string _selectedUnitId = "alpha";
        private string _lastFlexDirectiveKey = string.Empty;
        private bool _initialized;

        public TacticalStateEngine Engine
        {
            get { return _engine; }
        }

        public TacticalEvaluation Evaluation
        {
            get { return _evaluation; }
        }

        public TacticalScenarioDefinition Scenario
        {
            get { return _scenario; }
        }

        public TacticalPrototypePhase Phase
        {
            get { return _phase; }
        }

        public TacticalControlMode ControlMode
        {
            get { return _controlMode; }
        }

        public float ExecutionTime
        {
            get { return _executionTime; }
        }

        public string SelectedUnitId
        {
            get { return _selectedUnitId; }
        }

        public IList<TacticalEventRecord> Events
        {
            get { return _events; }
        }

        public TacticalMissionReport MissionReport
        {
            get
            {
                return _missionScorer != null
                    ? _missionScorer.BuildReport(_executionTime)
                    : null;
            }
        }

        public TacticalAttackPlaybook CurrentPlaybook
        {
            get
            {
                if (_scenario == null || _scenario.AttackPlaybooks.Count == 0)
                {
                    return null;
                }

                return _scenario.AttackPlaybooks[_playbookIndex];
            }
        }

        public void Initialize(TacticalScenarioDefinition scenario, TacticalPrototypeWorld world)
        {
            _scenario = scenario;
            _world = world;
            _engine = new TacticalStateEngine(scenario);
            _evaluation = _engine.Evaluate();
            _missionScorer = new TacticalMissionScorer(scenario);
            _initialized = true;
            ResetCurrentPlaybook();
        }

        public void RegisterAgent(TacticalUnitAgent agent)
        {
            if (agent == null || string.IsNullOrEmpty(agent.UnitId))
            {
                return;
            }

            _agents[agent.UnitId] = agent;
            agent.SetSelected(agent.UnitId == _selectedUnitId);
        }

        public void SetCameraController(TacticalPrototypeCamera cameraController)
        {
            _cameraController = cameraController;
            TacticalUnitAgent selected = GetAgent(_selectedUnitId);
            if (selected != null)
            {
                _cameraController.SetSelectedAgent(selected);
            }
        }

        public TacticalUnitAgent GetAgent(string unitId)
        {
            TacticalUnitAgent agent;
            return _agents.TryGetValue(unitId, out agent) ? agent : null;
        }

        public void SelectUnit(string unitId)
        {
            if (!_agents.ContainsKey(unitId))
            {
                return;
            }

            TacticalUnitAgent previous = GetAgent(_selectedUnitId);
            if (previous != null)
            {
                previous.SetSelected(false);
            }

            _selectedUnitId = unitId;
            TacticalUnitAgent selected = GetAgent(_selectedUnitId);
            selected.SetSelected(true);
            if (_cameraController != null)
            {
                _cameraController.SetSelectedAgent(selected);
            }
        }

        public void SelectUnitByIndex(int index)
        {
            if (_scenario == null || index < 0 || index >= _scenario.Units.Count)
            {
                return;
            }

            SelectUnit(_scenario.Units[index].UnitId);
        }

        public void ToggleView()
        {
            if (_cameraController != null)
            {
                _cameraController.ToggleMode();
            }
        }

        public void ToggleControlMode()
        {
            _controlMode = _controlMode == TacticalControlMode.Guided
                ? TacticalControlMode.Command
                : TacticalControlMode.Guided;

            if (_controlMode == TacticalControlMode.Guided)
            {
                AddEvent("GUIDED MODE: the reserve automatically follows the highest-pressure broken promise.");
                Recalculate(true, true);
            }
            else
            {
                AddEvent("COMMAND MODE: Foxtrot waits for your route order. Use the command bar or press Q.");
                Recalculate(false, true);
            }
        }

        public void StartOrRestartExecution()
        {
            if (_phase != TacticalPrototypePhase.Planning)
            {
                ResetCurrentPlaybook();
            }

            _phase = TacticalPrototypePhase.Execution;
            _executionTime = 0f;
            _nextAttackStep = 0;
            _missionScorer.Begin();
            AddEvent("EXECUTE: " + CurrentPlaybook.Name + ". " + CurrentPlaybook.Summary);
        }

        public void ResetCurrentPlaybook()
        {
            if (!_initialized)
            {
                return;
            }

            _engine.Reset();
            _missionScorer.Reset();
            _phase = TacticalPrototypePhase.Planning;
            _executionTime = 0f;
            _nextAttackStep = 0;
            _events.Clear();
            _lastRouteStatus.Clear();
            _lastFlexDirectiveKey = "reserve";

            for (int i = 0; i < _scenario.Units.Count; i++)
            {
                TacticalUnitPlan plan = _scenario.Units[i];
                TacticalUnitAgent agent = GetAgent(plan.UnitId);
                if (agent != null)
                {
                    agent.SetDown(false);
                    agent.Warp(_engine.GetPosition(plan.HomePositionId));
                }
            }

            for (int i = 0; i < _scenario.Surfaces.Count; i++)
            {
                TacticalSurfaceDefinition surface = _scenario.Surfaces[i];
                if (_world != null)
                {
                    _world.UpdateSurface(surface.Id, surface.DefaultState);
                }
            }

            Recalculate(false, false);
            AddEvent(
                "PLAN READY: " + CurrentPlaybook.Name +
                ". Press Space or Begin to run the attack in " +
                _controlMode.ToString().ToUpperInvariant() + " mode.");
        }

        public void NextPlaybook()
        {
            if (_scenario == null || _scenario.AttackPlaybooks.Count == 0)
            {
                return;
            }

            _playbookIndex = (_playbookIndex + 1) % _scenario.AttackPlaybooks.Count;
            ResetCurrentPlaybook();
        }

        public void PreviousPlaybook()
        {
            if (_scenario == null || _scenario.AttackPlaybooks.Count == 0)
            {
                return;
            }

            _playbookIndex--;
            if (_playbookIndex < 0)
            {
                _playbookIndex = _scenario.AttackPlaybooks.Count - 1;
            }

            ResetCurrentPlaybook();
        }

        public void OrderSelectedFallback()
        {
            TacticalUnitPlan plan = _engine.GetUnitPlan(_selectedUnitId);
            if (plan == null)
            {
                return;
            }

            OrderUnitTo(_selectedUnitId, plan.FallbackPositionId, plan.Callsign + " falls back.");
        }

        public void OrderSelectedToAssignment()
        {
            TacticalUnitPlan plan = _engine.GetUnitPlan(_selectedUnitId);
            if (plan == null)
            {
                return;
            }

            string targetPositionId = plan.HomePositionId;
            if (plan.IsFlex && _evaluation.FlexDirective.IsActive)
            {
                targetPositionId = _evaluation.FlexDirective.PositionId;
            }

            OrderUnitTo(
                _selectedUnitId,
                targetPositionId,
                plan.Callsign + " returns to the assigned promise.");
        }

        public void OrderFlexToRecommended()
        {
            TacticalFlexDirective directive = _evaluation != null
                ? _evaluation.FlexDirective
                : null;
            if (directive == null || !directive.IsActive)
            {
                TacticalUnitPlan flexPlan = _engine.GetUnitPlan(GlasshouseScenario.FlexUnitId);
                if (flexPlan != null)
                {
                    OrderUnitTo(
                        GlasshouseScenario.FlexUnitId,
                        flexPlan.HomePositionId,
                        "COMMAND: no broken promise. Foxtrot returns to reserve.");
                }
                return;
            }

            OrderFlexToRoute(directive.RouteId);
        }

        public void OrderFlexToRoute(string routeId)
        {
            TacticalFlexOption option = FindFlexOption(routeId);
            TacticalUnitAgent flexAgent = GetAgent(GlasshouseScenario.FlexUnitId);
            if (option == null || flexAgent == null || flexAgent.IsDown)
            {
                return;
            }

            if (_phase == TacticalPrototypePhase.Execution)
            {
                _missionScorer.RecordCommand(routeId, _evaluation.FlexDirective);
            }

            TacticalRouteDefinition route = _engine.GetRouteDefinition(routeId);
            string routeLabel = route != null ? route.Label : routeId;
            AddEvent("COMMAND: FOXTROT -> " + routeLabel + ".");
            OrderUnitToInternal(GlasshouseScenario.FlexUnitId, option.PositionId);
            Recalculate(false, true);
        }

        public void ToggleSelectedDown()
        {
            TacticalUnitRuntimeState state = _engine.GetUnitState(_selectedUnitId);
            if (state == null)
            {
                return;
            }

            bool down = state.Condition != TacticalUnitCondition.Down;
            SetUnitDown(_selectedUnitId, down, true);
        }

        public void CyclePermeableScreen()
        {
            TacticalSurfaceState current = _engine.GetSurfaceState(GlasshouseScenario.PermeableScreenId);
            TacticalSurfaceState next;
            if (current == TacticalSurfaceState.Permeable)
            {
                next = TacticalSurfaceState.Sealed;
            }
            else if (current == TacticalSurfaceState.Sealed)
            {
                next = TacticalSurfaceState.Open;
            }
            else
            {
                next = TacticalSurfaceState.Permeable;
            }

            SetSurfaceState(
                GlasshouseScenario.PermeableScreenId,
                next,
                "Overwatch screen changed to " + next.ToString().ToUpperInvariant() + ".");
        }

        public void TriggerGoCode()
        {
            AddEvent("GO CODE: Collapse toward the objective thresholds.");
            for (int i = 0; i < _scenario.Units.Count; i++)
            {
                TacticalUnitPlan plan = _scenario.Units[i];
                TacticalUnitRuntimeState state = _engine.GetUnitState(plan.UnitId);
                if (state != null && state.Condition == TacticalUnitCondition.Active)
                {
                    OrderUnitToInternal(plan.UnitId, plan.ExecutePositionId);
                }
            }

            Recalculate(false, true);
        }

        public void NotifyUnitArrived(string unitId, string positionId)
        {
            _engine.SetUnitPosition(unitId, positionId);
            TacticalUnitPlan plan = _engine.GetUnitPlan(unitId);
            TacticalPositionDefinition position = _engine.GetPosition(positionId);
            if (plan != null && position != null)
            {
                AddEvent(plan.Callsign + " reaches " + position.Label + ".");
            }

            Recalculate(true, true);
        }

        public void NotifyUnitLeftPosition(string unitId)
        {
            _engine.MarkUnitInTransit(unitId);
            Recalculate(true, true);
        }

        public void NotifyManualMovementEnded(TacticalUnitAgent agent)
        {
            if (agent == null)
            {
                return;
            }

            TacticalPositionDefinition nearest = _engine.FindNearestPosition(
                agent.transform.position,
                agent.CurrentFloor,
                1.0f);
            if (nearest != null)
            {
                agent.Warp(nearest);
                _engine.SetUnitPosition(agent.UnitId, nearest.Id);
                AddEvent(agent.Plan.Callsign + " settles at " + nearest.Label + ".");
            }

            Recalculate(true, true);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            HandleKeyboardInput();

            if (_phase == TacticalPrototypePhase.Execution)
            {
                _executionTime += Time.deltaTime;
                TacticalAttackPlaybook playbook = CurrentPlaybook;
                while (_nextAttackStep < playbook.Steps.Count &&
                       playbook.Steps[_nextAttackStep].Time <= _executionTime)
                {
                    ApplyAttackStep(playbook.Steps[_nextAttackStep]);
                    _nextAttackStep++;
                }

                _missionScorer.Tick(Time.deltaTime, _executionTime, _evaluation);

                if (_executionTime >= playbook.Duration)
                {
                    _missionScorer.Finish(_evaluation);
                    _phase = TacticalPrototypePhase.AfterAction;
                    TacticalMissionReport report = MissionReport;
                    AddEvent(
                        "RESULT " + report.Grade + ": " +
                        report.ControlPercent.ToString("0") + "% map control preserved.");
                    AddEvent("AFTER ACTION: " + playbook.Lesson);
                }
            }
        }

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectUnitByIndex(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectUnitByIndex(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectUnitByIndex(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectUnitByIndex(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SelectUnitByIndex(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) SelectUnitByIndex(5);

            if (Input.GetKeyDown(KeyCode.Tab)) ToggleView();
            if (Input.GetKeyDown(KeyCode.Space)) StartOrRestartExecution();
            if (Input.GetKeyDown(KeyCode.R)) ResetCurrentPlaybook();
            if (Input.GetKeyDown(KeyCode.P)) NextPlaybook();
            if (Input.GetKeyDown(KeyCode.LeftBracket)) PreviousPlaybook();
            if (Input.GetKeyDown(KeyCode.RightBracket)) NextPlaybook();
            if (Input.GetKeyDown(KeyCode.F)) OrderSelectedFallback();
            if (Input.GetKeyDown(KeyCode.H)) OrderSelectedToAssignment();
            if (Input.GetKeyDown(KeyCode.X)) ToggleSelectedDown();
            if (Input.GetKeyDown(KeyCode.B)) CyclePermeableScreen();
            if (Input.GetKeyDown(KeyCode.G)) TriggerGoCode();
            if (Input.GetKeyDown(KeyCode.M)) ToggleControlMode();
            if (Input.GetKeyDown(KeyCode.Q)) OrderFlexToRecommended();

            if (_cameraController == null || _cameraController.IsTacticalView)
            {
                return;
            }

            float horizontal = 0f;
            float vertical = 0f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;

            Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
            if (input.sqrMagnitude > 0.001f)
            {
                TacticalUnitAgent selected = GetAgent(_selectedUnitId);
                if (selected != null)
                {
                    Vector3 direction =
                        _cameraController.PlanarRight * input.x +
                        _cameraController.PlanarForward * input.y;
                    selected.ManualMove(direction, 4.3f);
                }
            }
        }

        private void ApplyAttackStep(TacticalAttackStep step)
        {
            switch (step.Action)
            {
                case TacticalAttackAction.SetPressure:
                    _engine.SetRoutePressure(step.TargetId, step.IntValue);
                    AddEvent(step.Message);
                    Recalculate(true, true);
                    break;

                case TacticalAttackAction.SetSurfaceState:
                    SetSurfaceState(step.TargetId, step.SurfaceState, step.Message);
                    break;

                case TacticalAttackAction.SetUnitDown:
                    if (!string.IsNullOrEmpty(step.Message))
                    {
                        AddEvent(step.Message);
                    }
                    SetUnitDown(step.TargetId, step.BoolValue, false);
                    break;

                default:
                    AddEvent(step.Message);
                    break;
            }
        }

        private void SetUnitDown(string unitId, bool down, bool addDefaultLog)
        {
            _engine.SetUnitDown(unitId, down);
            TacticalUnitAgent agent = GetAgent(unitId);
            if (agent != null)
            {
                agent.SetDown(down);
            }

            if (addDefaultLog)
            {
                TacticalUnitPlan plan = _engine.GetUnitPlan(unitId);
                AddEvent(plan.Callsign + (down ? " goes down." : " is restored for testing."));
            }

            Recalculate(true, true);
        }

        private void SetSurfaceState(string surfaceId, TacticalSurfaceState state, string message)
        {
            _engine.SetSurfaceState(surfaceId, state);
            if (_world != null)
            {
                _world.UpdateSurface(surfaceId, state);
            }

            if (!string.IsNullOrEmpty(message))
            {
                AddEvent(message);
            }

            Recalculate(true, true);
        }

        private void OrderUnitTo(string unitId, string positionId, string message)
        {
            TacticalUnitRuntimeState state = _engine.GetUnitState(unitId);
            if (state == null || state.Condition == TacticalUnitCondition.Down)
            {
                return;
            }

            if (!string.IsNullOrEmpty(message))
            {
                AddEvent(message);
            }

            OrderUnitToInternal(unitId, positionId);
            Recalculate(false, true);
        }

        private void OrderUnitToInternal(string unitId, string positionId)
        {
            TacticalUnitAgent agent = GetAgent(unitId);
            TacticalPositionDefinition target = _engine.GetPosition(positionId);
            if (agent == null || target == null)
            {
                return;
            }

            TacticalUnitRuntimeState state = _engine.GetUnitState(unitId);
            if (state != null && !state.IsInTransit && state.PositionId == positionId)
            {
                return;
            }

            if (agent.IsMoving && agent.TargetPositionId == positionId)
            {
                return;
            }

            _engine.MarkUnitInTransit(unitId);
            agent.OrderTo(target);
        }

        private void Recalculate(bool synchronizeFlex, bool logTransitions)
        {
            _evaluation = _engine.Evaluate();
            if (synchronizeFlex)
            {
                SynchronizeFlexOrder(logTransitions);
                _evaluation = _engine.Evaluate();
            }

            if (logTransitions)
            {
                LogRouteTransitions();
            }
            else
            {
                SnapshotRouteStatuses();
            }
        }

        private void SynchronizeFlexOrder(bool logTransitions)
        {
            TacticalUnitPlan flexPlan = _engine.GetUnitPlan(GlasshouseScenario.FlexUnitId);
            TacticalUnitAgent flexAgent = GetAgent(GlasshouseScenario.FlexUnitId);
            if (flexPlan == null || flexAgent == null || flexAgent.IsDown)
            {
                return;
            }

            TacticalFlexDirective directive = _evaluation.FlexDirective;
            string directiveKey = directive.IsActive ? directive.RouteId : "reserve";
            if (directiveKey != _lastFlexDirectiveKey)
            {
                if (logTransitions)
                {
                    if (directive.IsActive)
                    {
                        TacticalRouteDefinition route = _engine.GetRouteDefinition(directive.RouteId);
                        string prefix = _controlMode == TacticalControlMode.Guided
                            ? "FOXTROT REASSIGNED: repair "
                            : "RESERVE RECOMMENDATION: send Foxtrot to ";
                        AddEvent(prefix + route.Label + ".");
                    }
                    else
                    {
                        string message = _controlMode == TacticalControlMode.Guided
                            ? "FOXTROT RELEASED: all baseline promises are intact."
                            : "RESERVE CLEAR: all baseline promises are intact.";
                        AddEvent(message);
                    }
                }

                _lastFlexDirectiveKey = directiveKey;
            }

            if (_controlMode == TacticalControlMode.Command)
            {
                return;
            }

            bool playerIsManuallyDrivingFlex =
                _selectedUnitId == GlasshouseScenario.FlexUnitId &&
                _cameraController != null &&
                !_cameraController.IsTacticalView &&
                flexAgent.IsManual;
            if (playerIsManuallyDrivingFlex)
            {
                return;
            }

            string targetPositionId = directive.IsActive
                ? directive.PositionId
                : flexPlan.HomePositionId;
            OrderUnitToInternal(GlasshouseScenario.FlexUnitId, targetPositionId);
        }

        private void LogRouteTransitions()
        {
            for (int i = 0; i < _evaluation.Routes.Count; i++)
            {
                TacticalRouteRuntimeState routeState = _evaluation.Routes[i];
                string current = routeState.StatusLabel + "-" + routeState.Coverage;
                string previous;
                if (_lastRouteStatus.TryGetValue(routeState.RouteId, out previous) && previous != current)
                {
                    TacticalRouteDefinition route = _engine.GetRouteDefinition(routeState.RouteId);
                    AddEvent(
                        route.Label + " is " + routeState.StatusLabel +
                        " (" + routeState.Coverage + "/" + routeState.RequiredCoverage + " promises).");
                }

                _lastRouteStatus[routeState.RouteId] = current;
            }
        }

        private void SnapshotRouteStatuses()
        {
            _lastRouteStatus.Clear();
            for (int i = 0; i < _evaluation.Routes.Count; i++)
            {
                TacticalRouteRuntimeState routeState = _evaluation.Routes[i];
                _lastRouteStatus[routeState.RouteId] =
                    routeState.StatusLabel + "-" + routeState.Coverage;
            }
        }

        private TacticalFlexOption FindFlexOption(string routeId)
        {
            if (_scenario == null)
            {
                return null;
            }

            for (int i = 0; i < _scenario.FlexOptions.Count; i++)
            {
                if (_scenario.FlexOptions[i].RouteId == routeId)
                {
                    return _scenario.FlexOptions[i];
                }
            }

            return null;
        }

        private void AddEvent(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _events.Add(new TacticalEventRecord(_executionTime, text));
            const int maxEvents = 12;
            while (_events.Count > maxEvents)
            {
                _events.RemoveAt(0);
            }
        }
    }
}

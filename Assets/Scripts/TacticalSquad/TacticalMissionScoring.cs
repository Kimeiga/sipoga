using System.Collections.Generic;
using UnityEngine;

namespace Sipoga.Tactics
{
    public enum TacticalControlMode
    {
        Guided,
        Command
    }

    public sealed class TacticalMissionReport
    {
        public float ControlPercent;
        public float WeightedThreatSeconds;
        public float WeightedExposureSeconds;
        public float LongestBreakSeconds;
        public int RepairCount;
        public int CommandCount;
        public int CorrectCommandCount;
        public int UnansweredRoutes;
        public string Grade;
        public string Summary;
        public string Recommendation;

        public float CommandAccuracy
        {
            get
            {
                return CommandCount <= 0
                    ? 0f
                    : CorrectCommandCount / (float)CommandCount;
            }
        }
    }

    /// <summary>
    /// Converts the tactical state graph into an understandable mission result.
    /// A route only costs score while attackers are applying pressure, and the
    /// penalty is proportional to both pressure and the fraction of missing promises.
    /// </summary>
    public sealed class TacticalMissionScorer
    {
        private readonly TacticalScenarioDefinition _scenario;
        private readonly Dictionary<string, float> _routeExposure =
            new Dictionary<string, float>();
        private readonly Dictionary<string, float> _breakStartedAt =
            new Dictionary<string, float>();

        private float _weightedThreatSeconds;
        private float _weightedExposureSeconds;
        private float _longestBreakSeconds;
        private int _repairCount;
        private int _commandCount;
        private int _correctCommandCount;
        private bool _running;
        private bool _finished;
        private TacticalEvaluation _lastEvaluation;

        public TacticalMissionScorer(TacticalScenarioDefinition scenario)
        {
            _scenario = scenario;
            Reset();
        }

        public void Reset()
        {
            _routeExposure.Clear();
            _breakStartedAt.Clear();
            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                _routeExposure[_scenario.Routes[i].Id] = 0f;
            }

            _weightedThreatSeconds = 0f;
            _weightedExposureSeconds = 0f;
            _longestBreakSeconds = 0f;
            _repairCount = 0;
            _commandCount = 0;
            _correctCommandCount = 0;
            _running = false;
            _finished = false;
            _lastEvaluation = null;
        }

        public void Begin()
        {
            _running = true;
            _finished = false;
        }

        public void Tick(float deltaTime, float executionTime, TacticalEvaluation evaluation)
        {
            if (!_running || _finished || evaluation == null || deltaTime <= 0f)
            {
                return;
            }

            _lastEvaluation = evaluation;
            float intervalStart = Mathf.Max(0f, executionTime - deltaTime);
            for (int i = 0; i < evaluation.Routes.Count; i++)
            {
                TacticalRouteRuntimeState route = evaluation.Routes[i];
                float pressure = Mathf.Max(0f, route.Pressure);
                if (pressure <= 0f)
                {
                    CloseBreak(route.RouteId, intervalStart, false);
                    continue;
                }

                _weightedThreatSeconds += pressure * deltaTime;
                int missingPromises = Mathf.Max(0, route.RequiredCoverage - route.Coverage);
                float deficitFraction = route.RequiredCoverage <= 0
                    ? 0f
                    : missingPromises / (float)route.RequiredCoverage;
                float exposure = pressure * deficitFraction * deltaTime;
                _weightedExposureSeconds += exposure;
                _routeExposure[route.RouteId] = _routeExposure[route.RouteId] + exposure;

                if (missingPromises > 0)
                {
                    float startedAt;
                    if (!_breakStartedAt.TryGetValue(route.RouteId, out startedAt))
                    {
                        startedAt = intervalStart;
                        _breakStartedAt[route.RouteId] = startedAt;
                    }

                    _longestBreakSeconds = Mathf.Max(
                        _longestBreakSeconds,
                        Mathf.Max(0f, executionTime - startedAt));
                }
                else
                {
                    CloseBreak(route.RouteId, intervalStart, true);
                }
            }
        }

        public void RecordCommand(string routeId, TacticalFlexDirective recommended)
        {
            _commandCount++;
            if (recommended != null && recommended.IsActive && recommended.RouteId == routeId)
            {
                _correctCommandCount++;
            }
        }

        public void Finish(TacticalEvaluation evaluation)
        {
            _lastEvaluation = evaluation;
            _running = false;
            _finished = true;
        }

        public TacticalMissionReport BuildReport(float executionTime)
        {
            TacticalMissionReport report = new TacticalMissionReport();
            report.WeightedThreatSeconds = _weightedThreatSeconds;
            report.WeightedExposureSeconds = _weightedExposureSeconds;
            report.LongestBreakSeconds = GetLongestBreakIncludingOpen(executionTime);
            report.RepairCount = _repairCount;
            report.CommandCount = _commandCount;
            report.CorrectCommandCount = _correctCommandCount;
            report.UnansweredRoutes = CountUnansweredRoutes(_lastEvaluation);
            report.ControlPercent = _weightedThreatSeconds <= 0.001f
                ? 100f
                : Mathf.Clamp01(1f - _weightedExposureSeconds / _weightedThreatSeconds) * 100f;

            float gradeScore = report.ControlPercent;
            gradeScore -= Mathf.Max(0f, report.LongestBreakSeconds - 3f) * 1.25f;
            gradeScore -= report.UnansweredRoutes * 8f;
            if (report.CommandCount > 0)
            {
                gradeScore -= (1f - report.CommandAccuracy) * 8f;
            }

            report.Grade = GradeFor(gradeScore);
            report.Summary =
                "You preserved " + report.ControlPercent.ToString("0") +
                "% of pressure-weighted map control. " +
                report.RepairCount + " route repair" +
                (report.RepairCount == 1 ? string.Empty : "s") +
                ", longest active break " + report.LongestBreakSeconds.ToString("0.0") + "s.";
            report.Recommendation = BuildRecommendation(report);
            return report;
        }

        private float GetLongestBreakIncludingOpen(float executionTime)
        {
            float longest = _longestBreakSeconds;
            foreach (KeyValuePair<string, float> pair in _breakStartedAt)
            {
                longest = Mathf.Max(longest, Mathf.Max(0f, executionTime - pair.Value));
            }

            return longest;
        }

        private int CountUnansweredRoutes(TacticalEvaluation evaluation)
        {
            if (evaluation == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < evaluation.Routes.Count; i++)
            {
                TacticalRouteRuntimeState route = evaluation.Routes[i];
                if (route.Pressure > 0 && !route.IsSecured)
                {
                    count++;
                }
            }

            return count;
        }

        private string BuildRecommendation(TacticalMissionReport report)
        {
            string worstRouteId = string.Empty;
            float worstExposure = 0f;
            foreach (KeyValuePair<string, float> pair in _routeExposure)
            {
                if (pair.Value > worstExposure)
                {
                    worstExposure = pair.Value;
                    worstRouteId = pair.Key;
                }
            }

            if (string.IsNullOrEmpty(worstRouteId) || worstExposure <= 0.01f)
            {
                return "No pressured route accumulated meaningful exposure. Try Command mode and deliberately break a promise to study the repair loop.";
            }

            TacticalRouteDefinition route = FindRoute(worstRouteId);
            string routeLabel = route != null ? route.Label : worstRouteId;
            string commandNote = report.CommandCount == 0
                ? " Issue the reserve order as soon as the route turns orange."
                : report.CommandAccuracy < 0.75f
                    ? " Watch the recommended route before committing the reserve."
                    : " Your target choice was sound; shorten the travel delay.";
            return routeLabel + " created the most exposure." + commandNote;
        }

        private TacticalRouteDefinition FindRoute(string routeId)
        {
            for (int i = 0; i < _scenario.Routes.Count; i++)
            {
                if (_scenario.Routes[i].Id == routeId)
                {
                    return _scenario.Routes[i];
                }
            }

            return null;
        }

        private void CloseBreak(string routeId, float closedAt, bool countAsRepair)
        {
            float startedAt;
            if (_breakStartedAt.TryGetValue(routeId, out startedAt))
            {
                _longestBreakSeconds = Mathf.Max(
                    _longestBreakSeconds,
                    Mathf.Max(0f, closedAt - startedAt));
                _breakStartedAt.Remove(routeId);
                if (countAsRepair)
                {
                    _repairCount++;
                }
            }
        }

        private static string GradeFor(float score)
        {
            if (score >= 95f) return "S";
            if (score >= 87f) return "A";
            if (score >= 74f) return "B";
            if (score >= 58f) return "C";
            return "D";
        }
    }
}

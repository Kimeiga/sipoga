using NUnit.Framework;

namespace Sipoga.Tactics.Tests
{
    public sealed class TacticalMissionScorerTests
    {
        private TacticalScenarioDefinition _scenario;
        private TacticalStateEngine _engine;
        private TacticalMissionScorer _scorer;

        [SetUp]
        public void SetUp()
        {
            _scenario = GlasshouseScenario.Create();
            _engine = new TacticalStateEngine(_scenario);
            _scorer = new TacticalMissionScorer(_scenario);
        }

        [Test]
        public void SecuredPressure_PreservesPerfectControl()
        {
            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 6);
            TacticalEvaluation evaluation = _engine.Evaluate();

            _scorer.Begin();
            _scorer.Tick(5f, 5f, evaluation);
            _scorer.Finish(evaluation);
            TacticalMissionReport report = _scorer.BuildReport(5f);

            Assert.That(report.ControlPercent, Is.EqualTo(100f).Within(0.01f));
            Assert.That(report.WeightedExposureSeconds, Is.EqualTo(0f).Within(0.01f));
            Assert.That(report.Grade, Is.EqualTo("S"));
        }

        [Test]
        public void BrokenPromise_AccumulatesExposureUntilPhysicalRepair()
        {
            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 6);
            _engine.SetUnitDown("charlie", true);
            TacticalEvaluation broken = _engine.Evaluate();

            _scorer.Begin();
            _scorer.Tick(2f, 2f, broken);

            _engine.SetUnitPosition(GlasshouseScenario.FlexUnitId, "flex_crimson");
            TacticalEvaluation repaired = _engine.Evaluate();
            _scorer.Tick(1f, 3f, repaired);
            _scorer.Finish(repaired);
            TacticalMissionReport report = _scorer.BuildReport(3f);

            Assert.That(report.ControlPercent, Is.EqualTo(77.777f).Within(0.1f));
            Assert.That(report.RepairCount, Is.EqualTo(1));
            Assert.That(report.LongestBreakSeconds, Is.EqualTo(2f).Within(0.01f));
            StringAssert.Contains("CRIMSON STAIR", report.Recommendation);
        }

        [Test]
        public void ReserveCommands_RecordWhetherTheRecommendedRouteWasChosen()
        {
            _engine.SetUnitDown("charlie", true);
            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 7);
            TacticalEvaluation evaluation = _engine.Evaluate();

            _scorer.Begin();
            _scorer.RecordCommand(GlasshouseScenario.ServiceRouteId, evaluation.FlexDirective);
            _scorer.RecordCommand(GlasshouseScenario.CrimsonRouteId, evaluation.FlexDirective);
            _scorer.Finish(evaluation);
            TacticalMissionReport report = _scorer.BuildReport(0f);

            Assert.That(report.CommandCount, Is.EqualTo(2));
            Assert.That(report.CorrectCommandCount, Is.EqualTo(1));
            Assert.That(report.CommandAccuracy, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void EndingWithPressuredBrokenRoute_ReportsUnansweredThreat()
        {
            _engine.SetUnitDown("delta", true);
            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 5);
            _engine.SetRoutePressure(GlasshouseScenario.ServiceRouteId, 5);
            TacticalEvaluation evaluation = _engine.Evaluate();

            _scorer.Begin();
            _scorer.Tick(1f, 1f, evaluation);
            _scorer.Finish(evaluation);
            TacticalMissionReport report = _scorer.BuildReport(1f);

            Assert.That(report.UnansweredRoutes, Is.EqualTo(2));
            Assert.That(report.ControlPercent, Is.LessThan(100f));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;

namespace Sipoga.Tactics.Tests
{
    public sealed class TacticalStateEngineTests
    {
        private TacticalScenarioDefinition _scenario;
        private TacticalStateEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _scenario = GlasshouseScenario.Create();
            _engine = new TacticalStateEngine(_scenario);
        }

        [Test]
        public void GlasshouseScenario_HasNoBrokenReferences()
        {
            List<string> errors = TacticalScenarioValidator.Validate(_scenario);
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
        }

        [Test]
        public void InitialPlan_MeetsEveryRequiredPromiseCount()
        {
            TacticalEvaluation evaluation = _engine.Evaluate();

            AssertRoute(evaluation, GlasshouseScenario.CrimsonRouteId, 3, 3, true);
            AssertRoute(evaluation, GlasshouseScenario.ServiceRouteId, 2, 2, true);
            AssertRoute(evaluation, GlasshouseScenario.ColdHatchRouteId, 1, 1, true);
            Assert.That(evaluation.FlexDirective.IsActive, Is.False);
            Assert.That(evaluation.FlexDirective.PositionId, Is.EqualTo("foxtrot_reserve"));
        }

        [Test]
        public void GalleryLoss_AssignsFlexAndPhysicalArrivalRepairsCrimson()
        {
            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 6);
            _engine.SetUnitDown("charlie", true);

            TacticalEvaluation beforeArrival = _engine.Evaluate();
            TacticalRouteRuntimeState broken = beforeArrival.GetRoute(GlasshouseScenario.CrimsonRouteId);
            Assert.That(broken.BaselineCoverage, Is.EqualTo(2));
            Assert.That(broken.IsSecured, Is.False);
            Assert.That(broken.IsBeingRepaired, Is.True);
            Assert.That(beforeArrival.FlexDirective.RouteId, Is.EqualTo(GlasshouseScenario.CrimsonRouteId));
            Assert.That(beforeArrival.FlexDirective.PositionId, Is.EqualTo("flex_crimson"));

            _engine.SetUnitPosition(GlasshouseScenario.FlexUnitId, "flex_crimson");
            TacticalEvaluation afterArrival = _engine.Evaluate();
            TacticalRouteRuntimeState repaired = afterArrival.GetRoute(GlasshouseScenario.CrimsonRouteId);
            Assert.That(repaired.Coverage, Is.EqualTo(3));
            Assert.That(repaired.IsSecured, Is.True);
            Assert.That(repaired.IsFlexHolding, Is.True);
            Assert.That(repaired.StatusLabel, Is.EqualTo("REPAIRED"));
        }

        [Test]
        public void SealingScreen_DeletesRemoteControlWithoutDowningBravo()
        {
            _engine.SetRoutePressure(GlasshouseScenario.ColdHatchRouteId, 5);
            _engine.SetSurfaceState(
                GlasshouseScenario.PermeableScreenId,
                TacticalSurfaceState.Sealed);

            TacticalEvaluation evaluation = _engine.Evaluate();
            TacticalRouteRuntimeState cold = evaluation.GetRoute(GlasshouseScenario.ColdHatchRouteId);
            Assert.That(_engine.GetUnitState("bravo").Condition, Is.EqualTo(TacticalUnitCondition.Active));
            Assert.That(cold.BaselineCoverage, Is.EqualTo(0));
            Assert.That(cold.IsSecured, Is.False);
            Assert.That(evaluation.FlexDirective.RouteId, Is.EqualTo(GlasshouseScenario.ColdHatchRouteId));
            Assert.That(evaluation.FlexDirective.PositionId, Is.EqualTo("flex_cold"));
        }

        [Test]
        public void OneHingeLoss_CreatesTwoDeficitsAndPressureChoosesTheRepair()
        {
            _engine.SetUnitDown("delta", true);
            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 7);
            _engine.SetRoutePressure(GlasshouseScenario.ServiceRouteId, 4);

            TacticalEvaluation first = _engine.Evaluate();
            Assert.That(first.GetRoute(GlasshouseScenario.CrimsonRouteId).IsSecured, Is.False);
            Assert.That(first.GetRoute(GlasshouseScenario.ServiceRouteId).IsSecured, Is.False);
            Assert.That(first.FlexDirective.RouteId, Is.EqualTo(GlasshouseScenario.CrimsonRouteId));

            _engine.SetRoutePressure(GlasshouseScenario.CrimsonRouteId, 1);
            _engine.SetRoutePressure(GlasshouseScenario.ServiceRouteId, 8);
            TacticalEvaluation rotated = _engine.Evaluate();
            Assert.That(rotated.FlexDirective.RouteId, Is.EqualTo(GlasshouseScenario.ServiceRouteId));
        }

        [Test]
        public void LeavingAnAuthoredPosition_RemovesCoverageUntilTheUnitSettlesAgain()
        {
            _engine.MarkUnitInTransit("alpha");
            TacticalEvaluation moving = _engine.Evaluate();
            TacticalRouteRuntimeState service = moving.GetRoute(GlasshouseScenario.ServiceRouteId);

            Assert.That(service.BaselineCoverage, Is.EqualTo(1));
            Assert.That(service.IsSecured, Is.False);
            Assert.That(moving.FlexDirective.RouteId, Is.EqualTo(GlasshouseScenario.ServiceRouteId));

            _engine.SetUnitPosition("alpha", "alpha_archive");
            TacticalEvaluation restored = _engine.Evaluate();
            Assert.That(restored.GetRoute(GlasshouseScenario.ServiceRouteId).IsSecured, Is.True);
            Assert.That(restored.FlexDirective.IsActive, Is.False);
        }

        [Test]
        public void OpenScreen_PreservesTheRemoteHatchPromise()
        {
            _engine.SetSurfaceState(
                GlasshouseScenario.PermeableScreenId,
                TacticalSurfaceState.Open);
            TacticalEvaluation evaluation = _engine.Evaluate();
            TacticalRouteRuntimeState cold = evaluation.GetRoute(GlasshouseScenario.ColdHatchRouteId);

            Assert.That(cold.BaselineCoverage, Is.EqualTo(1));
            Assert.That(cold.IsSecured, Is.True);
        }

        private static void AssertRoute(
            TacticalEvaluation evaluation,
            string routeId,
            int baseline,
            int coverage,
            bool secured)
        {
            TacticalRouteRuntimeState route = evaluation.GetRoute(routeId);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.BaselineCoverage, Is.EqualTo(baseline));
            Assert.That(route.Coverage, Is.EqualTo(coverage));
            Assert.That(route.IsSecured, Is.EqualTo(secured));
        }
    }
}

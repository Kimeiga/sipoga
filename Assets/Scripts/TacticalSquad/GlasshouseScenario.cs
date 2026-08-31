using UnityEngine;

namespace Sipoga.Tactics
{
    /// <summary>
    /// A deliberately original tactical slice built to teach the same kind of
    /// interlocking map-control logic that makes professional destruction shooters
    /// interesting. It does not reproduce any commercial map, room sequence, names,
    /// dimensions, art, or assets.
    /// </summary>
    public static class GlasshouseScenario
    {
        public const string ScenarioId = "glasshouse";
        public const string FlexUnitId = "foxtrot";
        public const string PermeableScreenId = "overwatch_screen";

        public const string CrimsonRouteId = "crimson_route";
        public const string ServiceRouteId = "service_route";
        public const string ColdHatchRouteId = "cold_hatch_route";

        public static TacticalScenarioDefinition Create()
        {
            TacticalScenarioDefinition scenario = new TacticalScenarioDefinition(
                ScenarioId,
                "GLASSHOUSE",
                "prototype-0.1",
                "Six operators maintain a network of promises across two floors. " +
                "The reserve operator repairs whichever dependency fails first.");

            AddPositions(scenario);
            AddRoutes(scenario);
            AddSurfaces(scenario);
            AddUnits(scenario);
            AddCoverageRules(scenario);
            AddFlexOptions(scenario);
            AddAttackPlaybooks(scenario);
            return scenario;
        }

        private static void AddPositions(TacticalScenarioDefinition scenario)
        {
            // Ground floor.
            scenario.Positions.Add(Position("alpha_archive", "Archive anchor", -4.8f, 1.0f, -2.5f, 0));
            scenario.Positions.Add(Position("alpha_fallback", "Archive deep cover", -7.0f, 1.0f, -4.7f, 0));
            scenario.Positions.Add(Position("alpha_execute", "Archive threshold", -2.7f, 1.0f, -1.8f, 0));

            scenario.Positions.Add(Position("delta_packing", "Packing hinge", 2.7f, 1.0f, -1.5f, 0));
            scenario.Positions.Add(Position("delta_fallback", "Packing interior", 0.8f, 1.0f, -3.8f, 0));
            scenario.Positions.Add(Position("delta_execute", "Packing threshold", 1.4f, 1.0f, -1.3f, 0));

            scenario.Positions.Add(Position("echo_crimson", "Crimson delay", 6.3f, 1.0f, 2.8f, 0));
            scenario.Positions.Add(Position("echo_fallback", "Crimson fallback", 4.3f, 1.0f, 0.7f, 0));
            scenario.Positions.Add(Position("echo_execute", "Packing east", 3.7f, 1.0f, -0.2f, 0));

            scenario.Positions.Add(Position("foxtrot_reserve", "Central reserve", 0.0f, 1.0f, 2.1f, 0));
            scenario.Positions.Add(Position("foxtrot_fallback", "Reserve fallback", -1.5f, 1.0f, -1.1f, 0));
            scenario.Positions.Add(Position("foxtrot_execute", "Central execute", 0.0f, 1.0f, -0.8f, 0));
            scenario.Positions.Add(Position("flex_crimson", "Crimson backup", 4.7f, 1.0f, 1.5f, 0));
            scenario.Positions.Add(Position("flex_service", "Service backup", -0.8f, 1.0f, 0.3f, 0));
            scenario.Positions.Add(Position("flex_cold", "Cold Store backup", -3.3f, 1.0f, 2.8f, 0));

            // Upper floor catwalks.
            scenario.Positions.Add(Position("bravo_overwatch", "Overwatch screen", -4.8f, 4.35f, 1.2f, 1));
            scenario.Positions.Add(Position("bravo_fallback", "Overwatch rear", -7.0f, 4.35f, -0.7f, 1));
            scenario.Positions.Add(Position("bravo_execute", "Overwatch bridge", -2.7f, 4.35f, -1.8f, 1));

            scenario.Positions.Add(Position("charlie_gallery", "Gallery crossfire", 4.5f, 4.35f, -1.8f, 1));
            scenario.Positions.Add(Position("charlie_fallback", "Gallery back rail", 2.2f, 4.35f, -3.7f, 1));
            scenario.Positions.Add(Position("charlie_execute", "Gallery bridge", 3.4f, 4.35f, -0.4f, 1));
        }

        private static void AddRoutes(TacticalScenarioDefinition scenario)
        {
            scenario.Routes.Add(new TacticalRouteDefinition(
                CrimsonRouteId,
                "CRIMSON STAIR",
                "A deep stair route that should enter three overlapping lines of control.",
                3,
                new Vector3(8.2f, 0.13f, 6.8f),
                new Vector3(3.0f, 0.13f, 0.3f)));

            scenario.Routes.Add(new TacticalRouteDefinition(
                ServiceRouteId,
                "SERVICE HALL",
                "The direct objective route. Two operators must keep it from becoming a clean sprint.",
                2,
                new Vector3(-0.3f, 0.14f, 7.7f),
                new Vector3(-0.3f, 0.14f, -1.2f)));

            scenario.Routes.Add(new TacticalRouteDefinition(
                ColdHatchRouteId,
                "COLD HATCH",
                "A vertical opening remotely denied from Overwatch through a permeable screen.",
                1,
                new Vector3(-5.2f, 0.15f, 6.5f),
                new Vector3(-5.2f, 0.15f, 1.4f)));
        }

        private static void AddSurfaces(TacticalScenarioDefinition scenario)
        {
            scenario.Surfaces.Add(new TacticalSurfaceDefinition(
                PermeableScreenId,
                "Overwatch permeable screen",
                TacticalSurfaceState.Permeable,
                new Vector3(-3.8f, 4.4f, 1.2f),
                new Vector3(0.22f, 2.35f, 3.8f)));
        }

        private static void AddUnits(TacticalScenarioDefinition scenario)
        {
            scenario.Units.Add(new TacticalUnitPlan(
                "alpha",
                "ALPHA",
                "Service anchor",
                TacticalResponsibilityKind.DenyRoute,
                "alpha_archive",
                "alpha_fallback",
                "alpha_execute",
                false,
                new Color(0.30f, 0.78f, 1.00f),
                "Deny the Service Hall execute from Archive.",
                "Delta links Packing to Service, while Bravo protects the vertical opening behind you.",
                "Packing is lost, or the attack splits Service from the upper-floor support network.",
                "Fall back to Archive deep cover without abandoning the objective threshold."));

            scenario.Units.Add(new TacticalUnitPlan(
                "bravo",
                "BRAVO",
                "Remote hatch denial",
                TacticalResponsibilityKind.DenySurface,
                "bravo_overwatch",
                "bravo_fallback",
                "bravo_execute",
                false,
                new Color(0.50f, 0.92f, 0.62f),
                "Deny the Cold Hatch from Overwatch through the permeable screen.",
                "Alpha owns the floor below, and the screen permits a remote line of fire without exposing the objective room.",
                "The screen is sealed, Overwatch is isolated, or you leave the exact line that reaches the hatch.",
                "Fall back to the Overwatch rear rail, then re-establish control from a safer depth."));

            scenario.Units.Add(new TacticalUnitPlan(
                "charlie",
                "CHARLIE",
                "Gallery crossfire",
                TacticalResponsibilityKind.Crossfire,
                "charlie_gallery",
                "charlie_fallback",
                "charlie_execute",
                false,
                new Color(1.00f, 0.66f, 0.28f),
                "Hold Gallery's long crossfire on Crimson Stair.",
                "Delta and Echo watch the same route from different depths, so an attacker cannot clear every angle at once.",
                "Gallery is isolated, or Delta leaves the Packing hinge and removes the middle layer of the crossfire.",
                "Fall back to the Gallery back rail before the attack can convert pressure into a pinch."));

            scenario.Units.Add(new TacticalUnitPlan(
                "delta",
                "DELTA",
                "Two-route hinge",
                TacticalResponsibilityKind.HoldAngle,
                "delta_packing",
                "delta_fallback",
                "delta_execute",
                false,
                new Color(0.78f, 0.55f, 1.00f),
                "Link both Crimson Stair and Service Hall from the Packing hinge.",
                "Charlie and Echo complete the stair crossfire, while Alpha completes Service coverage.",
                "This is a hinge position: losing it weakens two routes at the same time.",
                "Fall back to Packing interior and force the reserve operator to choose which promise matters more."));

            scenario.Units.Add(new TacticalUnitPlan(
                "echo",
                "ECHO",
                "Stair delay",
                TacticalResponsibilityKind.Delay,
                "echo_crimson",
                "echo_fallback",
                "echo_execute",
                false,
                new Color(1.00f, 0.40f, 0.48f),
                "Delay the Crimson Stair entry, then leave before the route becomes a close-range trap.",
                "Charlie and Delta punish anyone who chases through your retreat path.",
                "Gallery is isolated, Packing turns away, or you remain after the supporting angles disappear.",
                "Fall back to the Packing threshold and preserve the final layer of the stair defense."));

            scenario.Units.Add(new TacticalUnitPlan(
                FlexUnitId,
                "FOXTROT",
                "Reserve",
                TacticalResponsibilityKind.Reserve,
                "foxtrot_reserve",
                "foxtrot_fallback",
                "foxtrot_execute",
                true,
                new Color(0.98f, 0.90f, 0.28f),
                "Remain in reserve and replace the first broken promise.",
                "A central starting position keeps every backup route reachable.",
                "Two simultaneous failures can exceed one reserve operator's capacity.",
                "Return to central reserve whenever all baseline responsibilities are restored."));
        }

        private static void AddCoverageRules(TacticalScenarioDefinition scenario)
        {
            scenario.CoverageRules.Add(new TacticalCoverageRule(
                "alpha_service",
                "alpha",
                ServiceRouteId,
                "alpha_archive",
                "Alpha controls the objective end of Service Hall."));

            scenario.CoverageRules.Add(new TacticalCoverageRule(
                "bravo_cold_hatch",
                "bravo",
                ColdHatchRouteId,
                "bravo_overwatch",
                PermeableScreenId,
                new[] { TacticalSurfaceState.Permeable, TacticalSurfaceState.Open },
                "Bravo's remote denial only exists while the Overwatch screen transmits fire."));

            scenario.CoverageRules.Add(new TacticalCoverageRule(
                "charlie_crimson",
                "charlie",
                CrimsonRouteId,
                "charlie_gallery",
                "Charlie supplies the upper Gallery layer of the Crimson crossfire."));

            scenario.CoverageRules.Add(new TacticalCoverageRule(
                "delta_crimson",
                "delta",
                CrimsonRouteId,
                "delta_packing",
                "Delta supplies the middle layer of the Crimson crossfire."));

            scenario.CoverageRules.Add(new TacticalCoverageRule(
                "delta_service",
                "delta",
                ServiceRouteId,
                "delta_packing",
                "Delta links Packing to Service Hall."));

            scenario.CoverageRules.Add(new TacticalCoverageRule(
                "echo_crimson",
                "echo",
                CrimsonRouteId,
                "echo_crimson",
                "Echo supplies the forward delay layer of the Crimson crossfire."));
        }

        private static void AddFlexOptions(TacticalScenarioDefinition scenario)
        {
            scenario.FlexOptions.Add(new TacticalFlexOption(
                CrimsonRouteId,
                "flex_crimson",
                "Repair Crimson Stair",
                3,
                "The reserve replaces one missing stair angle from the Crimson backup position."));

            scenario.FlexOptions.Add(new TacticalFlexOption(
                ServiceRouteId,
                "flex_service",
                "Repair Service Hall",
                2,
                "The reserve restores a second Service line from the central threshold."));

            scenario.FlexOptions.Add(new TacticalFlexOption(
                ColdHatchRouteId,
                "flex_cold",
                "Repair Cold Hatch",
                1,
                "The reserve moves below the hatch and replaces the lost remote denial with direct control."));
        }

        private static void AddAttackPlaybooks(TacticalScenarioDefinition scenario)
        {
            TacticalAttackPlaybook isolateGallery = new TacticalAttackPlaybook(
                "isolate_gallery",
                "ISOLATE GALLERY",
                "The attack ignores the objective, removes the operator making Crimson safe, then accelerates through the gap.",
                "The visible duel is not the real target. The attack is deleting a supporting promise before entering.",
                20.0f);
            isolateGallery.Steps.Add(TacticalAttackStep.Log(
                0.6f,
                "Attackers drone Gallery and deliberately ignore the objective door."));
            isolateGallery.Steps.Add(TacticalAttackStep.Pressure(
                2.0f,
                CrimsonRouteId,
                4,
                "Four attackers begin pressuring Crimson Stair."));
            isolateGallery.Steps.Add(TacticalAttackStep.UnitDown(
                5.0f,
                "charlie",
                true,
                "Gallery is isolated. Charlie goes down and the three-angle stair crossfire loses its upper layer."));
            isolateGallery.Steps.Add(TacticalAttackStep.Pressure(
                10.0f,
                CrimsonRouteId,
                7,
                "The attack accelerates through Crimson before the defense can live with the new geometry."));
            isolateGallery.Steps.Add(TacticalAttackStep.Log(
                16.0f,
                "Foxtrot must physically reach Crimson backup before the route is truly repaired."));
            scenario.AttackPlaybooks.Add(isolateGallery);

            TacticalAttackPlaybook blockRemote = new TacticalAttackPlaybook(
                "block_remote_control",
                "BLOCK REMOTE CONTROL",
                "The attack changes one surface state, deleting a line of control without fighting the operator who owned it.",
                "Map destruction is not decoration. A surface state can switch an entire responsibility on or off.",
                19.0f);
            blockRemote.Steps.Add(TacticalAttackStep.Log(
                0.6f,
                "Attackers identify that Cold Hatch is denied remotely from Overwatch."));
            blockRemote.Steps.Add(TacticalAttackStep.Pressure(
                2.0f,
                ColdHatchRouteId,
                4,
                "Cold Hatch pressure begins while Bravo still owns the remote line."));
            blockRemote.Steps.Add(TacticalAttackStep.Surface(
                5.0f,
                PermeableScreenId,
                TacticalSurfaceState.Sealed,
                "The Overwatch screen is sealed. Bravo remains alive, but the remote promise disappears."));
            blockRemote.Steps.Add(TacticalAttackStep.Pressure(
                9.0f,
                ColdHatchRouteId,
                7,
                "The attack commits to Cold Hatch after removing its supporting geometry."));
            blockRemote.Steps.Add(TacticalAttackStep.Log(
                15.0f,
                "Foxtrot repairs the route by taking direct control below the hatch."));
            scenario.AttackPlaybooks.Add(blockRemote);

            TacticalAttackPlaybook wideSplit = new TacticalAttackPlaybook(
                "wide_split",
                "WIDE SPLIT",
                "The attack removes the single hinge that supports two routes, forcing one reserve operator to choose.",
                "A reserve can repair one failure. Creating two failures at once turns map control into a prioritization problem.",
                22.0f);
            wideSplit.Steps.Add(TacticalAttackStep.Pressure(
                1.0f,
                ServiceRouteId,
                4,
                "Two attackers show presence in Service Hall."));
            wideSplit.Steps.Add(TacticalAttackStep.Pressure(
                2.0f,
                CrimsonRouteId,
                6,
                "A heavier group takes space toward Crimson Stair."));
            wideSplit.Steps.Add(TacticalAttackStep.UnitDown(
                5.0f,
                "delta",
                true,
                "Delta goes down. One casualty weakens both Crimson and Service at the same instant."));
            wideSplit.Steps.Add(TacticalAttackStep.Pressure(
                11.0f,
                CrimsonRouteId,
                1,
                "The Crimson group stops advancing and holds the defense in place."));
            wideSplit.Steps.Add(TacticalAttackStep.Pressure(
                11.2f,
                ServiceRouteId,
                8,
                "The real execute rotates into Service, forcing Foxtrot to abandon the first repair."));
            wideSplit.Steps.Add(TacticalAttackStep.Log(
                18.0f,
                "The route with the greater immediate pressure wins the reserve operator."));
            scenario.AttackPlaybooks.Add(wideSplit);
        }

        private static TacticalPositionDefinition Position(
            string id,
            string label,
            float x,
            float y,
            float z,
            int floor)
        {
            return new TacticalPositionDefinition(id, label, new Vector3(x, y, z), floor);
        }
    }
}

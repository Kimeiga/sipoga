using UnityEngine;

namespace Sipoga.Tactics
{
    /// <summary>
    /// A compact command layer over the explanatory HUD. Guided mode demonstrates
    /// the plan automatically; Command mode turns the same scenario into a reaction
    /// game where the player must send the reserve to the right broken promise.
    /// </summary>
    public sealed class TacticalCommandHud : MonoBehaviour
    {
        private const float DesignWidth = 1600f;
        private const float DesignHeight = 900f;

        private TacticalSquadDirector _director;
        private GUIStyle _bar;
        private GUIStyle _button;
        private GUIStyle _strongButton;
        private GUIStyle _label;
        private GUIStyle _muted;
        private GUIStyle _prompt;
        private GUIStyle _reportPanel;
        private GUIStyle _grade;
        private GUIStyle _reportTitle;
        private GUIStyle _reportBody;
        private bool _stylesReady;

        public void Initialize(TacticalSquadDirector director)
        {
            _director = director;
        }

        private void OnGUI()
        {
            if (_director == null || _director.Evaluation == null)
            {
                return;
            }

            GUI.depth = -20;
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
            if (scale <= 0f)
            {
                return;
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                Vector3.zero,
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            DrawDecisionPrompt();
            DrawCommandBar();
            DrawAfterActionReport();

            GUI.matrix = previousMatrix;
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }

        private void DrawCommandBar()
        {
            GUI.Box(new Rect(386f, 844f, 784f, 40f), GUIContent.none, _bar);

            string modeLabel = _director.ControlMode == TacticalControlMode.Guided
                ? "GUIDED [M]"
                : "COMMAND [M]";
            if (GUI.Button(new Rect(394f, 850f, 110f, 28f), modeLabel, _strongButton))
            {
                _director.ToggleControlMode();
            }

            bool commandMode = _director.ControlMode == TacticalControlMode.Command;
            GUI.enabled = commandMode;
            TacticalFlexDirective directive = _director.Evaluation.FlexDirective;
            bool hasRecommendation = directive != null && directive.IsActive;

            GUI.enabled = commandMode && hasRecommendation;
            if (GUI.Button(new Rect(510f, 850f, 150f, 28f), "SEND BEST [Q]", _button))
            {
                _director.OrderFlexToRecommended();
            }

            GUI.enabled = commandMode;
            for (int i = 0; i < _director.Scenario.Routes.Count; i++)
            {
                TacticalRouteDefinition route = _director.Scenario.Routes[i];
                TacticalRouteRuntimeState routeState = _director.Evaluation.GetRoute(route.Id);
                bool recommended = hasRecommendation && directive.RouteId == route.Id;
                Color oldBackground = GUI.backgroundColor;
                if (recommended)
                {
                    GUI.backgroundColor = new Color(1.00f, 0.63f, 0.20f);
                }
                else if (routeState != null && routeState.IsSecured)
                {
                    GUI.backgroundColor = new Color(0.35f, 0.52f, 0.48f);
                }
                else
                {
                    GUI.backgroundColor = new Color(0.86f, 0.30f, 0.32f);
                }

                float x = 666f + i * 104f;
                if (GUI.Button(
                    new Rect(x, 850f, 98f, 28f),
                    "F > " + ShortRouteName(route.Id),
                    _button))
                {
                    _director.OrderFlexToRoute(route.Id);
                }

                GUI.backgroundColor = oldBackground;
            }

            GUI.enabled = true;
            TacticalMissionReport report = _director.MissionReport;
            string score = report != null
                ? "CONTROL  " + report.ControlPercent.ToString("0") + "%"
                : "CONTROL  --";
            GUI.Label(new Rect(982f, 851f, 174f, 26f), score, _label);
        }

        private void DrawDecisionPrompt()
        {
            if (_director.Phase != TacticalPrototypePhase.Execution)
            {
                return;
            }

            TacticalFlexDirective directive = _director.Evaluation.FlexDirective;
            if (directive == null || !directive.IsActive)
            {
                return;
            }

            TacticalRouteDefinition route = _director.Engine.GetRouteDefinition(directive.RouteId);
            TacticalRouteRuntimeState routeState = _director.Evaluation.GetRoute(directive.RouteId);
            if (route == null || routeState == null || routeState.IsFlexHolding)
            {
                return;
            }

            string prefix = _director.ControlMode == TacticalControlMode.Guided
                ? "GUIDED RESPONSE"
                : "YOUR DECISION";
            string text =
                prefix + "  //  " + route.Label + " IS " + routeState.StatusLabel +
                "  //  SEND FOXTROT TO " + directive.ResponsibilityLabel.ToUpperInvariant();
            GUI.Box(new Rect(444f, 190f, 712f, 52f), text, _prompt);
        }

        private void DrawAfterActionReport()
        {
            if (_director.Phase != TacticalPrototypePhase.AfterAction)
            {
                return;
            }

            TacticalMissionReport report = _director.MissionReport;
            if (report == null)
            {
                return;
            }

            Rect panel = new Rect(452f, 190f, 696f, 270f);
            GUI.Box(panel, GUIContent.none, _reportPanel);
            GUI.Label(new Rect(478f, 208f, 180f, 94f), report.Grade, _grade);
            GUI.Label(new Rect(630f, 214f, 470f, 30f), "AFTER-ACTION CONTROL REPORT", _reportTitle);
            GUI.Label(
                new Rect(630f, 247f, 470f, 48f),
                report.Summary,
                _reportBody);

            string commandLine = report.CommandCount <= 0
                ? "Reserve commands: none"
                : "Reserve commands: " + report.CommandCount +
                  "  //  correct target " + (report.CommandAccuracy * 100f).ToString("0") + "%";
            GUI.Label(new Rect(478f, 310f, 620f, 24f), commandLine, _muted);
            GUI.Label(
                new Rect(478f, 339f, 620f, 54f),
                "COACH: " + report.Recommendation,
                _reportBody);

            if (GUI.Button(new Rect(478f, 408f, 150f, 34f), "REPLAY", _strongButton))
            {
                _director.StartOrRestartExecution();
            }

            if (GUI.Button(new Rect(638f, 408f, 150f, 34f), "NEXT ATTACK", _button))
            {
                _director.NextPlaybook();
            }

            string modeLabel = _director.ControlMode == TacticalControlMode.Guided
                ? "TRY COMMAND"
                : "WATCH GUIDED";
            if (GUI.Button(new Rect(798f, 408f, 170f, 34f), modeLabel, _button))
            {
                _director.ToggleControlMode();
                _director.ResetCurrentPlaybook();
            }

            if (GUI.Button(new Rect(978f, 408f, 144f, 34f), "RESET PLAN", _button))
            {
                _director.ResetCurrentPlaybook();
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            Texture2D barTexture = SolidTexture(new Color(0.025f, 0.032f, 0.040f, 0.96f));
            Texture2D buttonTexture = SolidTexture(new Color(0.11f, 0.14f, 0.17f, 0.98f));
            Texture2D strongTexture = SolidTexture(new Color(0.92f, 0.48f, 0.18f, 0.98f));
            Texture2D panelTexture = SolidTexture(new Color(0.025f, 0.032f, 0.040f, 0.985f));
            Texture2D promptTexture = SolidTexture(new Color(0.34f, 0.12f, 0.08f, 0.97f));

            _bar = new GUIStyle(GUI.skin.box);
            _bar.normal.background = barTexture;
            _bar.border = new RectOffset(1, 1, 1, 1);

            _button = new GUIStyle(GUI.skin.button);
            _button.normal.background = buttonTexture;
            _button.hover.background = buttonTexture;
            _button.active.background = strongTexture;
            _button.normal.textColor = new Color(0.88f, 0.91f, 0.94f);
            _button.hover.textColor = Color.white;
            _button.active.textColor = Color.white;
            _button.alignment = TextAnchor.MiddleCenter;
            _button.fontSize = 11;
            _button.fontStyle = FontStyle.Bold;
            _button.padding = new RectOffset(4, 4, 2, 2);

            _strongButton = new GUIStyle(_button);
            _strongButton.normal.background = strongTexture;
            _strongButton.hover.background = strongTexture;
            _strongButton.normal.textColor = Color.white;

            _label = new GUIStyle(GUI.skin.label);
            _label.alignment = TextAnchor.MiddleRight;
            _label.fontSize = 14;
            _label.fontStyle = FontStyle.Bold;
            _label.normal.textColor = new Color(0.48f, 0.94f, 0.72f);

            _muted = new GUIStyle(GUI.skin.label);
            _muted.fontSize = 13;
            _muted.normal.textColor = new Color(0.58f, 0.64f, 0.69f);

            _prompt = new GUIStyle(GUI.skin.box);
            _prompt.normal.background = promptTexture;
            _prompt.normal.textColor = new Color(1f, 0.91f, 0.75f);
            _prompt.alignment = TextAnchor.MiddleCenter;
            _prompt.fontSize = 15;
            _prompt.fontStyle = FontStyle.Bold;
            _prompt.padding = new RectOffset(12, 12, 8, 8);

            _reportPanel = new GUIStyle(GUI.skin.box);
            _reportPanel.normal.background = panelTexture;
            _reportPanel.padding = new RectOffset(18, 18, 18, 18);

            _grade = new GUIStyle(GUI.skin.label);
            _grade.fontSize = 76;
            _grade.fontStyle = FontStyle.Bold;
            _grade.alignment = TextAnchor.MiddleCenter;
            _grade.normal.textColor = new Color(1.00f, 0.63f, 0.20f);

            _reportTitle = new GUIStyle(GUI.skin.label);
            _reportTitle.fontSize = 20;
            _reportTitle.fontStyle = FontStyle.Bold;
            _reportTitle.normal.textColor = Color.white;

            _reportBody = new GUIStyle(GUI.skin.label);
            _reportBody.fontSize = 14;
            _reportBody.wordWrap = true;
            _reportBody.normal.textColor = new Color(0.82f, 0.86f, 0.90f);

            _stylesReady = true;
        }

        private static string ShortRouteName(string routeId)
        {
            if (routeId == GlasshouseScenario.CrimsonRouteId) return "CRIMSON";
            if (routeId == GlasshouseScenario.ServiceRouteId) return "SERVICE";
            if (routeId == GlasshouseScenario.ColdHatchRouteId) return "COLD";
            return routeId.ToUpperInvariant();
        }

        private static Texture2D SolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "Tactical HUD " + ColorUtility.ToHtmlStringRGBA(color);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}

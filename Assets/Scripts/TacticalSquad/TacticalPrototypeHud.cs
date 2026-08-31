using UnityEngine;

namespace Sipoga.Tactics
{
    public sealed class TacticalPrototypeHud : MonoBehaviour
    {
        private const float DesignWidth = 1600f;
        private const float DesignHeight = 900f;

        private TacticalSquadDirector _director;
        private GUIStyle _topBar;
        private GUIStyle _panel;
        private GUIStyle _card;
        private GUIStyle _selectedCard;
        private GUIStyle _button;
        private GUIStyle _buttonStrong;
        private GUIStyle _title;
        private GUIStyle _heading;
        private GUIStyle _body;
        private GUIStyle _small;
        private GUIStyle _muted;
        private GUIStyle _routeStatus;
        private GUIStyle _afterAction;
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

            DrawTopBar();
            DrawSquadPanel();
            DrawRoutePanel();
            DrawExplanationPanel();
            DrawEventPanel();
            DrawControls();
            DrawPhaseCard();

            GUI.matrix = previousMatrix;
        }

        private void DrawTopBar()
        {
            GUI.Box(new Rect(18f, 16f, 1564f, 62f), GUIContent.none, _topBar);
            GUI.Label(new Rect(38f, 25f, 390f, 28f), "SIPOGA // TACTICAL PROTOTYPE", _title);
            GUI.Label(
                new Rect(38f, 50f, 520f, 20f),
                _director.Scenario.DisplayName + "  •  " + _director.Scenario.Revision +
                "  •  ORIGINAL TRAINING MAP",
                _muted);

            TacticalAttackPlaybook playbook = _director.CurrentPlaybook;
            GUI.Label(new Rect(585f, 25f, 390f, 24f), playbook.Name, _heading);
            GUI.Label(new Rect(585f, 49f, 390f, 20f), playbook.Summary, _small);

            string phase = _director.Phase.ToString().ToUpperInvariant();
            string timer = _director.Phase == TacticalPrototypePhase.Planning
                ? "READY"
                : _director.ExecutionTime.ToString("00.0") + "s";
            GUI.Label(new Rect(1000f, 25f, 120f, 22f), phase, _heading);
            GUI.Label(new Rect(1000f, 49f, 120f, 20f), timer, _muted);

            string beginLabel = _director.Phase == TacticalPrototypePhase.Planning
                ? "BEGIN"
                : "RESTART";
            if (GUI.Button(new Rect(1128f, 27f, 102f, 38f), beginLabel, _buttonStrong))
            {
                _director.StartOrRestartExecution();
            }

            if (GUI.Button(new Rect(1238f, 27f, 102f, 38f), "NEXT PLAY", _button))
            {
                _director.NextPlaybook();
            }

            if (GUI.Button(new Rect(1348f, 27f, 98f, 38f), "RESET", _button))
            {
                _director.ResetCurrentPlaybook();
            }

            string viewLabel = IsTacticalView() ? "OPERATOR" : "TACTICAL";
            if (GUI.Button(new Rect(1454f, 27f, 108f, 38f), viewLabel, _button))
            {
                _director.ToggleView();
            }
        }

        private void DrawSquadPanel()
        {
            GUI.Box(new Rect(18f, 92f, 352f, 598f), GUIContent.none, _panel);
            GUI.Label(new Rect(36f, 108f, 260f, 26f), "SIX PROMISES", _heading);
            GUI.Label(
                new Rect(36f, 134f, 300f, 40f),
                "Select an operator to see what they control and why their position works.",
                _small);

            for (int i = 0; i < _director.Scenario.Units.Count; i++)
            {
                TacticalUnitPlan plan = _director.Scenario.Units[i];
                TacticalUnitAgent agent = _director.GetAgent(plan.UnitId);
                TacticalUnitRuntimeState state = _director.Engine.GetUnitState(plan.UnitId);
                bool selected = plan.UnitId == _director.SelectedUnitId;
                Rect cardRect = new Rect(34f, 184f + i * 80f, 320f, 68f);
                if (GUI.Button(cardRect, GUIContent.none, selected ? _selectedCard : _card))
                {
                    _director.SelectUnit(plan.UnitId);
                }

                Color previousColor = GUI.color;
                GUI.color = plan.DisplayColor;
                GUI.DrawTexture(
                    new Rect(cardRect.x + 8f, cardRect.y + 10f, 5f, cardRect.height - 20f),
                    Texture2D.whiteTexture);
                GUI.color = previousColor;

                GUI.Label(
                    new Rect(cardRect.x + 22f, cardRect.y + 7f, 54f, 23f),
                    (i + 1).ToString("00"),
                    _muted);
                GUI.Label(
                    new Rect(cardRect.x + 65f, cardRect.y + 6f, 130f, 24f),
                    plan.Callsign,
                    _heading);

                string condition = state.Condition == TacticalUnitCondition.Down
                    ? "DOWN"
                    : agent != null ? agent.GetStatusLabel() : "READY";
                GUIStyle conditionStyle = new GUIStyle(_small);
                conditionStyle.alignment = TextAnchor.UpperRight;
                conditionStyle.normal.textColor = state.Condition == TacticalUnitCondition.Down
                    ? new Color(1f, 0.35f, 0.38f)
                    : plan.DisplayColor;
                GUI.Label(
                    new Rect(cardRect.x + 210f, cardRect.y + 8f, 92f, 22f),
                    condition,
                    conditionStyle);

                GUI.Label(
                    new Rect(cardRect.x + 65f, cardRect.y + 31f, 228f, 18f),
                    _director.Engine.GetCurrentResponsibilityLabel(plan.UnitId, _director.Evaluation),
                    _body);
                GUI.Label(
                    new Rect(cardRect.x + 65f, cardRect.y + 49f, 228f, 16f),
                    _director.Engine.GetPositionLabel(plan.UnitId),
                    _muted);
            }
        }

        private void DrawRoutePanel()
        {
            GUI.Box(new Rect(1186f, 92f, 396f, 250f), GUIContent.none, _panel);
            GUI.Label(new Rect(1204f, 108f, 270f, 26f), "LIVE MAP CONTROL", _heading);
            GUI.Label(
                new Rect(1204f, 134f, 345f, 34f),
                "A route is safe only while enough independent promises remain active.",
                _small);

            for (int i = 0; i < _director.Evaluation.Routes.Count; i++)
            {
                TacticalRouteRuntimeState routeState = _director.Evaluation.Routes[i];
                TacticalRouteDefinition route = _director.Engine.GetRouteDefinition(routeState.RouteId);
                Rect routeRect = new Rect(1204f, 176f + i * 50f, 360f, 42f);
                GUI.Box(routeRect, GUIContent.none, _card);

                Color statusColor = GetRouteColor(routeState);
                Color previousColor = GUI.color;
                GUI.color = statusColor;
                GUI.DrawTexture(
                    new Rect(routeRect.x + 8f, routeRect.y + 8f, 4f, routeRect.height - 16f),
                    Texture2D.whiteTexture);
                GUI.color = previousColor;

                GUI.Label(new Rect(routeRect.x + 20f, routeRect.y + 5f, 190f, 20f), route.Label, _body);
                GUI.Label(
                    new Rect(routeRect.x + 20f, routeRect.y + 23f, 200f, 16f),
                    "pressure " + routeState.Pressure + "  •  baseline " + routeState.BaselineCoverage,
                    _muted);

                GUIStyle statusStyle = new GUIStyle(_routeStatus);
                statusStyle.normal.textColor = statusColor;
                GUI.Label(
                    new Rect(routeRect.x + 215f, routeRect.y + 5f, 132f, 30f),
                    routeState.Coverage + "/" + routeState.RequiredCoverage + "  " + routeState.StatusLabel,
                    statusStyle);
            }
        }

        private void DrawExplanationPanel()
        {
            GUI.Box(new Rect(1186f, 356f, 396f, 334f), GUIContent.none, _panel);
            TacticalUnitPlan selectedPlan = _director.Engine.GetUnitPlan(_director.SelectedUnitId);
            TacticalUnitRuntimeState selectedState = _director.Engine.GetUnitState(_director.SelectedUnitId);
            TacticalExplanation explanation = _director.Engine.GetExplanation(
                _director.SelectedUnitId,
                _director.Evaluation);

            GUI.Label(new Rect(1204f, 372f, 260f, 24f), selectedPlan.Callsign, _heading);
            GUI.Label(
                new Rect(1204f, 397f, 340f, 18f),
                _director.Engine.GetCurrentResponsibilityLabel(selectedPlan.UnitId, _director.Evaluation) +
                "  •  " + _director.Engine.GetPositionLabel(selectedPlan.UnitId),
                _muted);

            DrawExplanationRow(1204f, 426f, "WHAT", explanation.What, selectedPlan.DisplayColor);
            DrawExplanationRow(1204f, 486f, "WHY SAFE", explanation.WhySafe, new Color(0.30f, 0.88f, 0.68f));
            DrawExplanationRow(1204f, 554f, "WHAT BREAKS IT", explanation.WhatBreaksIt, new Color(1.0f, 0.42f, 0.42f));
            DrawExplanationRow(1204f, 622f, "FALLBACK", explanation.Fallback, new Color(0.72f, 0.76f, 0.82f));

            GUI.enabled = selectedState.Condition != TacticalUnitCondition.Down;
            if (GUI.Button(new Rect(1204f, 657f, 104f, 24f), "FALL BACK", _button))
            {
                _director.OrderSelectedFallback();
            }

            if (GUI.Button(new Rect(1314f, 657f, 104f, 24f), "HOLD ROLE", _button))
            {
                _director.OrderSelectedToAssignment();
            }
            GUI.enabled = true;

            string downLabel = selectedState.Condition == TacticalUnitCondition.Down ? "RESTORE" : "TEST DOWN";
            if (GUI.Button(new Rect(1424f, 657f, 140f, 24f), downLabel, _button))
            {
                _director.ToggleSelectedDown();
            }
        }

        private void DrawEventPanel()
        {
            GUI.Box(new Rect(386f, 696f, 784f, 142f), GUIContent.none, _panel);
            GUI.Label(new Rect(404f, 710f, 260f, 24f), "CAUSAL REPLAY", _heading);
            GUI.Label(
                new Rect(404f, 733f, 720f, 18f),
                "The log names the dependency that changed, not only the kill or breach that happened.",
                _muted);

            int first = Mathf.Max(0, _director.Events.Count - 4);
            float y = 758f;
            for (int i = first; i < _director.Events.Count; i++)
            {
                TacticalEventRecord record = _director.Events[i];
                GUI.Label(
                    new Rect(404f, y, 64f, 19f),
                    record.Time.ToString("00.0"),
                    _muted);
                GUI.Label(new Rect(466f, y, 680f, 19f), record.Text, _small);
                y += 20f;
            }
        }

        private void DrawControls()
        {
            GUI.Box(new Rect(18f, 704f, 352f, 178f), GUIContent.none, _panel);
            GUI.Label(new Rect(36f, 720f, 200f, 24f), "CONTROLS", _heading);
            GUI.Label(
                new Rect(36f, 749f, 316f, 114f),
                "1–6  select operator\n" +
                "Tab  tactical / operator view\n" +
                "WASD  move selected operator\n" +
                "F / H  fallback / return to role\n" +
                "X  test operator down\n" +
                "B  cycle permeable screen\n" +
                "P or [ ]  change attacker play\n" +
                "G  squad collapse go-code",
                _body);

            GUI.Box(new Rect(1186f, 704f, 396f, 178f), GUIContent.none, _panel);
            GUI.Label(new Rect(1204f, 720f, 250f, 24f), "SURFACE + GO-CODE", _heading);
            TacticalSurfaceState screenState = _director.Engine.GetSurfaceState(
                GlasshouseScenario.PermeableScreenId);
            GUI.Label(
                new Rect(1204f, 750f, 350f, 20f),
                "Overwatch screen: " + screenState.ToString().ToUpperInvariant(),
                _body);
            if (GUI.Button(new Rect(1204f, 782f, 170f, 36f), "CYCLE SCREEN [B]", _button))
            {
                _director.CyclePermeableScreen();
            }

            if (GUI.Button(new Rect(1382f, 782f, 182f, 36f), "COLLAPSE [G]", _buttonStrong))
            {
                _director.TriggerGoCode();
            }

            GUI.Label(
                new Rect(1204f, 828f, 350f, 42f),
                "Permeable transmits Bravo's line. Sealed deletes it. Open preserves it.",
                _small);
        }

        private void DrawPhaseCard()
        {
            if (_director.Phase == TacticalPrototypePhase.Execution)
            {
                return;
            }

            Rect cardRect = new Rect(430f, 96f, 690f, 82f);
            GUI.Box(cardRect, GUIContent.none, _topBar);
            if (_director.Phase == TacticalPrototypePhase.Planning)
            {
                GUI.Label(new Rect(452f, 110f, 620f, 24f), "READ THE PLAN BEFORE THE FIGHT", _heading);
                GUI.Label(
                    new Rect(452f, 137f, 630f, 30f),
                    "Green routes are not inherently safe. They are safe because named operators are keeping interlocking promises.",
                    _body);
            }
            else
            {
                GUI.Label(new Rect(452f, 108f, 620f, 24f), "AFTER ACTION", _heading);
                GUI.Label(
                    new Rect(452f, 135f, 630f, 34f),
                    _director.CurrentPlaybook.Lesson,
                    _afterAction);
            }
        }

        private void DrawExplanationRow(float x, float y, string label, string text, Color color)
        {
            GUIStyle labelStyle = new GUIStyle(_small);
            labelStyle.normal.textColor = color;
            GUI.Label(new Rect(x, y, 120f, 18f), label, labelStyle);
            GUI.Label(new Rect(x, y + 18f, 350f, 45f), text, _small);
        }

        private bool IsTacticalView()
        {
            Camera main = Camera.main;
            if (main == null)
            {
                return true;
            }

            TacticalPrototypeCamera controller = main.GetComponent<TacticalPrototypeCamera>();
            return controller == null || controller.IsTacticalView;
        }

        private Color GetRouteColor(TacticalRouteRuntimeState state)
        {
            if (state.IsSecured)
            {
                return state.IsFlexHolding
                    ? new Color(0.98f, 0.88f, 0.28f)
                    : new Color(0.24f, 0.92f, 0.67f);
            }

            return state.IsBeingRepaired
                ? new Color(1.00f, 0.58f, 0.20f)
                : new Color(1.00f, 0.22f, 0.30f);
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _topBar = MakeBoxStyle(new Color(0.035f, 0.045f, 0.055f, 0.96f), 10);
            _panel = MakeBoxStyle(new Color(0.030f, 0.039f, 0.047f, 0.93f), 8);
            _card = MakeBoxStyle(new Color(0.060f, 0.073f, 0.084f, 0.92f), 5);
            _selectedCard = MakeBoxStyle(new Color(0.105f, 0.128f, 0.145f, 0.98f), 5);

            _button = new GUIStyle(GUI.skin.button);
            _button.normal.background = MakeTexture(new Color(0.10f, 0.12f, 0.14f, 0.98f));
            _button.hover.background = MakeTexture(new Color(0.15f, 0.18f, 0.21f, 1f));
            _button.active.background = MakeTexture(new Color(0.07f, 0.09f, 0.11f, 1f));
            _button.normal.textColor = new Color(0.82f, 0.86f, 0.89f);
            _button.hover.textColor = Color.white;
            _button.fontSize = 12;
            _button.fontStyle = FontStyle.Bold;

            _buttonStrong = new GUIStyle(_button);
            _buttonStrong.normal.background = MakeTexture(new Color(0.18f, 0.50f, 0.47f, 0.98f));
            _buttonStrong.hover.background = MakeTexture(new Color(0.23f, 0.64f, 0.59f, 1f));
            _buttonStrong.normal.textColor = Color.white;

            _title = MakeTextStyle(20, FontStyle.Bold, new Color(0.96f, 0.98f, 1f), TextAnchor.UpperLeft, false);
            _heading = MakeTextStyle(15, FontStyle.Bold, new Color(0.93f, 0.96f, 0.98f), TextAnchor.UpperLeft, false);
            _body = MakeTextStyle(13, FontStyle.Normal, new Color(0.82f, 0.86f, 0.89f), TextAnchor.UpperLeft, true);
            _small = MakeTextStyle(12, FontStyle.Normal, new Color(0.78f, 0.82f, 0.85f), TextAnchor.UpperLeft, true);
            _muted = MakeTextStyle(11, FontStyle.Normal, new Color(0.47f, 0.54f, 0.59f), TextAnchor.UpperLeft, true);
            _routeStatus = MakeTextStyle(13, FontStyle.Bold, Color.white, TextAnchor.MiddleRight, false);
            _afterAction = MakeTextStyle(13, FontStyle.Bold, new Color(0.98f, 0.87f, 0.36f), TextAnchor.UpperLeft, true);
            _stylesReady = true;
        }

        private GUIStyle MakeBoxStyle(Color color, int padding)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTexture(color);
            style.padding = new RectOffset(padding, padding, padding, padding);
            return style;
        }

        private GUIStyle MakeTextStyle(
            int fontSize,
            FontStyle fontStyle,
            Color color,
            TextAnchor alignment,
            bool wordWrap)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.normal.textColor = color;
            style.alignment = alignment;
            style.wordWrap = wordWrap;
            style.clipping = TextClipping.Clip;
            return style;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }
    }
}

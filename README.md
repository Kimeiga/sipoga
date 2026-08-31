# Sipoga

Sipoga is an FPS and squad-control prototype. The current tactical vertical slice asks the player to control six **responsibilities** rather than six independent crosshairs: one operator is directly selected while five deterministic agents maintain a network of routes, crossfires, vertical control, fallbacks, and repairs.

## Tactical squad prototype

Open `Assets/Scenes/TacticalSquadPrototype.unity` in Unity **2020.1.10f1** and press Play, or use:

`Sipoga > Tactical Prototype > Run Glasshouse`

The scene builds itself at runtime from primitives and authored tactical data. It uses no Rainbow Six maps, names, meshes, textures, audio, or other proprietary assets. `Glasshouse` is an original two-floor training map designed around general tactical motifs:

- Three operators form a layered crossfire on Crimson Stair.
- A hinge operator supports two routes at once.
- An upper operator remotely denies a hatch through a stateful permeable screen.
- A sixth reserve operator repairs the highest-pressure broken responsibility.
- Three attacker playbooks demonstrate isolation, geometry denial, and a simultaneous split.
- Visible attacker tokens move through each route according to pressure and the current control state.

The same scenario has two control modes:

- **Guided** demonstrates the dependency graph by automatically routing Foxtrot, the reserve operator, to the recommended repair.
- **Command** leaves Foxtrot in the player's hands. The player must read the threatened routes and issue the repair order before pressure converts into exposure.

A pressure-weighted scorer measures how much threatened map control stayed intact, how long the worst break remained open, whether reserve commands targeted the recommended route, and which route created the most exposure. Every play ends with a letter grade and a concrete coaching note.

### Controls

| Input | Action |
|---|---|
| `1`–`6` | Select an operator |
| `Tab` | Switch between tactical and operator views |
| `WASD` | Move the selected operator in operator view |
| `Space` | Begin or restart the current attacker playbook |
| `P`, `[` or `]` | Change attacker playbook |
| `M` | Toggle Guided / Command mode |
| `Q` | Send Foxtrot to the currently recommended repair |
| Command bar buttons | Send Foxtrot to Crimson, Service, or Cold directly |
| `F` | Order the selected operator to fallback |
| `H` | Return the selected operator to their assigned responsibility |
| `X` | Toggle the selected operator down/restored for testing |
| `B` | Cycle the Overwatch screen: permeable, sealed, open |
| `G` | Trigger the squad collapse go-code |
| `R` | Reset the current playbook |

The HUD answers four questions for every operator: what they are doing, why the position is safe, what breaks it, and where they fall back. Route bands and promise lines update as operators move, go down, or lose a required surface state. The causal event log explains which dependency changed rather than reporting only the visible kill or breach. The command layer turns those explanations into a playable reaction loop rather than a passive diagram.

## Architecture

The prototype is data-driven and self-contained under `Assets/Scripts/TacticalSquad`:

- `GlasshouseScenario` authors positions, routes, surfaces, six unit plans, coverage rules, flex repair options, and attacker timelines.
- `TacticalStateEngine` is a pure evaluator shared by gameplay, overlays, explanations, tests, and scoring.
- `TacticalSquadDirector` executes attacker playbooks and supports both automatic and player-commanded reserve behavior.
- `TacticalMissionScorer` turns pressure, missing promises, repair time, and command accuracy into an after-action report.
- `TacticalOverlayRenderer` visualizes live promises, broken routes, and reserve assignments.
- `TacticalThreatVisualizer` converts abstract pressure values into attacker tokens advancing through the map.
- `TacticalPrototypeHud` explains the plan; `TacticalCommandHud` supplies the command trial and graded debrief.
- `TacticalPrototypeBootstrap` generates the original map, squad, camera, overlays, and HUD at runtime.

Edit-mode tests live under `Assets/Tests/EditMode`. They verify initial coverage, position-dependent responsibility loss, stateful surface control, flex repairs, pressure-based prioritization, reference integrity, pressure-weighted scoring, break duration, unanswered threats, and command accuracy.

## Validation

Run the repository-level structural checks with:

```sh
python3 tools/validate_tactical_prototype.py
```

The validator checks required files, Unity metadata pairs and GUID uniqueness, assembly-definition JSON, C# delimiter balance, prototype wiring, scene registration, and documentation. GitHub Actions runs the same check on pushes and pull requests. This fast validator does not replace importing the project and running its edit-mode tests in Unity.

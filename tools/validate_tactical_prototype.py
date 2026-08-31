#!/usr/bin/env python3
"""Fast structural checks for the self-contained tactical prototype.

This does not replace importing the project in Unity. It catches the common errors
that can be verified without a Unity installation: malformed assembly definitions,
missing metadata, duplicate GUIDs, unbalanced C# delimiters, and incomplete scene
or documentation wiring.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[1]
TACTICS = ROOT / "Assets" / "Scripts" / "TacticalSquad"
TESTS = ROOT / "Assets" / "Tests" / "EditMode"
SCENE = ROOT / "Assets" / "Scenes" / "TacticalSquadPrototype.unity"

REQUIRED_FILES = [
    TACTICS / "GlasshouseScenario.cs",
    TACTICS / "TacticalModel.cs",
    TACTICS / "TacticalMissionScoring.cs",
    TACTICS / "TacticalSquadDirector.cs",
    TACTICS / "TacticalUnitAgent.cs",
    TACTICS / "TacticalOverlayRenderer.cs",
    TACTICS / "TacticalThreatVisualizer.cs",
    TACTICS / "TacticalPrototypeWorld.cs",
    TACTICS / "TacticalPrototypeCamera.cs",
    TACTICS / "TacticalPrototypeHud.cs",
    TACTICS / "TacticalCommandHud.cs",
    TACTICS / "TacticalPrototypeBootstrap.cs",
    TESTS / "TacticalStateEngineTests.cs",
    TESTS / "TacticalMissionScorerTests.cs",
    SCENE,
]


def fail(errors: list[str], message: str) -> None:
    errors.append(message)


def strip_csharp_noncode(source: str) -> str:
    """Replace comments and literals with spaces while preserving newlines."""
    output: list[str] = []
    i = 0
    state = "normal"
    while i < len(source):
        ch = source[i]
        nxt = source[i + 1] if i + 1 < len(source) else ""

        if state == "normal":
            if ch == "/" and nxt == "/":
                output.extend("  ")
                i += 2
                state = "line_comment"
                continue
            if ch == "/" and nxt == "*":
                output.extend("  ")
                i += 2
                state = "block_comment"
                continue
            if ch == "@" and nxt == '"':
                output.extend("  ")
                i += 2
                state = "verbatim_string"
                continue
            if ch == '"':
                output.append(" ")
                i += 1
                state = "string"
                continue
            if ch == "'":
                output.append(" ")
                i += 1
                state = "char"
                continue
            output.append(ch)
            i += 1
            continue

        if state == "line_comment":
            if ch == "\n":
                output.append("\n")
                state = "normal"
            else:
                output.append(" ")
            i += 1
            continue

        if state == "block_comment":
            if ch == "*" and nxt == "/":
                output.extend("  ")
                i += 2
                state = "normal"
            else:
                output.append("\n" if ch == "\n" else " ")
                i += 1
            continue

        if state == "string":
            if ch == "\\":
                output.append(" ")
                if nxt:
                    output.append("\n" if nxt == "\n" else " ")
                    i += 2
                else:
                    i += 1
                continue
            output.append("\n" if ch == "\n" else " ")
            i += 1
            if ch == '"':
                state = "normal"
            continue

        if state == "verbatim_string":
            if ch == '"' and nxt == '"':
                output.extend("  ")
                i += 2
                continue
            output.append("\n" if ch == "\n" else " ")
            i += 1
            if ch == '"':
                state = "normal"
            continue

        if state == "char":
            if ch == "\\":
                output.append(" ")
                if nxt:
                    output.append("\n" if nxt == "\n" else " ")
                    i += 2
                else:
                    i += 1
                continue
            output.append("\n" if ch == "\n" else " ")
            i += 1
            if ch == "'":
                state = "normal"
            continue

    if state in {"block_comment", "string", "verbatim_string", "char"}:
        raise ValueError(f"unterminated C# token: {state}")
    return "".join(output)


def validate_delimiters(path: Path, errors: list[str]) -> None:
    source = path.read_text(encoding="utf-8")
    try:
        code = strip_csharp_noncode(source)
    except ValueError as exc:
        fail(errors, f"{path.relative_to(ROOT)}: {exc}")
        return

    opening = {"(": ")", "[": "]", "{": "}"}
    closing = {value: key for key, value in opening.items()}
    stack: list[tuple[str, int]] = []
    line = 1
    for ch in code:
        if ch == "\n":
            line += 1
            continue
        if ch in opening:
            stack.append((ch, line))
        elif ch in closing:
            if not stack or stack[-1][0] != closing[ch]:
                fail(errors, f"{path.relative_to(ROOT)}:{line}: unmatched '{ch}'")
                return
            stack.pop()

    if stack:
        symbol, symbol_line = stack[-1]
        fail(errors, f"{path.relative_to(ROOT)}:{symbol_line}: unclosed '{symbol}'")


def tactical_assets() -> Iterable[Path]:
    for root in (TACTICS, TESTS):
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.is_file() and path.suffix in {".cs", ".asmdef"}:
                yield path
    if SCENE.exists():
        yield SCENE


def validate_meta_pairs(errors: list[str]) -> None:
    for path in tactical_assets():
        meta = path.with_name(path.name + ".meta")
        if not meta.exists():
            fail(errors, f"Missing Unity metadata: {meta.relative_to(ROOT)}")


def validate_guids(errors: list[str]) -> None:
    seen: dict[str, Path] = {}
    guid_pattern = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)
    for meta in (ROOT / "Assets").rglob("*.meta"):
        text = meta.read_text(encoding="utf-8", errors="replace")
        match = guid_pattern.search(text)
        if not match:
            continue
        guid = match.group(1).lower()
        previous = seen.get(guid)
        if previous is not None:
            fail(
                errors,
                "Duplicate Unity GUID " + guid + ": " +
                str(previous.relative_to(ROOT)) + " and " + str(meta.relative_to(ROOT)),
            )
        else:
            seen[guid] = meta


def validate_json(errors: list[str]) -> None:
    for asmdef in list(TACTICS.rglob("*.asmdef")) + list(TESTS.rglob("*.asmdef")):
        try:
            json.loads(asmdef.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            fail(errors, f"{asmdef.relative_to(ROOT)}: invalid JSON: {exc}")


def validate_wiring(errors: list[str]) -> None:
    for required in REQUIRED_FILES:
        if not required.exists():
            fail(errors, f"Missing required prototype file: {required.relative_to(ROOT)}")

    scenario = (TACTICS / "GlasshouseScenario.cs").read_text(encoding="utf-8")
    if scenario.count("new TacticalUnitPlan(") != 6:
        fail(errors, "GlasshouseScenario must author exactly six tactical unit plans.")
    if scenario.count("new TacticalAttackPlaybook(") != 3:
        fail(errors, "GlasshouseScenario must author exactly three attacker playbooks.")

    bootstrap = (TACTICS / "TacticalPrototypeBootstrap.cs").read_text(encoding="utf-8")
    for component in (
        "TacticalOverlayRenderer",
        "TacticalThreatVisualizer",
        "TacticalPrototypeHud",
        "TacticalCommandHud",
    ):
        if component not in bootstrap:
            fail(errors, f"Bootstrap does not wire {component}.")

    build_settings = (ROOT / "ProjectSettings" / "EditorBuildSettings.asset").read_text(
        encoding="utf-8"
    )
    if "Assets/Scenes/TacticalSquadPrototype.unity" not in build_settings:
        fail(errors, "TacticalSquadPrototype.unity is missing from EditorBuildSettings.asset.")

    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    for required_text in ("Guided", "Command", "TacticalMissionScorer", "TacticalCommandHud"):
        if required_text not in readme:
            fail(errors, f"README is missing the expected documentation term: {required_text}")


def main() -> int:
    errors: list[str] = []
    validate_meta_pairs(errors)
    validate_guids(errors)
    validate_json(errors)
    validate_wiring(errors)

    for path in list(TACTICS.rglob("*.cs")) + list(TESTS.glob("Tactical*.cs")):
        validate_delimiters(path, errors)

    if errors:
        print("Tactical prototype validation failed:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("Tactical prototype structural checks passed.")
    print(f"Checked {len(list(TACTICS.rglob('*.cs')))} runtime/editor C# files.")
    print(f"Checked {len(list(TESTS.glob('Tactical*.cs')))} tactical test files.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

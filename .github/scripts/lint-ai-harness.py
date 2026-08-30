#!/usr/bin/env python3
"""Lint the AI harness's own files.

Checks that would otherwise fail silently at agent runtime:
  1. Every .claude/agents/*.md and .claude/skills/*/SKILL.md has YAML frontmatter that
     actually parses and carries a non-empty name + description. (A description with an
     unquoted ": " silently never reaches the model's skill/agent listing — this has
     dropped two whole agents and three skill descriptions before.)
  2. Frontmatter consistency: name matches the file/dir name; tools and model values
     come from the known sets (catches typos that silently grant/deny nothing).
  3. Body/frontmatter drift: an agent body that says to use "the `X` tool" while X is
     missing from its tools list mandates a step the agent cannot execute
     (the unit-implementer/Skill bug).
  4. Name references: "`x` agent" / "`x` skill" phrases must point at files that exist —
     a rename otherwise silently breaks every referencing prompt.
  5. Description budget: always-loaded description fields stay under DESC_MAX_CHARS so
     per-session token bloat can't creep back in one edit at a time.
  6. Every `Docs/...*.md` path referenced from .claude/, AGENTS.md, or CLAUDE.md exists.
  7. .claude/settings.json parses and its permission rules have valid shape.
  8. Every hook command named in settings.json points at a file that exists.

Run from anywhere: paths resolve relative to the repo root.
"""

import json
import re
import sys
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[2]
errors: list[str] = []

KNOWN_TOOLS = {
    "Read", "Grep", "Glob", "Edit", "Write", "Bash", "PowerShell", "Skill",
    "Task", "Agent", "WebFetch", "WebSearch", "NotebookEdit", "TodoWrite",
    "AskUserQuestion", "LSP",
}
KNOWN_MODELS = {"inherit", "sonnet", "opus", "haiku", "fable"}
# Platform-provided agent types that repo prompts may legitimately reference.
PLATFORM_AGENTS = {"general-purpose", "claude"}
DESC_MAX_CHARS = 1000

TOOL_MENTION = re.compile(
    r"[`\"](" + "|".join(sorted(KNOWN_TOOLS)) + r")[`\"]\s+tool"
)
NEGATIVE_CONTEXT = re.compile(r"\b(never|not|don't|do not|no|without|instead of)\b[^.]{0,60}$", re.I)
NAME_REF = re.compile(r"`([a-z0-9][a-z0-9-]*)` (agent|skill)")


def rel(path: Path) -> str:
    return str(path.relative_to(ROOT))


def split_frontmatter(path: Path) -> tuple[dict | None, str]:
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---\n"):
        errors.append(f"{rel(path)}: missing YAML frontmatter")
        return None, text
    end = text.find("\n---", 4)
    if end < 0:
        errors.append(f"{rel(path)}: unterminated frontmatter")
        return None, text
    body = text[end + 4:]
    try:
        data = yaml.safe_load(text[4:end])
    except yaml.YAMLError as exc:
        errors.append(f"{rel(path)}: frontmatter is not valid YAML: {exc}")
        return None, body
    if not isinstance(data, dict):
        errors.append(f"{rel(path)}: frontmatter did not parse to a mapping")
        return None, body
    return data, body


def check_desc_style(path: Path) -> None:
    """Descriptions must use the folded block scalar form (`description: >-`).

    Standard per CLAUDE.md "Harness file conventions": a plain scalar silently breaks the
    whole frontmatter as soon as the text contains ": " — that dropped 2 agents and 3
    skill descriptions before this check existed. The block form is immune.
    """
    text = path.read_text(encoding="utf-8")
    end = text.find("\n---", 4)
    if end < 0:
        return  # already reported by split_frontmatter
    if not re.search(r"^description: >-\s*$", text[4:end], re.M):
        errors.append(
            f"{rel(path)}: description must use the folded block scalar form "
            f"('description: >-' + indented lines) — see CLAUDE.md, Harness file conventions"
        )


def check_common_frontmatter(path: Path, data: dict, expected_name: str) -> None:
    for key in ("name", "description"):
        value = data.get(key)
        if not isinstance(value, str) or not value.strip():
            errors.append(f"{rel(path)}: frontmatter missing non-empty '{key}'")
            return
    if data["name"] != expected_name:
        errors.append(
            f"{rel(path)}: frontmatter name '{data['name']}' does not match '{expected_name}'"
        )
    if len(data["description"]) > DESC_MAX_CHARS:
        errors.append(
            f"{rel(path)}: description is {len(data['description'])} chars "
            f"(max {DESC_MAX_CHARS}) — it loads into every session; move detail into the body"
        )


def parse_tools(raw: str) -> list[str]:
    return [t.strip() for t in raw.split(",") if t.strip()]


agent_names: set[str] = set()
skill_names: set[str] = set()
agent_files = sorted((ROOT / ".claude" / "agents").glob("*.md"))
skill_files = sorted((ROOT / ".claude" / "skills").glob("*/SKILL.md"))
agent_names.update(p.stem for p in agent_files)
skill_names.update(p.parent.name for p in skill_files)

for agent_file in agent_files:
    data, body = split_frontmatter(agent_file)
    if data is None:
        continue
    check_common_frontmatter(agent_file, data, agent_file.stem)
    check_desc_style(agent_file)

    model = data.get("model")
    if model is not None and model not in KNOWN_MODELS and not str(model).startswith("claude-"):
        errors.append(f"{rel(agent_file)}: unknown model '{model}' (expected one of {sorted(KNOWN_MODELS)} or a claude-* id)")

    raw_tools = data.get("tools")
    if isinstance(raw_tools, str):
        granted = parse_tools(raw_tools)
        for tool in granted:
            if tool not in KNOWN_TOOLS:
                errors.append(f"{rel(agent_file)}: unknown tool '{tool}' in tools list")
        # Body/frontmatter drift: a body that mandates "the `X` tool" needs X granted.
        for match in TOOL_MENTION.finditer(body):
            tool = match.group(1)
            if tool in granted:
                continue
            if NEGATIVE_CONTEXT.search(body[max(0, match.start() - 80):match.start()]):
                continue  # "never use the `Edit` tool" is a prohibition, not a mandate
            errors.append(
                f"{rel(agent_file)}: body references the `{tool}` tool but frontmatter "
                f"tools list does not grant it (tools: {raw_tools})"
            )

for skill_file in skill_files:
    data, _body = split_frontmatter(skill_file)
    if data is None:
        continue
    check_common_frontmatter(skill_file, data, skill_file.parent.name)
    check_desc_style(skill_file)

# "`x` agent" / "`x` skill" phrases must resolve — catches renames breaking prompts.
known_agents = agent_names | PLATFORM_AGENTS
name_ref_sources = [ROOT / "CLAUDE.md", ROOT / "AGENTS.md", *agent_files, *skill_files]
for source in name_ref_sources:
    if not source.is_file():
        continue
    text = source.read_text(encoding="utf-8")
    for name, kind in sorted(set(NAME_REF.findall(text))):
        pool = known_agents if kind == "agent" else skill_names
        if name not in pool:
            errors.append(f"{rel(source)}: references `{name}` {kind}, but no such {kind} exists")

# Concrete Docs/ file references (globs, placeholders, and anchors are excluded by the
# character class: '*', '<', and '#' terminate a match). A file may name a doc that is
# deliberately not written yet (e.g. a draft-skeleton skill pointing at its future
# standards doc) by declaring it: <!-- lint-allow-missing: Docs/path/to/file.md -->
DOC_REF = re.compile(r"Docs/[A-Za-z0-9_\-./]+\.(?:md|mmd)")
ALLOW_MISSING = re.compile(r"<!--\s*lint-allow-missing:\s*(Docs/[A-Za-z0-9_\-./]+\.(?:md|mmd))\s*-->")
ref_sources = [ROOT / "AGENTS.md", ROOT / "CLAUDE.md"]
# Compare parts relative to ROOT — the repo itself may be checked out inside a
# .claude/worktrees/ directory, which must not disable the scan.
ref_sources += sorted(
    p for p in (ROOT / ".claude").rglob("*.md")
    if "worktrees" not in p.relative_to(ROOT).parts
)
for source in ref_sources:
    if not source.is_file():
        continue
    text = source.read_text(encoding="utf-8")
    allowed_missing = set(ALLOW_MISSING.findall(text))
    for ref in sorted(set(DOC_REF.findall(text)) - allowed_missing):
        if not (ROOT / ref).is_file():
            errors.append(f"{rel(source)}: broken reference {ref}")

# settings.json must parse, its permission rules must have a plausible shape, and every
# hook it registers must point at a file that exists — a hook path typo disables the hook
# silently, which is exactly the class of failure the hooks themselves exist to prevent.
settings_path = ROOT / ".claude" / "settings.json"
if settings_path.is_file():
    try:
        settings = json.loads(settings_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        errors.append(f".claude/settings.json: invalid JSON: {exc}")
        settings = {}
    rule_shape = re.compile(r"^[A-Za-z][A-Za-z0-9_]*(\(.*\))?$|^mcp__[A-Za-z0-9_-]+(__[A-Za-z0-9_-]+)?$")
    for section in ("allow", "deny", "ask"):
        for entry in settings.get("permissions", {}).get(section, []):
            if not isinstance(entry, str) or not rule_shape.match(entry):
                errors.append(f".claude/settings.json: malformed permissions.{section} rule: {entry!r}")

    HOOK_PATH = re.compile(r"\.claude/hooks/[A-Za-z0-9_\-.]+")
    for event, matchers in (settings.get("hooks") or {}).items():
        for matcher in matchers:
            for hook in matcher.get("hooks", []):
                for hook_path in HOOK_PATH.findall(hook.get("command", "")):
                    if not (ROOT / hook_path).is_file():
                        errors.append(
                            f".claude/settings.json: hooks.{event} references {hook_path}, "
                            f"which does not exist"
                        )

if errors:
    print(f"AI harness lint: {len(errors)} problem(s)")
    for error in errors:
        print(f"  - {error}")
    sys.exit(1)
print("AI harness lint: OK")

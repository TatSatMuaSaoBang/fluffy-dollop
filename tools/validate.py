#!/usr/bin/env python3
"""Checks that the VN Talks add-on and the VNApproach mod agree with each other.
Run from the zip root:  python tools/validate.py
Prints OK or a list of problems. Needs only Python 3."""
import json, re, sys, pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
ADDON = ROOT / "Addons" / "VN_Talks"
ASSETS = ROOT / "Mods" / "VNApproach" / "assets"
problems = []

def strip_comments(text):
    """Remove // and /* */ comments but leave strings alone."""
    out, i, n, in_str = [], 0, len(text), False
    while i < n:
        c = text[i]
        if in_str:
            out.append(c)
            if c == "\\" and i + 1 < n:
                out.append(text[i + 1]); i += 1
            elif c == '"':
                in_str = False
        elif c == '"':
            in_str = True; out.append(c)
        elif text.startswith("//", i):
            while i < n and text[i] != "\n": i += 1
            continue
        elif text.startswith("/*", i):
            i = text.index("*/", i) + 2
            continue
        else:
            out.append(c)
        i += 1
    return "".join(out)

def load(path):
    try:
        return json.loads(strip_comments(path.read_text(encoding="utf-8")))
    except Exception as e:
        problems.append(f"{path.name}: invalid JSON ({e})")
        return None

def norm(s):  # {{NpcName}} and {NpcName} are the same thing
    return re.sub(r"\s+", " ", s.replace("{{NpcName}}", "{NpcName}")).strip()

def walk(node):
    """Yield every dict inside a JSON value."""
    if isinstance(node, dict):
        yield node
        for v in node.values(): yield from walk(v)
    elif isinstance(node, list):
        for v in node: yield from walk(v)

interact = load(ADDON / "interact.json")
mail = load(ASSETS / "mail.json")
rules = load(ASSETS / "rules.json")
load(ROOT / "Mods" / "VNApproach" / "manifest.json")
if not (interact and mail is not None and rules is not None):
    print("\n".join(problems)); sys.exit(1)

ids, sets, checks, mail_used, chapters = {}, set(), set(), set(), {}
for inc in interact["Include"]:
    f = ADDON / inc
    if not f.exists():
        problems.append(f"interact.json includes {inc}, file missing"); continue
    data = load(f)
    if data is None: continue
    for npc in data.get("Npcs", []):
        if npc not in interact["Npcs"]:
            problems.append(f"{inc}: villager {npc} missing from Npcs in interact.json")
    for d in walk(data.get("Actions", [])):
        if "Id" in d and "Label" in d:
            if d["Id"] in ids: problems.append(f"{inc}: duplicate Id {d['Id']} (also in {ids[d['Id']]})")
            ids[d["Id"]] = inc
        for flag in d.get("SetFlags", []): sets.add(flag)
        for t in d.get("TriggerActions", []):
            m = re.match(r"AddMail Current (\S+)", t)
            if m: mail_used.add(m.group(1))
        c = d.get("Conditions")
        if isinstance(c, str):
            for m in re.finditer(r"HAS_FLAG ([^\s,]+)", c): checks.add(m.group(1))
            m = re.match(r"(\w+)_Ch([123])$", d.get("Id", ""))
            if m and "Label" in d: chapters[(m.group(1), int(m.group(2)))] = norm(c)

for flag in checks:
    if flag not in sets:
        problems.append(f"flag {flag} is checked in a Condition but never set by SetFlags")
for m in mail_used - set(mail): problems.append(f"AddMail uses {m} but assets/mail.json has no such letter")
for m in set(mail) - mail_used: problems.append(f"letter {m} in mail.json is never sent by any chapter")
for npc in interact["Npcs"]:
    for n in (1, 2, 3):
        if (npc, n) not in chapters: problems.append(f"{npc}: chapter {n} action ({npc}_Ch{n}) not found")

# the approach rules must repeat the chapter conditions exactly
for n in (1, 2, 3):
    rule = rules.get(f"Chapter{n}")
    if not rule: problems.append(f"rules.json: Chapter{n} missing"); continue
    for npc in interact["Npcs"]:
        want = chapters.get((npc, n))
        if want and norm(rule.get("Conditions", "")) != want:
            problems.append(f"rules.json Chapter{n} Conditions differ from {npc}_Ch{n}:\n   rule:    {norm(rule.get('Conditions',''))}\n   chapter: {want}")
    if rule.get("Npcs") != ["*"]:
        problems.append(f"rules.json Chapter{n}: Npcs should be [\"*\"] while every villager has this chapter")

print("OK: everything agrees." if not problems else "PROBLEMS:\n- " + "\n- ".join(problems))
sys.exit(1 if problems else 0)

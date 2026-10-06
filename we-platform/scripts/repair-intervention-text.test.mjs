import assert from "node:assert/strict";
import test from "node:test";
import { repairInterventionText } from "./lib/repair-intervention-text.mjs";

const isolate = "00000000-0000-4000-8000-000000001004";
const names = new Map([[isolate, "Isolate the variable"]]);

test("rewrites skill guids and 8-character hex ids to names", () => {
  const stored =
    "Address medium severity gap in micro-skill 00000000-0000-4000-8000-000000001004.\n\n" +
    "Micro-skill 00000000-0000-4000-8000-000000001004 has a medium-severity gap.";

  assert.equal(
    repairInterventionText(stored, names),
    "Address medium severity gap in micro-skill Isolate the variable.\n\n" +
      "Micro-skill Isolate the variable has a medium-severity gap."
  );
});

test("rewrites a unique 8-character prefix and leaves a shared prefix unnamed", () => {
  const shared = new Map([
    ["00000000-0000-4000-8000-000000001001", "Read an equation"],
    ["00000000-0000-4000-8000-000000001004", "Isolate the variable"],
    ["abcdef12-0000-4000-8000-000000000099", "Read an equation"],
  ]);
  assert.equal(
    repairInterventionText("skill abcdef12.", shared),
    "skill Read an equation."
  );
  assert.equal(
    repairInterventionText("id 00000000.", shared),
    "id Unknown skill."
  );
});

test("uses Unknown skill when an id does not match one name", () => {
  assert.equal(
    repairInterventionText("gap in micro-skill abcdef12.", names),
    "gap in micro-skill Unknown skill."
  );
  assert.equal(
    repairInterventionText(
      "gap in 11111111-1111-1111-1111-111111111111.",
      names
    ),
    "gap in Unknown skill."
  );
});

test("leaves text that already uses names unchanged", () => {
  const clean = "Address the gap in Isolate the variable.";
  assert.equal(repairInterventionText(clean, names), clean);
  const repaired = repairInterventionText(
    `micro-skill ${isolate}.`,
    names
  );
  assert.equal(repairInterventionText(repaired, names), repaired);
});

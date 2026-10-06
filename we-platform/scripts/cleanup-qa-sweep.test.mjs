import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const script = fs.readFileSync(
  path.join(path.dirname(fileURLToPath(import.meta.url)), "cleanup-qa-sweep.sh"),
  "utf8"
);

const emails = [
  "qa1006.student@school.local",
  "qa1006.parent@school.local",
  "qa1006.teacher@school.local",
  "qa1006.schoolleader@school.local",
  "qa1006.educationauthorityofficer@school.local",
  "qa1006.federationadmin@school.local",
  "qa1006.systemadministrator@school.local",
];

test("qa sweep cleanup deactivates the sweep accounts and removes the leftover rows", () => {
  for (const email of emails) {
    assert.match(script, new RegExp(email.replaceAll(".", "\\.")));
  }
  assert.match(script, /is_active\s*=\s*false/);
  assert.doesNotMatch(script, /delete\s+from\s+"AspNetUsers"/i);

  assert.match(script, /QA Year 1006/);
  assert.match(script, /'Y'/);
  assert.match(script, /QA1006/);
  assert.match(script, /class_teachers/);
  assert.match(script, /class_enrollments/);
  assert.match(script, /parent_student_links/);

  for (const title of ["QA check 1006b", "QA check 1006c", "QA check 1006", "QA"]) {
    assert.match(script, new RegExp(`'${title}'`));
  }
  assert.match(script, /CHEM/);
  assert.match(script, /qa-policy/);
  assert.doesNotMatch(script, /00000000-0000-4000-8000-000000000111/);
});

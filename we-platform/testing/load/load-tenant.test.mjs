import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.dirname(fileURLToPath(import.meta.url));
const scenario = fs.readFileSync(path.join(root, "scenarios/approve-evidence.js"), "utf8");
const tenant = fs.readFileSync(path.join(root, "lib/load-tenant.js"), "utf8");
const seeder = fs.readFileSync(
  path.join(root, "../../services/identity-service/src/Infrastructure/Data/IdentityDataSeeder.cs"),
  "utf8"
);
const cleanup = fs.readFileSync(
  path.join(root, "../../scripts/cleanup-load-evidence.sh"),
  "utf8"
);

const demoOrg = "00000000-0000-4000-8000-000000000001";
const demoClass = "00000000-0000-4000-8000-000000000111";
const demoStudent = "22222222-2222-2222-2222-222222222222";
const loadOrg = "00000000-0000-4000-8000-0000000000a1";
const loadClass = "00000000-0000-4000-8000-0000000000a3";

test("evidence approval load writes into its own tenant and class", () => {
  assert.match(scenario, /loadTenant/);
  assert.doesNotMatch(scenario, /demo\.(organisationId|classId|studentUserId|emails)/);
  assert.doesNotMatch(scenario, new RegExp(demoOrg));
  assert.doesNotMatch(scenario, new RegExp(demoClass));
  assert.doesNotMatch(scenario, new RegExp(demoStudent));

  assert.match(tenant, new RegExp(loadOrg));
  assert.match(tenant, new RegExp(loadClass));
  assert.doesNotMatch(tenant, new RegExp(demoOrg));
  assert.doesNotMatch(tenant, new RegExp(demoClass));
  assert.doesNotMatch(tenant, new RegExp(demoStudent));

  assert.match(seeder, /load-teacher@load\.local/);
  assert.match(seeder, /load-student@load\.local/);
  assert.match(seeder, new RegExp(loadOrg));

  assert.match(cleanup, /Load evidence VU/);
  assert.match(cleanup, new RegExp(demoOrg));
  assert.doesNotMatch(cleanup, new RegExp(loadOrg));
});

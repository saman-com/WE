#!/usr/bin/env node
// Rewrite intervention text saved before skill ids were replaced with names.
// Idempotent: a second run finds nothing left to change.
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { repairInterventionText } from "./lib/repair-intervention-text.mjs";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");

function psql(database, sql) {
  return execFileSync(
    "docker",
    ["compose", "exec", "-T", "postgres", "psql", "-U", "we", "-d", database, "-v", "ON_ERROR_STOP=1", "-At", "-c", sql],
    { cwd: root, encoding: "utf8" }
  ).trim();
}

function loadNames() {
  const names = new Map();
  const skills = psql("we_curriculum", "select id::text || chr(31) || name from micro_skills");
  const objectives = psql(
    "we_curriculum",
    "select id::text || chr(31) || title from learning_objectives"
  );
  for (const line of `${skills}\n${objectives}`.split("\n")) {
    if (!line) {
      continue;
    }
    const [id, name] = line.split("\u001f");
    if (id && name) {
      names.set(id, name);
    }
  }

  const seed = fs.readFileSync(path.join(root, "scripts/seed-demo.sh"), "utf8");
  const constants = [...seed.matchAll(/^SKILL_[A-Z_]+=([0-9a-f-]{36})/gm)].map((match) => match[1]);
  const declared = seed.match(/for skill in ((?:"[^"]+"\s*)+);/);
  const declaredNames = declared ? [...declared[1].matchAll(/"([^"]+)"/g)].map((match) => match[1]) : [];
  constants.forEach((id, index) => {
    const name = declaredNames[index];
    if (name && !names.has(id)) {
      names.set(id, name);
    }
  });

  return names;
}

function loadRows() {
  const raw = psql(
    "we_interventions",
    "select coalesce(json_agg(json_build_object('id', id, 'planned', planned_actions, 'notes', notes, 'outcome', outcome)), '[]'::json) from interventions"
  );
  return JSON.parse(raw);
}

function countMarked(rows, names) {
  return rows.filter((row) =>
    ["planned", "notes", "outcome"].some((field) => {
      const value = row[field] ?? "";
      return repairInterventionText(value, names) !== value;
    })
  ).length;
}

const names = loadNames();
const beforeRows = loadRows();
const before = countMarked(beforeRows, names);
const changed = [];

for (const row of beforeRows) {
  const planned = repairInterventionText(row.planned ?? "", names);
  const notes = repairInterventionText(row.notes ?? "", names);
  const outcome = row.outcome == null ? null : repairInterventionText(row.outcome, names);
  if (planned === (row.planned ?? "") && notes === (row.notes ?? "") && outcome === row.outcome) {
    continue;
  }
  changed.push({ id: row.id, planned, notes, outcome });
}

if (changed.length > 0) {
  const statements = changed.map((row) => {
    const tag = `r_${row.id.replaceAll("-", "")}`;
    const outcomeSql = row.outcome == null ? "null" : `$${tag}o$${row.outcome}$${tag}o$`;
    return `update interventions set planned_actions = $${tag}p$${row.planned}$${tag}p$, notes = $${tag}n$${row.notes}$${tag}n$, outcome = ${outcomeSql}, updated_at = now() where id = '${row.id}';`;
  });
  const sql = `begin;\n${statements.join("\n")}\ncommit;\n`;
  execFileSync(
    "docker",
    ["compose", "exec", "-T", "postgres", "psql", "-U", "we", "-d", "we_interventions", "-v", "ON_ERROR_STOP=1", "-q"],
    { cwd: root, input: sql }
  );
}

const after = countMarked(loadRows(), names);
console.log(`names loaded: ${names.size}`);
console.log(`intervention rows: ${beforeRows.length}`);
console.log(`rows with skill ids before: ${before}`);
console.log(`rows rewritten: ${changed.length}`);
console.log(`rows with skill ids after: ${after}`);

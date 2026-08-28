-- V002: learning objectives linked to units, micro-skills linked to learning objectives

CREATE TABLE IF NOT EXISTS learning_objectives (
    id UUID PRIMARY KEY,
    unit_id UUID NOT NULL REFERENCES units (id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    sort_order INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS micro_skills (
    id UUID PRIMARY KEY,
    learning_objective_id UUID NOT NULL REFERENCES learning_objectives (id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    sort_order INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_learning_objectives_unit_id ON learning_objectives (unit_id);
CREATE INDEX IF NOT EXISTS ix_micro_skills_learning_objective_id ON micro_skills (learning_objective_id);

-- V001: curricula scoped to organisation, subject → unit → topic hierarchy

CREATE TABLE IF NOT EXISTS curricula (
    id UUID PRIMARY KEY,
    organisation_id UUID NOT NULL,
    name TEXT NOT NULL,
    version TEXT NOT NULL,
    status TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS subjects (
    id UUID PRIMARY KEY,
    curriculum_id UUID NOT NULL REFERENCES curricula (id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    code TEXT NOT NULL,
    sort_order INTEGER NOT NULL,
    UNIQUE (curriculum_id, code)
);

CREATE TABLE IF NOT EXISTS units (
    id UUID PRIMARY KEY,
    subject_id UUID NOT NULL REFERENCES subjects (id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    sort_order INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS topics (
    id UUID PRIMARY KEY,
    unit_id UUID NOT NULL REFERENCES units (id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    sort_order INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_curricula_organisation_id ON curricula (organisation_id);
CREATE INDEX IF NOT EXISTS ix_subjects_curriculum_id ON subjects (curriculum_id);
CREATE INDEX IF NOT EXISTS ix_units_subject_id ON units (subject_id);
CREATE INDEX IF NOT EXISTS ix_topics_unit_id ON topics (unit_id);

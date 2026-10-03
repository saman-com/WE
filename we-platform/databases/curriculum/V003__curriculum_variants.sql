-- V003: curriculum variants — regional scope, inheritance, and node override provenance

ALTER TABLE curricula
    ADD COLUMN IF NOT EXISTS region_code TEXT NULL,
    ADD COLUMN IF NOT EXISTS scope TEXT NOT NULL DEFAULT 'School',
    ADD COLUMN IF NOT EXISTS parent_curriculum_id UUID NULL REFERENCES curricula (id);

CREATE INDEX IF NOT EXISTS ix_curricula_region_code ON curricula (region_code);
CREATE INDEX IF NOT EXISTS ix_curricula_parent_curriculum_id ON curricula (parent_curriculum_id);

ALTER TABLE subjects
    ADD COLUMN IF NOT EXISTS source_node_id UUID NULL,
    ADD COLUMN IF NOT EXISTS is_overridden BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE units
    ADD COLUMN IF NOT EXISTS source_node_id UUID NULL,
    ADD COLUMN IF NOT EXISTS is_overridden BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE topics
    ADD COLUMN IF NOT EXISTS source_node_id UUID NULL,
    ADD COLUMN IF NOT EXISTS is_overridden BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE learning_objectives
    ADD COLUMN IF NOT EXISTS source_node_id UUID NULL,
    ADD COLUMN IF NOT EXISTS is_overridden BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE micro_skills
    ADD COLUMN IF NOT EXISTS source_node_id UUID NULL,
    ADD COLUMN IF NOT EXISTS is_overridden BOOLEAN NOT NULL DEFAULT FALSE;

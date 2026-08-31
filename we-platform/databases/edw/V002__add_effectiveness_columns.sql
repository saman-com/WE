ALTER TABLE fact_evidence
    ADD COLUMN IF NOT EXISTS subject_id UUID,
    ADD COLUMN IF NOT EXISTS subject_name VARCHAR(256),
    ADD COLUMN IF NOT EXISTS unit_id UUID,
    ADD COLUMN IF NOT EXISTS unit_name VARCHAR(256),
    ADD COLUMN IF NOT EXISTS mastered_micro_skill_count INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_micro_skill_count INTEGER NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS idx_fact_evidence_subject_id ON fact_evidence (subject_id);
CREATE INDEX IF NOT EXISTS idx_fact_evidence_unit_id ON fact_evidence (unit_id);

ALTER TABLE fact_intervention
    ADD COLUMN IF NOT EXISTS intervention_type VARCHAR(64);

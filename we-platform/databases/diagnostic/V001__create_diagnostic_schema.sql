CREATE TABLE IF NOT EXISTS micro_skill_diagnostics (
    id UUID PRIMARY KEY,
    student_user_id TEXT NOT NULL,
    organisation_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    assessment_id UUID NOT NULL,
    micro_skill_id UUID NOT NULL,
    status TEXT NOT NULL,
    mark NUMERIC(3, 1) NOT NULL,
    reason TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    UNIQUE (evidence_id, micro_skill_id)
);

CREATE INDEX IF NOT EXISTS idx_micro_skill_diagnostics_student_user_id
    ON micro_skill_diagnostics (student_user_id);

CREATE TABLE IF NOT EXISTS learning_gaps (
    id UUID PRIMARY KEY,
    student_user_id TEXT NOT NULL,
    organisation_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    assessment_id UUID NOT NULL,
    micro_skill_id UUID NOT NULL,
    learning_objective_id UUID,
    expected_mastery TEXT NOT NULL,
    actual_mastery TEXT NOT NULL,
    mark NUMERIC(3, 1) NOT NULL,
    severity TEXT NOT NULL,
    urgency TEXT NOT NULL,
    explanation TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    UNIQUE (evidence_id, micro_skill_id)
);

CREATE INDEX IF NOT EXISTS idx_learning_gaps_student_user_id ON learning_gaps (student_user_id);

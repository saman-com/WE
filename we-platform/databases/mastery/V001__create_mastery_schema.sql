CREATE TABLE IF NOT EXISTS mastery_evidence_marks (
    id UUID PRIMARY KEY,
    student_user_id TEXT NOT NULL,
    organisation_id UUID NOT NULL,
    micro_skill_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    assessment_id UUID NOT NULL,
    mark NUMERIC(3, 1) NOT NULL,
    weight NUMERIC(5, 2) NOT NULL DEFAULT 1.0,
    recorded_at TIMESTAMPTZ NOT NULL,
    UNIQUE (evidence_id, micro_skill_id)
);

CREATE INDEX IF NOT EXISTS idx_mastery_evidence_marks_student_micro_skill
    ON mastery_evidence_marks (student_user_id, micro_skill_id);

CREATE TABLE IF NOT EXISTS mastery_records (
    id UUID PRIMARY KEY,
    student_user_id TEXT NOT NULL,
    organisation_id UUID NOT NULL,
    micro_skill_id UUID NOT NULL,
    mastery_level TEXT NOT NULL,
    weighted_average NUMERIC(5, 2) NOT NULL,
    confidence_score NUMERIC(5, 2) NOT NULL,
    evidence_count INT NOT NULL,
    explanation TEXT NOT NULL,
    calculated_at TIMESTAMPTZ NOT NULL,
    UNIQUE (student_user_id, micro_skill_id)
);

CREATE INDEX IF NOT EXISTS idx_mastery_records_student_user_id ON mastery_records (student_user_id);

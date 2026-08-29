CREATE TABLE IF NOT EXISTS educational_evidence (
    id UUID PRIMARY KEY,
    organisation_id UUID NOT NULL,
    class_id UUID NOT NULL,
    assessment_id UUID NOT NULL,
    submission_id UUID NOT NULL UNIQUE,
    student_user_id TEXT NOT NULL,
    title TEXT NOT NULL,
    status TEXT NOT NULL,
    approved_by_teacher_user_id TEXT NOT NULL,
    approved_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS evidence_micro_skill_marks (
    evidence_id UUID NOT NULL REFERENCES educational_evidence(id),
    micro_skill_id UUID NOT NULL,
    mark NUMERIC NOT NULL,
    feedback TEXT NOT NULL,
    PRIMARY KEY (evidence_id, micro_skill_id)
);

CREATE INDEX IF NOT EXISTS idx_educational_evidence_assessment_id
    ON educational_evidence(assessment_id);
CREATE INDEX IF NOT EXISTS idx_educational_evidence_student_user_id
    ON educational_evidence(student_user_id);

CREATE OR REPLACE FUNCTION prevent_approved_evidence_mutation()
RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'Approved evidence is immutable';
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS educational_evidence_immutable ON educational_evidence;
CREATE TRIGGER educational_evidence_immutable
    BEFORE UPDATE OR DELETE ON educational_evidence
    FOR EACH ROW EXECUTE FUNCTION prevent_approved_evidence_mutation();

DROP TRIGGER IF EXISTS evidence_micro_skill_marks_immutable ON evidence_micro_skill_marks;
CREATE TRIGGER evidence_micro_skill_marks_immutable
    BEFORE UPDATE OR DELETE ON evidence_micro_skill_marks
    FOR EACH ROW EXECUTE FUNCTION prevent_approved_evidence_mutation();

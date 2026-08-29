\c we_assessment

CREATE TABLE assessment_submissions (
    id UUID PRIMARY KEY,
    assessment_id UUID NOT NULL REFERENCES assessments (id) ON DELETE CASCADE,
    student_user_id TEXT NOT NULL,
    responses TEXT NOT NULL,
    status TEXT NOT NULL,
    is_late BOOLEAN NOT NULL DEFAULT FALSE,
    submitted_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    UNIQUE (assessment_id, student_user_id)
);

CREATE INDEX idx_assessment_submissions_assessment_id ON assessment_submissions (assessment_id);
CREATE INDEX idx_assessment_submissions_student_user_id ON assessment_submissions (student_user_id);

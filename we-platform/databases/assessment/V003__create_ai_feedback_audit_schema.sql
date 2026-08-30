\c we_assessment

CREATE TABLE ai_feedback_audit_logs (
    id UUID PRIMARY KEY,
    assessment_id UUID NOT NULL REFERENCES assessments (id) ON DELETE CASCADE,
    submission_id UUID NOT NULL REFERENCES assessment_submissions (id) ON DELETE CASCADE,
    micro_skill_id UUID NOT NULL,
    teacher_user_id TEXT NOT NULL,
    prompt_id TEXT NOT NULL,
    prompt_version TEXT NOT NULL,
    prompt_variables_json TEXT NOT NULL,
    ai_response TEXT NOT NULL,
    teacher_edited_feedback TEXT,
    evidence_id UUID,
    final_approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_ai_feedback_audit_logs_submission_id ON ai_feedback_audit_logs (submission_id);
CREATE INDEX idx_ai_feedback_audit_logs_teacher_user_id ON ai_feedback_audit_logs (teacher_user_id);

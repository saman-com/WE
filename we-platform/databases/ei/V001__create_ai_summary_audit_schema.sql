\c we_ei

CREATE TABLE ai_summary_audit_logs (
    id UUID PRIMARY KEY,
    summary_type TEXT NOT NULL,
    organisation_id UUID,
    class_id UUID,
    unit_id UUID,
    student_user_id TEXT,
    teacher_user_id TEXT NOT NULL,
    prompt_id TEXT NOT NULL,
    prompt_version TEXT NOT NULL,
    prompt_variables_json TEXT NOT NULL,
    context_scope_json TEXT NOT NULL,
    ai_response TEXT NOT NULL,
    teacher_edited_content TEXT,
    final_approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_ai_summary_audit_logs_teacher_user_id ON ai_summary_audit_logs (teacher_user_id);
CREATE INDEX idx_ai_summary_audit_logs_class_id ON ai_summary_audit_logs (class_id);
CREATE INDEX idx_ai_summary_audit_logs_student_user_id ON ai_summary_audit_logs (student_user_id);

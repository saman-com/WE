\c we_ai_gateway

CREATE TABLE ai_audit_logs (
    id UUID PRIMARY KEY,
    caller_user_id TEXT NOT NULL,
    prompt_id TEXT NOT NULL,
    prompt_version TEXT NOT NULL,
    context_scope TEXT NOT NULL,
    prompt_variables_json TEXT NOT NULL,
    ai_response TEXT NOT NULL,
    outcome TEXT NOT NULL,
    block_reason TEXT,
    provider_name TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_ai_audit_logs_caller_user_id ON ai_audit_logs (caller_user_id);
CREATE INDEX idx_ai_audit_logs_prompt_id ON ai_audit_logs (prompt_id);
CREATE INDEX idx_ai_audit_logs_outcome ON ai_audit_logs (outcome);
CREATE INDEX idx_ai_audit_logs_created_at ON ai_audit_logs (created_at DESC);

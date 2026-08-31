CREATE TABLE IF NOT EXISTS generated_reports (
    id UUID PRIMARY KEY,
    report_type VARCHAR(64) NOT NULL,
    organisation_id UUID NOT NULL,
    class_id UUID NULL,
    requested_by_user_id VARCHAR(128) NOT NULL,
    content_json JSONB NOT NULL,
    generated_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_generated_reports_organisation_id
    ON generated_reports (organisation_id);

CREATE INDEX IF NOT EXISTS idx_generated_reports_requested_by_user_id
    ON generated_reports (requested_by_user_id);

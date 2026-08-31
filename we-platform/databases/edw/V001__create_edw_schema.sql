CREATE TABLE IF NOT EXISTS dim_time (
    date_key INTEGER PRIMARY KEY,
    calendar_date DATE NOT NULL,
    year INTEGER NOT NULL,
    month INTEGER NOT NULL,
    day INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS fact_evidence (
    event_id UUID PRIMARY KEY,
    evidence_id UUID NOT NULL,
    organisation_id UUID NOT NULL,
    assessment_id UUID NOT NULL,
    submission_id UUID NOT NULL,
    student_user_id VARCHAR(128) NOT NULL,
    class_id UUID NOT NULL,
    approved_by_teacher_user_id VARCHAR(128) NOT NULL,
    approved_at TIMESTAMPTZ NOT NULL,
    time_key INTEGER NOT NULL REFERENCES dim_time (date_key),
    micro_skill_count INTEGER NOT NULL,
    ingested_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_fact_evidence_organisation_id ON fact_evidence (organisation_id);
CREATE INDEX IF NOT EXISTS idx_fact_evidence_student_user_id ON fact_evidence (student_user_id);
CREATE INDEX IF NOT EXISTS idx_fact_evidence_time_key ON fact_evidence (time_key);

CREATE TABLE IF NOT EXISTS fact_assessment (
    event_id UUID PRIMARY KEY,
    assessment_id UUID NOT NULL,
    organisation_id UUID NOT NULL,
    submission_id UUID NOT NULL,
    evidence_id UUID NOT NULL,
    student_user_id VARCHAR(128) NOT NULL,
    approved_by_teacher_user_id VARCHAR(128) NOT NULL,
    approved_at TIMESTAMPTZ NOT NULL,
    time_key INTEGER NOT NULL REFERENCES dim_time (date_key),
    micro_skill_count INTEGER NOT NULL,
    ingested_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_fact_assessment_organisation_id ON fact_assessment (organisation_id);
CREATE INDEX IF NOT EXISTS idx_fact_assessment_student_user_id ON fact_assessment (student_user_id);
CREATE INDEX IF NOT EXISTS idx_fact_assessment_time_key ON fact_assessment (time_key);

CREATE TABLE IF NOT EXISTS fact_intervention (
    event_id UUID PRIMARY KEY,
    intervention_id UUID NOT NULL,
    organisation_id UUID NOT NULL,
    student_user_id VARCHAR(128) NOT NULL,
    learning_gap_id UUID NOT NULL,
    assigned_teacher_user_id VARCHAR(128) NOT NULL,
    status VARCHAR(32) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    time_key INTEGER NOT NULL REFERENCES dim_time (date_key),
    ingested_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_fact_intervention_organisation_id ON fact_intervention (organisation_id);
CREATE INDEX IF NOT EXISTS idx_fact_intervention_student_user_id ON fact_intervention (student_user_id);
CREATE INDEX IF NOT EXISTS idx_fact_intervention_time_key ON fact_intervention (time_key);

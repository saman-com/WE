CREATE TABLE IF NOT EXISTS ministry_api_keys (
    id UUID PRIMARY KEY,
    client_name VARCHAR(256) NOT NULL,
    key_hash VARCHAR(128) NOT NULL UNIQUE,
    scopes VARCHAR(512) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS regional_enrollment_facts (
    id UUID PRIMARY KEY,
    region_code VARCHAR(64) NOT NULL,
    school_code VARCHAR(64) NOT NULL,
    student_count INTEGER NOT NULL,
    teacher_count INTEGER NOT NULL,
    as_of_date DATE NOT NULL,
    UNIQUE (region_code, school_code, as_of_date)
);

CREATE TABLE IF NOT EXISTS regional_mastery_facts (
    id UUID PRIMARY KEY,
    region_code VARCHAR(64) NOT NULL,
    subject_code VARCHAR(64) NOT NULL,
    average_mastery_percent NUMERIC(8, 2) NOT NULL,
    mastered_share_percent NUMERIC(8, 2) NOT NULL,
    sample_size INTEGER NOT NULL,
    as_of_date DATE NOT NULL,
    UNIQUE (region_code, subject_code, as_of_date)
);

CREATE TABLE IF NOT EXISTS regional_coverage_facts (
    id UUID PRIMARY KEY,
    region_code VARCHAR(64) NOT NULL,
    curriculum_code VARCHAR(64) NOT NULL,
    covered_objective_percent NUMERIC(8, 2) NOT NULL,
    schools_reporting INTEGER NOT NULL,
    as_of_date DATE NOT NULL,
    UNIQUE (region_code, curriculum_code, as_of_date)
);

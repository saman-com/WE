CREATE TABLE IF NOT EXISTS regional_equity_facts (
    id UUID PRIMARY KEY,
    region_code VARCHAR(64) NOT NULL,
    demographic_dimension VARCHAR(64) NOT NULL,
    demographic_category VARCHAR(64) NOT NULL,
    average_mastery_percent NUMERIC(8, 2) NOT NULL,
    sample_size INTEGER NOT NULL,
    as_of_date DATE NOT NULL,
    UNIQUE (region_code, demographic_dimension, demographic_category, as_of_date)
);

CREATE TABLE IF NOT EXISTS regional_curriculum_effectiveness_facts (
    id UUID PRIMARY KEY,
    region_code VARCHAR(64) NOT NULL,
    curriculum_code VARCHAR(64) NOT NULL,
    subject_code VARCHAR(64) NOT NULL,
    mastery_rate_percent NUMERIC(8, 2) NOT NULL,
    coverage_percent NUMERIC(8, 2) NOT NULL,
    schools_reporting INTEGER NOT NULL,
    as_of_date DATE NOT NULL,
    UNIQUE (region_code, curriculum_code, subject_code, as_of_date)
);

CREATE TABLE IF NOT EXISTS regional_intervention_impact_facts (
    id UUID PRIMARY KEY,
    region_code VARCHAR(64) NOT NULL,
    intervention_type VARCHAR(128) NOT NULL,
    total_count INTEGER NOT NULL,
    successful_count INTEGER NOT NULL,
    average_growth_percent NUMERIC(8, 2) NOT NULL,
    as_of_date DATE NOT NULL,
    UNIQUE (region_code, intervention_type, as_of_date)
);

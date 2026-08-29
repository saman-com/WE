CREATE TABLE IF NOT EXISTS profile_evidence_entries (
    id UUID PRIMARY KEY,
    profile_id UUID NOT NULL REFERENCES student_learning_profiles(id) ON DELETE CASCADE,
    assessment_id UUID NOT NULL,
    title TEXT NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS profile_evidence_micro_skills (
    evidence_entry_id UUID NOT NULL REFERENCES profile_evidence_entries(id) ON DELETE CASCADE,
    micro_skill_id UUID NOT NULL,
    PRIMARY KEY (evidence_entry_id, micro_skill_id)
);

CREATE INDEX IF NOT EXISTS idx_profile_evidence_entries_profile_id
    ON profile_evidence_entries(profile_id);

\c we_assessment

CREATE TABLE assessments (
    id UUID PRIMARY KEY,
    organisation_id UUID NOT NULL,
    class_id UUID NOT NULL,
    created_by_teacher_user_id TEXT NOT NULL,
    title TEXT NOT NULL,
    instructions TEXT,
    due_at TIMESTAMPTZ,
    status TEXT NOT NULL,
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX idx_assessments_class_id ON assessments (class_id);
CREATE INDEX idx_assessments_organisation_id ON assessments (organisation_id);
CREATE INDEX idx_assessments_status ON assessments (status);

CREATE TABLE assessment_learning_objectives (
    assessment_id UUID NOT NULL REFERENCES assessments (id) ON DELETE CASCADE,
    learning_objective_id UUID NOT NULL,
    PRIMARY KEY (assessment_id, learning_objective_id)
);

CREATE TABLE assessment_micro_skills (
    assessment_id UUID NOT NULL REFERENCES assessments (id) ON DELETE CASCADE,
    micro_skill_id UUID NOT NULL,
    PRIMARY KEY (assessment_id, micro_skill_id)
);

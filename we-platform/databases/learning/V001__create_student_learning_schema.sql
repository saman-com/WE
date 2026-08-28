CREATE TABLE IF NOT EXISTS student_learning_profiles (
    id UUID PRIMARY KEY,
    student_user_id TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS profile_class_enrollments (
    id UUID PRIMARY KEY,
    profile_id UUID NOT NULL REFERENCES student_learning_profiles(id) ON DELETE CASCADE,
    organisation_id UUID NOT NULL,
    class_id UUID NOT NULL,
    class_name TEXT NOT NULL,
    class_code TEXT NOT NULL,
    enrolled_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (profile_id, class_id)
);

CREATE INDEX IF NOT EXISTS idx_profile_class_enrollments_profile_id
    ON profile_class_enrollments(profile_id);

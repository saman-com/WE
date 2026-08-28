-- V001: organisation, year levels, classes, teacher assignment, student enrollment

CREATE TABLE IF NOT EXISTS organisations (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    code TEXT NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS year_levels (
    id UUID PRIMARY KEY,
    organisation_id UUID NOT NULL REFERENCES organisations (id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    sort_order INTEGER NOT NULL,
    UNIQUE (organisation_id, name)
);

CREATE TABLE IF NOT EXISTS classes (
    id UUID PRIMARY KEY,
    organisation_id UUID NOT NULL REFERENCES organisations (id) ON DELETE CASCADE,
    year_level_id UUID NOT NULL REFERENCES year_levels (id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    code TEXT NOT NULL,
    UNIQUE (organisation_id, code)
);

CREATE TABLE IF NOT EXISTS class_teachers (
    class_id UUID NOT NULL REFERENCES classes (id) ON DELETE CASCADE,
    teacher_user_id TEXT NOT NULL,
    PRIMARY KEY (class_id, teacher_user_id)
);

CREATE TABLE IF NOT EXISTS class_enrollments (
    class_id UUID NOT NULL REFERENCES classes (id) ON DELETE CASCADE,
    student_user_id TEXT NOT NULL,
    PRIMARY KEY (class_id, student_user_id)
);

CREATE INDEX IF NOT EXISTS ix_year_levels_organisation_id ON year_levels (organisation_id);
CREATE INDEX IF NOT EXISTS ix_classes_organisation_id ON classes (organisation_id);
CREATE INDEX IF NOT EXISTS ix_classes_year_level_id ON classes (year_level_id);
CREATE INDEX IF NOT EXISTS ix_class_teachers_teacher_user_id ON class_teachers (teacher_user_id);
CREATE INDEX IF NOT EXISTS ix_class_enrollments_student_user_id ON class_enrollments (student_user_id);

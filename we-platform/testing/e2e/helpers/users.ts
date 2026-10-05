export const DEMO_PASSWORD = "Password123!";

export const users = {
  teacher: {
    email: "teacher@school.local",
    role: "Teacher",
    home: "/teacher",
    storageFile: "teacher.json",
    userId: "11111111-1111-1111-1111-111111111111",
  },
  student: {
    email: "student@school.local",
    role: "Student",
    home: "/student",
    storageFile: "student.json",
    userId: "22222222-2222-2222-2222-222222222222",
  },
  parent: {
    email: "parent@school.local",
    role: "Parent",
    home: "/parent",
    storageFile: "parent.json",
    userId: "33333333-3333-3333-3333-333333333333",
  },
  admin: {
    email: "admin@school.local",
    role: "SystemAdministrator",
    home: "/dashboard",
    storageFile: "admin.json",
  },
  authority: {
    email: "authority@ministry.local",
    role: "EducationAuthorityOfficer",
    home: "/authority",
    storageFile: "authority.json",
  },
  leader: {
    email: "leader@school.local",
    role: "SchoolLeader",
    home: "/leadership",
    storageFile: "leader.json",
    userId: "55555555-5555-5555-5555-555555555555",
  },
  federation: {
    email: "federation@ministry.local",
    role: "FederationAdmin",
    home: "/admin/federation",
    storageFile: "federation.json",
  },
  teacherB: {
    email: "teacher-b@schoolb.local",
    role: "Teacher",
    home: "/teacher",
    storageFile: "teacher-b.json",
    userId: "77777777-7777-7777-7777-777777777777",
  },
  studentB: {
    email: "student-b@schoolb.local",
    role: "Student",
    home: "/student",
    storageFile: "student-b.json",
    userId: "88888888-8888-8888-8888-888888888888",
  },
} as const;

export type UserKey = keyof typeof users;

export const ORG_A = "00000000-0000-4000-8000-000000000001";
export const CLASS_A = "00000000-0000-4000-8000-000000000111";
export const STUDENT_A = users.student.userId;

export const ALGEBRA_SKILLS = [
  "Read an equation",
  "Substitute a value",
  "Use inverse operations",
  "Isolate the variable",
] as const;

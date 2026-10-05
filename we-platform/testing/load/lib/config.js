/**
 * Base URLs and demo IDs for local docker-compose.
 * Override any value with env vars (see README.md).
 */

export const urls = {
  identity: __ENV.IDENTITY_API_URL || 'http://localhost:8081',
  organisation: __ENV.ORGANISATION_API_URL || 'http://localhost:8082',
  curriculum: __ENV.CURRICULUM_API_URL || 'http://localhost:8083',
  studentLearning: __ENV.STUDENT_LEARNING_API_URL || 'http://localhost:8084',
  assessment: __ENV.ASSESSMENT_API_URL || 'http://localhost:8085',
  evidence: __ENV.EVIDENCE_API_URL || 'http://localhost:8086',
  diagnostic: __ENV.DIAGNOSTIC_API_URL || 'http://localhost:8088',
  gaps: __ENV.GAP_API_URL || 'http://localhost:8089',
  mastery: __ENV.MASTERY_API_URL || 'http://localhost:8090',
  ei: __ENV.EI_API_URL || 'http://localhost:8091',
  intervention: __ENV.INTERVENTION_API_URL || 'http://localhost:8092',
  reporting: __ENV.REPORTING_API_URL || 'http://localhost:8096',
  nationalReporting: __ENV.NATIONAL_REPORTING_API_URL || 'http://localhost:8100',
};

/** Demo org / class / users from IdentityDataSeeder + scripts/seed-demo.sh */
export const demo = {
  organisationId: __ENV.ORG_ID || '00000000-0000-4000-8000-000000000001',
  yearLevelId: __ENV.YEAR_LEVEL_ID || '00000000-0000-4000-8000-000000000011',
  classId: __ENV.CLASS_ID || '00000000-0000-4000-8000-000000000111',
  studentUserId: __ENV.STUDENT_USER_ID || '22222222-2222-2222-2222-222222222222',
  teacherUserId: __ENV.TEACHER_USER_ID || '11111111-1111-1111-1111-111111111111',
  password: __ENV.DEMO_PASSWORD || 'Password123!',
  emails: {
    student: __ENV.STUDENT_EMAIL || 'student@school.local',
    teacher: __ENV.TEACHER_EMAIL || 'teacher@school.local',
    leader: __ENV.LEADER_EMAIL || 'leader@school.local',
    authority: __ENV.AUTHORITY_EMAIL || 'authority@ministry.local',
  },
  microSkills: [
    '00000000-0000-4000-8000-000000001001',
    '00000000-0000-4000-8000-000000001002',
    '00000000-0000-4000-8000-000000001003',
    '00000000-0000-4000-8000-000000001004',
  ],
};

export const jsonHeaders = { 'Content-Type': 'application/json' };

export function authHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}

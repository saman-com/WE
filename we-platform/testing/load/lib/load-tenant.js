/**
 * Dedicated school for k6 writes. Do not point this at the demo tenant:
 * approve-evidence creates one assessment per iteration.
 */
export const loadTenant = {
  organisationId: '00000000-0000-4000-8000-0000000000a1',
  yearLevelId: '00000000-0000-4000-8000-0000000000a2',
  classId: '00000000-0000-4000-8000-0000000000a3',
  studentUserId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
  teacherUserId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  password: 'Password123!',
  emails: {
    teacher: 'load-teacher@load.local',
    student: 'load-student@load.local',
  },
  microSkills: [
    '00000000-0000-4000-8000-00000000a011',
    '00000000-0000-4000-8000-00000000a012',
    '00000000-0000-4000-8000-00000000a013',
    '00000000-0000-4000-8000-00000000a014',
  ],
};

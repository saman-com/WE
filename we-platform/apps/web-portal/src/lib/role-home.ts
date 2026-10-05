const roleHomes: ReadonlyArray<readonly [string, string]> = [
  ["Student", "/student"],
  ["Teacher", "/teacher"],
  ["Parent", "/parent"],
  ["SchoolLeader", "/leadership"],
  ["EducationAuthorityOfficer", "/authority"],
  ["FederationAdmin", "/admin/federation"],
  ["SystemAdministrator", "/organisation"],
];

/** Where a signed-in user should land. Earlier roles win when several are present. */
export function roleHome(roles: readonly string[]): string {
  for (const [role, path] of roleHomes) {
    if (roles.includes(role)) {
      return path;
    }
  }
  return "/dashboard";
}

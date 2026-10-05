import { describe, expect, it } from "vitest";
import { roleHome } from "@/lib/role-home";

describe("roleHome", () => {
  it("sends each role to its home", () => {
    expect(roleHome(["Student"])).toBe("/student");
    expect(roleHome(["Teacher"])).toBe("/teacher");
    expect(roleHome(["Parent"])).toBe("/parent");
    expect(roleHome(["SchoolLeader"])).toBe("/leadership");
    expect(roleHome(["EducationAuthorityOfficer"])).toBe("/authority");
    expect(roleHome(["FederationAdmin"])).toBe("/admin/federation");
    expect(roleHome(["SystemAdministrator"])).toBe("/organisation");
  });

  it("prefers the learner and teacher homes when several roles are present", () => {
    expect(roleHome(["SystemAdministrator", "Teacher"])).toBe("/teacher");
    expect(roleHome(["Teacher", "Student"])).toBe("/student");
  });

  it("falls back to the dashboard for an unknown role", () => {
    expect(roleHome([])).toBe("/dashboard");
  });
});

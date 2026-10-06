import { readFileSync, readdirSync, statSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { beforeEach, describe, expect, it } from "vitest";
import enCatalog from "@/locales/en.json";
import arCatalog from "@/locales/ar.json";
import {
  DEFAULT_LOCALE,
  errorMessageKey,
  getDirection,
  isRtlLocale,
  LOCALE_STORAGE_KEY,
  readStoredLocale,
  resolveLocale,
  translate,
  writeStoredLocale,
  type Messages,
} from "@/i18n";

const en: Messages = {
  "login.title": "WE Platform Login",
  "login.greeting": "Hello, {name}",
  "errors.auth.invalid_credentials": "Login failed. Check your email and password.",
  "errors.validation.invalid_request": "Request failed.",
};

const ar: Messages = {
  "login.title": "تسجيل الدخول إلى منصة WE",
  "login.greeting": "مرحبًا، {name}",
  "errors.auth.invalid_credentials": "فشل تسجيل الدخول. تحقق من بريدك وكلمة المرور.",
  "errors.validation.invalid_request": "فشل الطلب.",
};

const enMessages = enCatalog as Messages;
const arMessages = arCatalog as Messages;

const EMPTY_STATE_KEYS = [
  "student.focus.quietDay",
  "student.focus.skillsEmpty",
  "student.focus.nextEmpty",
  "student.focus.notesEmpty",
  "student.focus.growthEmpty",
  "teacher.workspace.noClasses",
  "teacher.interventions.empty",
  "teacher.class.ei.masteryEmpty",
  "teacher.class.ei.gapsEmpty",
  "teacher.class.ei.trendsEmpty",
  "teacher.class.ei.attentionEmpty",
  "parent.home.masteryEmpty",
  "parent.home.resultsEmpty",
  "parent.home.feedbackEmpty",
  "parent.home.interventionsEmpty",
] as const;

const HARDCODED_LITERAL_ALLOWLIST = new Set([
  // Next.js metadata description is not locale-switched in layout.tsx today.
  "layout.description",
]);

describe("i18n translate", () => {
  it("returns translated string for the active locale", () => {
    expect(translate(ar, "login.title")).toBe("تسجيل الدخول إلى منصة WE");
  });

  it("interpolates parameters", () => {
    expect(translate(en, "login.greeting", { name: "Ava" })).toBe("Hello, Ava");
  });

  it("falls back to English when key missing in active locale", () => {
    const sparseAr: Messages = { "login.title": "تسجيل الدخول إلى منصة WE" };
    expect(translate(sparseAr, "login.greeting", { name: "Ava" }, en)).toBe(
      "Hello, Ava"
    );
  });

  it("uses English and Arabic plural categories for counts", () => {
    const key = "admin.aiAudit.shown";
    expect(translate(enMessages, key, { count: 1 }, undefined, "en")).toBe("Showing 1 entry.");
    expect(translate(enMessages, key, { count: 0 }, undefined, "en")).toBe("Showing 0 entries.");
    expect(translate(enMessages, key, { count: 4 }, undefined, "en")).toBe("Showing 4 entries.");
    expect(translate(arMessages, key, { count: 0 }, undefined, "ar")).toBe("لا تُعرض سجلات.");
    expect(translate(arMessages, key, { count: 1 }, undefined, "ar")).toBe("يُعرض سجل واحد.");
    expect(translate(arMessages, key, { count: 2 }, undefined, "ar")).toBe("يُعرض سجلان.");
    expect(translate(arMessages, key, { count: 5 }, undefined, "ar")).toBe("تُعرض 5 سجلات.");
    expect(translate(arMessages, key, { count: 15 }, undefined, "ar")).toBe("يُعرض 15 سجلاً.");
    expect(translate(arMessages, key, { count: 100 }, undefined, "ar")).toBe("يُعرض 100 سجل.");
  });
});

describe("i18n locale preference and RTL", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("defaults to English and LTR", () => {
    expect(resolveLocale(undefined)).toBe(DEFAULT_LOCALE);
    expect(getDirection(DEFAULT_LOCALE)).toBe("ltr");
    expect(isRtlLocale(DEFAULT_LOCALE)).toBe(false);
  });

  it("marks Arabic as RTL", () => {
    expect(getDirection("ar")).toBe("rtl");
    expect(isRtlLocale("ar")).toBe(true);
  });

  it("persists and restores the selected locale", () => {
    writeStoredLocale("ar");
    expect(localStorage.getItem(LOCALE_STORAGE_KEY)).toBe("ar");
    expect(readStoredLocale()).toBe("ar");
  });

  it("ignores unsupported stored locales", () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "zz");
    expect(readStoredLocale()).toBe(DEFAULT_LOCALE);
  });
});

describe("locale catalogues", () => {
  it("keeps English and Arabic key sets in lockstep", () => {
    const enKeys = Object.keys(enMessages).sort();
    const arKeys = Object.keys(arMessages).sort();
    expect(arKeys).toEqual(enKeys);
  });

  it("covers home and empty-state keys used by student, teacher, and parent surfaces", () => {
    for (const key of EMPTY_STATE_KEYS) {
      expect(enMessages[key], `missing en key ${key}`).toBeTruthy();
      expect(arMessages[key], `missing ar key ${key}`).toBeTruthy();
      expect(arMessages[key]).not.toBe(enMessages[key]);
    }
  });

  it("scans portal source for hard-coded English catalogue literals", () => {
    const rootDir = path.dirname(fileURLToPath(import.meta.url));
    const srcRoot = path.resolve(rootDir, "..");
    const scanRoots = [
      path.join(srcRoot, "app"),
      path.join(srcRoot, "components"),
    ];

    const sourceFiles: string[] = [];
    function walk(dir: string) {
      for (const entry of readdirSync(dir)) {
        const full = path.join(dir, entry);
        const stat = statSync(full);
        if (stat.isDirectory()) {
          walk(full);
          continue;
        }
        if (!/\.(tsx|ts)$/.test(entry)) {
          continue;
        }
        if (/\.(test|spec)\.(tsx|ts)$/.test(entry)) {
          continue;
        }
        sourceFiles.push(full);
      }
    }
    for (const root of scanRoots) {
      walk(root);
    }

    const sources = sourceFiles.map((file) => ({
      file,
      text: readFileSync(file, "utf8"),
    }));

    const hits: string[] = [];
    for (const [key, value] of Object.entries(enMessages)) {
      if (HARDCODED_LITERAL_ALLOWLIST.has(key)) {
        continue;
      }
      if (typeof value !== "string" || value.length < 18 || value.includes("{")) {
        continue;
      }
      const quoted = JSON.stringify(value);
      const single = `'${value.replace(/'/g, "\\'")}'`;
      for (const { file, text } of sources) {
        if (text.includes(quoted) || text.includes(single)) {
          hits.push(`${key} in ${path.relative(srcRoot, file)}`);
        }
      }
    }

    expect(hits).toEqual([]);
  });
});

describe("API error code translation", () => {
  it("maps API error codes onto catalogue keys", () => {
    expect(errorMessageKey("auth.invalid_credentials")).toBe(
      "errors.auth.invalid_credentials"
    );
    expect(errorMessageKey("auth.unauthorized")).toBe("errors.auth.unauthorized");
    expect(errorMessageKey("validation.invalid_request")).toBe(
      "errors.validation.invalid_request"
    );
    expect(errorMessageKey("unknown")).toBe("errors.unknown");
  });

  it("translates every errors.* catalogue entry for English and Arabic", () => {
    const errorKeys = Object.keys(enMessages)
      .filter((key) => key.startsWith("errors."))
      .sort();

    expect(errorKeys.length).toBeGreaterThanOrEqual(4);
    expect(errorKeys).toEqual([
      "errors.assessments.micro_skills_required",
      "errors.assessments.title_too_short",
      "errors.auth.account_inactive",
      "errors.auth.invalid_credentials",
      "errors.auth.unauthorized",
      "errors.federation.duplicate_school_code",
      "errors.federation.policy_value_too_short",
      "errors.intervention.text_contains_id",
      "errors.messages.invalid_teacher",
      "errors.organisation.duplicate_class_code",
      "errors.organisation.duplicate_year_name",
      "errors.organisation.year_name_too_short",
      "errors.unknown",
      "errors.users.cannot_deactivate_self",
      "errors.users.cannot_remove_own_admin",
      "errors.users.email_taken",
      "errors.users.invalid",
      "errors.users.password_invalid",
      "errors.validation.invalid_request",
    ]);

    for (const key of errorKeys) {
      expect(translate(enMessages, key)).toBe(enMessages[key]);
      expect(translate(arMessages, key)).toBe(arMessages[key]);
      expect(arMessages[key]).not.toBe(enMessages[key]);
    }
  });

  it("translates known API error codes for the active locale", () => {
    expect(translate(arMessages, "errors.auth.invalid_credentials")).toBe(
      "فشل تسجيل الدخول. تحقق من بريدك وكلمة المرور."
    );
    expect(translate(arMessages, "errors.auth.unauthorized")).toBe(
      "غير مصرح لك بتنفيذ هذا الإجراء."
    );
    expect(translate(enMessages, "errors.validation.invalid_request")).toBe(
      "Request failed."
    );
    expect(translate(enMessages, "errors.unknown")).toBe("Something went wrong.");
  });
});

import { beforeEach, describe, expect, it } from "vitest";
import {
  DEFAULT_LOCALE,
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

describe("API error code translation", () => {
  it("translates known API error codes for the active locale", () => {
    expect(translate(ar, "errors.auth.invalid_credentials")).toBe(
      "فشل تسجيل الدخول. تحقق من بريدك وكلمة المرور."
    );
    expect(translate(en, "errors.validation.invalid_request")).toBe(
      "Request failed."
    );
  });
});

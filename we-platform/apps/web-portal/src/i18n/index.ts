export type Locale = "en" | "ar";
export type Messages = Record<string, string>;
export type TranslateParams = Record<string, string | number>;

export const DEFAULT_LOCALE: Locale = "en";
export const SUPPORTED_LOCALES: readonly Locale[] = ["en", "ar"] as const;
export const RTL_LOCALES: readonly Locale[] = ["ar"] as const;
export const LOCALE_STORAGE_KEY = "we_locale";

export function isLocale(value: string | null | undefined): value is Locale {
  return value === "en" || value === "ar";
}

export function resolveLocale(value: string | null | undefined): Locale {
  return isLocale(value) ? value : DEFAULT_LOCALE;
}

export function isRtlLocale(locale: Locale): boolean {
  return RTL_LOCALES.includes(locale);
}

export function getDirection(locale: Locale): "ltr" | "rtl" {
  return isRtlLocale(locale) ? "rtl" : "ltr";
}

export function readStoredLocale(): Locale {
  if (typeof window === "undefined") {
    return DEFAULT_LOCALE;
  }
  return resolveLocale(window.localStorage.getItem(LOCALE_STORAGE_KEY));
}

export function writeStoredLocale(locale: Locale): void {
  if (typeof window === "undefined") {
    return;
  }
  window.localStorage.setItem(LOCALE_STORAGE_KEY, locale);
}

export function applyDocumentLocale(locale: Locale): void {
  if (typeof document === "undefined") {
    return;
  }
  document.documentElement.lang = locale;
  document.documentElement.dir = getDirection(locale);
}

export function pluralCategory(locale: Locale, count: number): string {
  const value = Math.abs(Math.trunc(count));
  if (locale === "ar") {
    if (value === 0) {
      return "zero";
    }
    if (value === 1) {
      return "one";
    }
    if (value === 2) {
      return "two";
    }
    const mod = value % 100;
    if (mod >= 3 && mod <= 10) {
      return "few";
    }
    if (mod >= 11 && mod <= 99) {
      return "many";
    }
    return "other";
  }
  return value === 1 ? "one" : "other";
}

function applyPlural(template: string, params: TranslateParams, locale: Locale): string | null {
  const match = template.match(/^\{(\w+),\s*plural,\s*([\s\S]+)\}$/);
  if (!match) {
    return null;
  }
  const raw = params[match[1]];
  const count = typeof raw === "number" ? raw : Number(raw);
  if (!Number.isFinite(count)) {
    return null;
  }
  const branches: Record<string, string> = {};
  for (const found of match[2].matchAll(/(\w+)\s*\{([^{}]*)\}/g)) {
    branches[found[1]] = found[2];
  }
  const text = branches[pluralCategory(locale, count)] ?? branches.other;
  if (text === undefined) {
    return null;
  }
  return text.replaceAll("#", String(count));
}

export function translate(
  messages: Messages,
  key: string,
  params?: TranslateParams,
  fallbackMessages?: Messages,
  locale: Locale = DEFAULT_LOCALE
): string {
  const template = messages[key] ?? fallbackMessages?.[key] ?? key;
  if (!params) {
    return template;
  }
  const plural = applyPlural(template, params, locale);
  if (plural !== null) {
    return plural;
  }
  return Object.entries(params).reduce(
    (result, [name, value]) =>
      result.replaceAll(`{${name}}`, String(value)),
    template
  );
}

export function errorMessageKey(code: string): string {
  return `errors.${code}`;
}

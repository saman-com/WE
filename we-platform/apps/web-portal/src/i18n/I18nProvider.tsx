"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import en from "@/locales/en.json";
import ar from "@/locales/ar.json";
import {
  applyDocumentLocale,
  DEFAULT_LOCALE,
  errorMessageKey,
  readStoredLocale,
  resolveLocale,
  translate,
  writeStoredLocale,
  type Locale,
  type Messages,
  type TranslateParams,
} from "@/i18n";

const catalogs: Record<Locale, Messages> = {
  en: en as Messages,
  ar: ar as Messages,
};

type I18nContextValue = {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  t: (key: string, params?: TranslateParams) => string;
  translateError: (code: string | null | undefined, fallbackKey?: string) => string;
  messages: Messages;
};

const I18nContext = createContext<I18nContextValue | null>(null);

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(DEFAULT_LOCALE);

  useEffect(() => {
    const stored = readStoredLocale();
    setLocaleState(stored);
    applyDocumentLocale(stored);
  }, []);

  const setLocale = useCallback((next: Locale) => {
    const resolved = resolveLocale(next);
    setLocaleState(resolved);
    writeStoredLocale(resolved);
    applyDocumentLocale(resolved);
  }, []);

  const value = useMemo<I18nContextValue>(() => {
    const messages = catalogs[locale];
    const fallback = catalogs.en;
    return {
      locale,
      setLocale,
      messages,
      t: (key, params) => translate(messages, key, params, fallback),
      translateError: (code, fallbackKey = "errors.unknown") => {
        if (!code) {
          return translate(messages, fallbackKey, undefined, fallback);
        }
        const key = errorMessageKey(code);
        return translate(messages, key, undefined, fallback);
      },
    };
  }, [locale, setLocale]);

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error("useI18n must be used within I18nProvider");
  }
  return context;
}

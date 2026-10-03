"use client";

import type { ReactNode } from "react";
import { LanguageSwitcher } from "@/components/language-switcher";
import { I18nProvider } from "@/i18n/I18nProvider";

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <I18nProvider>
      <div className="min-h-screen">
        <div className="flex justify-end p-3">
          <LanguageSwitcher />
        </div>
        {children}
      </div>
    </I18nProvider>
  );
}

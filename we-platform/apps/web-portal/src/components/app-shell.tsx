"use client";

import { createContext, useContext, useLayoutEffect, useState, type ReactNode } from "react";
import { LanguageSwitcher } from "@/components/language-switcher";
import { I18nProvider } from "@/i18n/I18nProvider";

const FrameChromeContext = createContext<((framed: boolean) => void) | null>(null);

export function useRegisterFrame() {
  const setFramed = useContext(FrameChromeContext);
  useLayoutEffect(() => {
    if (!setFramed) {
      return;
    }
    setFramed(true);
    return () => setFramed(false);
  }, [setFramed]);
}

export function AppShell({ children }: { children: ReactNode }) {
  return (
    <I18nProvider>
      <FrameChrome>{children}</FrameChrome>
    </I18nProvider>
  );
}

function FrameChrome({ children }: { children: ReactNode }) {
  const [framed, setFramed] = useState(false);
  return (
    <FrameChromeContext.Provider value={setFramed}>
      <div className="min-h-screen">
        {framed ? null : (
          <div className="flex justify-end p-3">
            <LanguageSwitcher />
          </div>
        )}
        {children}
      </div>
    </FrameChromeContext.Provider>
  );
}

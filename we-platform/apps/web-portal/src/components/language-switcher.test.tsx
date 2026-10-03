import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it } from "vitest";
import { LanguageSwitcher } from "@/components/language-switcher";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";

describe("LanguageSwitcher", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.lang = "en";
    document.documentElement.dir = "ltr";
  });

  it("switches UI language and persists the preference", async () => {
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <LanguageSwitcher />
      </I18nProvider>
    );

    expect(screen.getByRole("combobox", { name: /language/i })).toHaveValue("en");

    await user.selectOptions(screen.getByRole("combobox", { name: /language/i }), "ar");

    expect(localStorage.getItem(LOCALE_STORAGE_KEY)).toBe("ar");
    expect(screen.getByRole("combobox", { name: /language|اللغة/i })).toHaveValue("ar");
  });

  it("applies RTL layout attributes when Arabic is selected", async () => {
    const user = userEvent.setup();
    render(
      <I18nProvider>
        <LanguageSwitcher />
      </I18nProvider>
    );

    await user.selectOptions(screen.getByRole("combobox", { name: /language/i }), "ar");

    expect(document.documentElement.dir).toBe("rtl");
    expect(document.documentElement.lang).toBe("ar");
  });

  it("restores RTL from a persisted Arabic preference on mount", async () => {
    localStorage.setItem(LOCALE_STORAGE_KEY, "ar");
    render(
      <I18nProvider>
        <LanguageSwitcher />
      </I18nProvider>
    );

    await waitFor(() => {
      expect(document.documentElement.dir).toBe("rtl");
      expect(document.documentElement.lang).toBe("ar");
    });
  });
});

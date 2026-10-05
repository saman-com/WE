import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { AppShell } from "@/components/app-shell";
import { LanguageSwitcher } from "@/components/language-switcher";
import { LearningFrame } from "@/components/learning-frame";
import { I18nProvider } from "@/i18n/I18nProvider";
import { LOCALE_STORAGE_KEY } from "@/i18n";

describe("LanguageSwitcher", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.lang = "en";
    document.documentElement.dir = "ltr";
  });

  afterEach(cleanup);

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

describe("language picker placement", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.lang = "en";
    document.documentElement.dir = "ltr";
  });

  afterEach(cleanup);

  it("keeps one picker beside the name and sign out inside the frame", () => {
    render(
      <AppShell>
        <LearningFrame
          eyebrow="WE"
          title="Demo Student"
          onSignOut={() => undefined}
          signOutLabel="Sign out"
        >
          <p>Home</p>
        </LearningFrame>
      </AppShell>
    );

    const banner = screen.getByRole("banner");
    expect(within(banner).getByText("Demo Student")).toBeInTheDocument();
    expect(within(banner).getByRole("button", { name: "Sign out" })).toBeInTheDocument();
    expect(within(banner).getByRole("combobox", { name: /language/i })).toBeInTheDocument();
    expect(screen.getAllByRole("combobox")).toHaveLength(1);
  });

  it("keeps the picker on pages that do not use the frame", () => {
    render(
      <AppShell>
        <p>Login</p>
      </AppShell>
    );

    expect(screen.getByRole("combobox", { name: /language/i })).toBeInTheDocument();
    expect(screen.queryByRole("banner")).not.toBeInTheDocument();
  });

  it("keeps the frame picker working in Arabic", async () => {
    const user = userEvent.setup();
    render(
      <AppShell>
        <LearningFrame eyebrow="WE" title="Demo Student" signOutLabel="Sign out" onSignOut={() => undefined}>
          <p>Home</p>
        </LearningFrame>
      </AppShell>
    );

    await user.selectOptions(screen.getByRole("combobox", { name: /language/i }), "ar");

    expect(document.documentElement.dir).toBe("rtl");
    expect(within(screen.getByRole("banner")).getByRole("combobox")).toHaveValue("ar");
  });
});

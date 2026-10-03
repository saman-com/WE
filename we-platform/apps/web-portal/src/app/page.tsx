"use client";

import Link from "next/link";
import { useI18n } from "@/i18n/I18nProvider";

export default function Home() {
  const { t } = useI18n();

  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 p-8">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-semibold">{t("home.title")}</h1>
        <p className="text-black/60">{t("home.subtitle")}</p>
      </div>
      <Link
        href="/login"
        className="rounded bg-black text-white px-6 py-2 font-medium"
      >
        {t("home.signIn")}
      </Link>
    </div>
  );
}

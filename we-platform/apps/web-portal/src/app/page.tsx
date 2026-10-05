"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile } from "@/lib/auth";
import { roleHome } from "@/lib/role-home";
import { useI18n } from "@/i18n/I18nProvider";

export default function Home() {
  const { t } = useI18n();
  const router = useRouter();
  const [phase, setPhase] = useState<"checking" | "public">("checking");

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      setPhase("public");
      return;
    }

    let cancelled = false;
    fetchProfile(token)
      .then((profile) => {
        if (!cancelled) {
          router.replace(roleHome(profile.roles));
        }
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        if (!cancelled) {
          setPhase("public");
        }
      });

    return () => {
      cancelled = true;
    };
  }, [router]);

  if (phase === "checking") {
    return (
      <div className="min-h-screen flex items-center justify-center p-8">
        <p>{t("common.loading")}</p>
      </div>
    );
  }

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

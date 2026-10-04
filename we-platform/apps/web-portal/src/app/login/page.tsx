"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { login, fetchProfile } from "@/lib/auth";
import { ApiError } from "@/lib/api-error";
import { useI18n } from "@/i18n/I18nProvider";

export default function LoginPage() {
  const router = useRouter();
  const { t, translateError } = useI18n();
  const [email, setEmail] = useState("teacher@school.local");
  const [password, setPassword] = useState("Password123!");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const result = await login(email, password);
      localStorage.setItem("we_access_token", result.accessToken);
      const profile = await fetchProfile(result.accessToken);
      if (profile.roles.includes("Student")) {
        router.push("/student");
      } else if (profile.roles.includes("Teacher")) {
        router.push("/teacher");
      } else {
        router.push("/dashboard");
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setError(translateError(err.code, "login.errorFailed"));
      } else {
        setError(t("login.errorFailed"));
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-6">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md space-y-4 rounded-lg border border-black/10 p-8 shadow-sm"
      >
        <div>
          <h1 className="text-2xl font-semibold">{t("login.title")}</h1>
          <p className="text-sm text-black/60 mt-1">{t("login.subtitle")}</p>
        </div>

        <label className="block space-y-1">
          <span className="text-sm font-medium">{t("login.emailLabel")}</span>
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="w-full rounded border border-black/20 px-3 py-2"
            required
          />
        </label>

        <label className="block space-y-1">
          <span className="text-sm font-medium">{t("login.passwordLabel")}</span>
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className="w-full rounded border border-black/20 px-3 py-2"
            required
          />
        </label>

        {error ? <p className="text-sm text-red-600">{error}</p> : null}

        <button
          type="submit"
          disabled={loading}
          className="w-full rounded bg-black text-white py-2 font-medium disabled:opacity-60"
        >
          {loading ? t("login.submitLoading") : t("login.submit")}
        </button>
      </form>
    </div>
  );
}

"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, type UserProfile } from "@/lib/auth";
import {
  fetchNotifications,
  markNotificationRead,
  type Notification,
} from "@/lib/notifications";
import { useI18n } from "@/i18n/I18nProvider";

export default function NotificationsPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        setProfile(loaded);
        const list = await fetchNotifications(token);
        setNotifications(list.notifications);
      })
      .catch(() => {
        localStorage.removeItem("we_access_token");
        setError(t("common.sessionExpired"));
      })
      .finally(() => setLoading(false));
  }, [router, t]);

  async function handleMarkRead(notificationId: string) {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      return;
    }

    const updated = await markNotificationRead(token, notificationId);
    setNotifications((current) =>
      current.map((notification) =>
        notification.id === notificationId ? updated : notification
      )
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/login" className="underline">
            {t("common.backToLogin")}
          </Link>
        </div>
      </div>
    );
  }

  if (loading || !profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("notifications.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <h1 className="text-2xl font-semibold">{t("notifications.title")}</h1>
          <Link href="/dashboard" className="text-sm underline">
            {t("common.backToDashboard")}
          </Link>
        </div>

        {notifications.length === 0 ? (
          <p className="text-gray-600">{t("notifications.empty")}</p>
        ) : (
          <ul className="space-y-3">
            {notifications.map((notification) => (
              <li
                key={notification.id}
                className={`border rounded-lg p-4 space-y-2 ${
                  notification.isRead ? "bg-gray-50" : "bg-white border-blue-200"
                }`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-medium">{notification.title}</p>
                    <p className="text-sm text-gray-600">{notification.body}</p>
                    <p className="text-xs text-gray-400 mt-1">
                      {new Date(notification.createdAt).toLocaleString()}
                    </p>
                  </div>
                  {!notification.isRead && (
                    <button
                      type="button"
                      onClick={() => handleMarkRead(notification.id)}
                      className="text-sm underline shrink-0"
                    >
                      {t("notifications.markRead")}
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

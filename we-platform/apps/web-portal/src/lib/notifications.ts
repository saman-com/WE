const notificationApiUrl =
  process.env.NEXT_PUBLIC_NOTIFICATION_API_URL ?? "http://localhost:8095";

export type Notification = {
  id: string;
  type: string;
  title: string;
  body: string;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAt: string;
  readAt: string | null;
};

export type NotificationList = {
  notifications: Notification[];
};

async function notificationRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${notificationApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Notification request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchNotifications(token: string): Promise<NotificationList> {
  return notificationRequest<NotificationList>(token, "/api/v1/notifications");
}

export function markNotificationRead(
  token: string,
  notificationId: string
): Promise<Notification> {
  return notificationRequest<Notification>(
    token,
    `/api/v1/notifications/${notificationId}/read`,
    { method: "PATCH" }
  );
}

export function unreadNotificationCount(notifications: Notification[]): number {
  return notifications.filter((notification) => !notification.isRead).length;
}

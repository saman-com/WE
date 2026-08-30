const communicationApiUrl =
  process.env.NEXT_PUBLIC_COMMUNICATION_API_URL ?? "http://localhost:8094";

export type Message = {
  id: string;
  studentUserId: string;
  parentUserId: string;
  teacherUserId: string;
  senderUserId: string;
  senderRole: string;
  body: string;
  createdAt: string;
};

export type Conversation = {
  studentUserId: string;
  parentUserId: string;
  teacherUserId: string;
  messages: Message[];
};

export type MessageInboxThread = {
  studentUserId: string;
  parentUserId: string;
  teacherUserId: string;
  latestMessage: Message;
  messageCount: number;
};

export type MessageInbox = {
  threads: MessageInboxThread[];
};

async function messagingRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${communicationApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Messaging request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchMessageInbox(token: string): Promise<MessageInbox> {
  return messagingRequest<MessageInbox>(token, "/api/v1/messages/inbox");
}

export function fetchConversation(
  token: string,
  studentUserId: string,
  participantUserId: string
): Promise<Conversation> {
  const params = new URLSearchParams({
    studentUserId,
    participantUserId,
  });
  return messagingRequest<Conversation>(
    token,
    `/api/v1/messages/conversation?${params.toString()}`
  );
}

export function sendMessage(
  token: string,
  studentUserId: string,
  recipientUserId: string,
  body: string
): Promise<Message> {
  return messagingRequest<Message>(token, "/api/v1/messages", {
    method: "POST",
    body: JSON.stringify({ studentUserId, recipientUserId, body }),
  });
}

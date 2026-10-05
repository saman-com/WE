export type Paged<T> = {
  items: T[];
  hasMore: boolean;
  nextCursor: string | null;
};

export class UnexpectedPageError extends Error {
  constructor() {
    super("Unexpected paged response.");
    this.name = "UnexpectedPageError";
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** Reject list payloads that are not `{ items, hasMore, nextCursor }`. */
export function parsePaged<T>(value: unknown): Paged<T> {
  if (!isRecord(value) || !Array.isArray(value.items) || typeof value.hasMore !== "boolean") {
    throw new UnexpectedPageError();
  }

  const nextCursor = value.nextCursor;
  if (nextCursor !== null && typeof nextCursor !== "string") {
    throw new UnexpectedPageError();
  }

  return {
    items: value.items as T[],
    hasMore: value.hasMore,
    nextCursor,
  };
}

export async function fetchAllPages<T>(
  loadPage: (cursor: string | null) => Promise<Paged<T>>
): Promise<T[]> {
  const items: T[] = [];
  let cursor: string | null = null;
  let hasMore = true;
  while (hasMore) {
    const page = await loadPage(cursor);
    items.push(...page.items);
    hasMore = page.hasMore;
    cursor = page.nextCursor;
    if (hasMore && !cursor) {
      break;
    }
  }
  return items;
}

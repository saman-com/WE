export type Paged<T> = {
  items: T[];
  hasMore: boolean;
  nextCursor: string | null;
};

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

export function uniqueSuffix(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

export function uniqueTitle(prefix: string): string {
  return `${prefix} ${uniqueSuffix()}`;
}

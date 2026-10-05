export function isMockAiProvider(providerName: string | null | undefined): boolean {
  return !!providerName && providerName.trim().toLowerCase() === "mock";
}

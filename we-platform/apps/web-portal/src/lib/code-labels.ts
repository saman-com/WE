const knownCodes = new Set([
  "submitted",
  "Mastered",
  "Developing",
  "Proficient",
  "NotStarted",
  "Struggling",
  "Low",
  "Medium",
  "High",
  "Planned",
  "Active",
  "Completed",
  "Closed",
  "Draft",
  "Published",
]);

const storedCodePattern = new RegExp(`\\b(${[...knownCodes].join("|")})\\b`, "g");

/** Translate a stored status or level code. Unknown codes stay as stored. */
export function codeLabel(t: (key: string) => string, code: string): string {
  if (!knownCodes.has(code)) {
    return code;
  }
  return t(`codes.${code}`);
}

/** Translate mastery and status words that are embedded in stored sentences. */
export function localizeStoredText(text: string, t: (key: string) => string): string {
  return text.replace(storedCodePattern, (code) => codeLabel(t, code));
}

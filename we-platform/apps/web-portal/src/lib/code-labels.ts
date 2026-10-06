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
]);

/** Translate a stored status or level code. Unknown codes stay as stored. */
export function codeLabel(t: (key: string) => string, code: string): string {
  if (!knownCodes.has(code)) {
    return code;
  }
  return t(`codes.${code}`);
}

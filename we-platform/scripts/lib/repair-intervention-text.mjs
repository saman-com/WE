const guidPattern =
  /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
const hex8Pattern = /(?<![0-9a-f])[0-9a-f]{8}(?![0-9a-f])/gi;

export const unknownSkill = "Unknown skill";

export function repairInterventionText(text, names) {
  if (!text) {
    return text;
  }

  const byId = new Map();
  for (const [id, name] of names) {
    if (id && name) {
      byId.set(String(id).toLowerCase(), name);
    }
  }

  const withoutGuids = text.replace(guidPattern, (id) => byId.get(id.toLowerCase()) ?? unknownSkill);

  return withoutGuids.replace(hex8Pattern, (token) => {
    const prefix = token.toLowerCase();
    const matched = new Set();
    for (const [id, name] of byId) {
      const compact = id.replaceAll("-", "");
      if (id.startsWith(prefix) || compact.startsWith(prefix)) {
        matched.add(name);
      }
    }
    return matched.size === 1 ? [...matched][0] : unknownSkill;
  });
}

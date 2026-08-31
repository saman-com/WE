import type {
  CurriculumEffectivenessItem,
  InterventionEffectivenessItem,
} from "@/lib/effectiveness-analytics";
import { formatInterventionType, formatRate } from "@/lib/effectiveness-analytics";

type CurriculumEffectivenessTableProps = {
  items: CurriculumEffectivenessItem[];
};

export function CurriculumEffectivenessTable({ items }: CurriculumEffectivenessTableProps) {
  if (items.length === 0) {
    return (
      <p className="text-sm text-black/60">
        No curriculum mastery data in the analytics warehouse yet.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-left text-black/60 border-b border-black/10">
            <th className="py-2 pr-4">Subject</th>
            <th className="py-2 pr-4">Unit</th>
            <th className="py-2 pr-4">Mastery rate</th>
            <th className="py-2 pr-4">Mastered / Total</th>
            <th className="py-2">Status</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.unitId} className="border-b border-black/5">
              <td className="py-2 pr-4">{item.subjectName}</td>
              <td className="py-2 pr-4">{item.unitName}</td>
              <td className="py-2 pr-4">{formatRate(item.masteryRate)}</td>
              <td className="py-2 pr-4">
                {item.masteredMicroSkills} / {item.totalMicroSkills}
              </td>
              <td className="py-2">
                {item.isUnderperforming ? (
                  <span className="text-xs rounded px-2 py-0.5 bg-red-100 text-red-800">
                    Underperforming
                  </span>
                ) : (
                  <span className="text-xs rounded px-2 py-0.5 bg-green-100 text-green-800">
                    On track
                  </span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type InterventionEffectivenessTableProps = {
  items: InterventionEffectivenessItem[];
};

export function InterventionEffectivenessTable({ items }: InterventionEffectivenessTableProps) {
  if (items.length === 0) {
    return (
      <p className="text-sm text-black/60">
        No intervention effectiveness data in the analytics warehouse yet.
      </p>
    );
  }

  const maxRate = Math.max(...items.map((item) => item.successRate), 0.01);

  return (
    <div className="space-y-4">
      <div className="space-y-3">
        {items.map((item) => (
          <div key={item.interventionType} className="space-y-1">
            <div className="flex items-center justify-between text-sm">
              <span>{formatInterventionType(item.interventionType)}</span>
              <span className="text-black/70">
                {formatRate(item.successRate)} ({item.successfulCount}/{item.totalCount})
              </span>
            </div>
            <div className="h-2 rounded bg-black/5">
              <div
                className="h-2 rounded bg-emerald-600"
                style={{ width: `${(item.successRate / maxRate) * 100}%` }}
              />
            </div>
          </div>
        ))}
      </div>
      <p className="text-xs text-black/50">
        Success rate based on closed or completed interventions (EDW analytics).
      </p>
    </div>
  );
}

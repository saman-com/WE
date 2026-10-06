"use client";

import type { MasteryTrendPoint } from "@/lib/longitudinal-analytics";
import { formatPeriodKey } from "@/lib/longitudinal-analytics";
import { useI18n } from "@/i18n/I18nProvider";

type MasteryTrendChartProps = {
  points: MasteryTrendPoint[];
  title?: string;
};

export function MasteryTrendChart({ points, title }: MasteryTrendChartProps) {
  const { t } = useI18n();
  const heading = title ?? t("charts.mastery.defaultTitle");
  if (points.length === 0) {
    return (
      <div className="space-y-2">
        <h3 className="text-sm font-medium">{heading}</h3>
        <p className="text-sm text-black/60">{t("charts.mastery.empty")}</p>
      </div>
    );
  }

  const maxValue = Math.max(...points.map((p) => p.cumulativeMicroSkills), 1);
  const chartHeight = 120;

  return (
    <div className="space-y-3">
      <h3 className="text-sm font-medium">{heading}</h3>
      <div className="flex items-end gap-2 h-[140px] border-b border-black/10 pb-1">
        {points.map((point) => {
          const height = Math.max(4, (point.cumulativeMicroSkills / maxValue) * chartHeight);
          return (
            <div key={point.periodKey} className="flex-1 flex flex-col items-center gap-1 min-w-0">
              <span className="text-xs text-black/60">{point.cumulativeMicroSkills}</span>
              <div
                className="w-full max-w-10 bg-blue-600 rounded-t"
                style={{ height: `${height}px` }}
                title={t("charts.mastery.barTitle", {
                  period: formatPeriodKey(point.periodKey),
                  cumulative: point.cumulativeMicroSkills,
                  recorded: point.microSkillsRecorded,
                })}
              />
              <span className="text-[10px] text-black/50 truncate w-full text-center">
                {formatPeriodKey(point.periodKey)}
              </span>
            </div>
          );
        })}
      </div>
      <p className="text-xs text-black/50">
        {t("charts.mastery.footnote")}
      </p>
    </div>
  );
}

type GapHistoryTimelineProps = {
  events: Array<{
    learningGapId: string;
    eventType: string;
    occurredAt: string;
  }>;
  gapNames?: Record<string, string>;
};

export function GapHistoryTimeline({ events, gapNames = {} }: GapHistoryTimelineProps) {
  const { t } = useI18n();
  if (events.length === 0) {
    return (
      <div className="space-y-2">
        <h3 className="text-sm font-medium">{t("charts.gapHistory.title")}</h3>
        <p className="text-sm text-black/60">{t("charts.gapHistory.empty")}</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <h3 className="text-sm font-medium">{t("charts.gapHistory.title")}</h3>
      <ol className="relative border-l border-black/20 ml-2 space-y-4">
        {events.map((event, index) => (
          <li key={`${event.learningGapId}-${event.eventType}-${index}`} className="ml-4">
            <span
              className={`absolute -left-1.5 mt-1.5 h-3 w-3 rounded-full border-2 border-white ${
                event.eventType === "Closed" ? "bg-green-500" : "bg-amber-500"
              }`}
            />
            <p className="text-sm font-medium">
              {t("charts.gapHistory.event", { event: event.eventType.toLowerCase() })}
            </p>
            <p className="text-xs text-black/60">
              {gapNames[event.learningGapId] || t("assessments.review.unknownSkill")}
              {" · "}
              {new Date(event.occurredAt).toLocaleDateString()}
            </p>
          </li>
        ))}
      </ol>
    </div>
  );
}

type InterventionOutcomesTimelineProps = {
  outcomes: Array<{
    interventionId: string;
    learningGapId: string;
    status: string;
    createdAt: string;
  }>;
  gapNames?: Record<string, string>;
};

export function InterventionOutcomesTimeline({ outcomes, gapNames = {} }: InterventionOutcomesTimelineProps) {
  const { t } = useI18n();
  if (outcomes.length === 0) {
    return (
      <div className="space-y-2">
        <h3 className="text-sm font-medium">{t("charts.outcomes.title")}</h3>
        <p className="text-sm text-black/60">{t("charts.outcomes.empty")}</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <h3 className="text-sm font-medium">{t("charts.outcomes.title")}</h3>
      <ul className="space-y-2">
        {outcomes.map((outcome) => (
          <li
            key={outcome.interventionId}
            className="flex items-center justify-between rounded border border-black/5 px-3 py-2 text-sm"
          >
            <span>
              {gapNames[outcome.learningGapId] || t("assessments.review.unknownSkill")}
            </span>
            <span className="text-xs rounded px-2 py-0.5 bg-black/5">
              {outcome.status}
            </span>
            <span className="text-xs text-black/50">
              {new Date(outcome.createdAt).toLocaleDateString()}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

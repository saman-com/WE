"use client";

import { isMockAiProvider } from "@/lib/ai-provider";

type AiMockPreviewLabelProps = {
  providerName: string | null | undefined;
  label: string;
};

export function AiMockPreviewLabel({ providerName, label }: AiMockPreviewLabelProps) {
  if (!isMockAiProvider(providerName)) {
    return null;
  }

  return (
    <p className="text-xs font-medium text-amber-800" data-testid="ai-mock-preview-label">
      {label}
    </p>
  );
}

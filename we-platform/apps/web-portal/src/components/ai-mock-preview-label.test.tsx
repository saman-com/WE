import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { AiMockPreviewLabel } from "@/components/ai-mock-preview-label";
import { isMockAiProvider } from "@/lib/ai-provider";

afterEach(cleanup);

describe("isMockAiProvider", () => {
  it("detects Mock provider names case-insensitively", () => {
    expect(isMockAiProvider("Mock")).toBe(true);
    expect(isMockAiProvider("mock")).toBe(true);
    expect(isMockAiProvider("OpenAI")).toBe(false);
    expect(isMockAiProvider(undefined)).toBe(false);
    expect(isMockAiProvider("")).toBe(false);
  });
});

describe("AiMockPreviewLabel", () => {
  it("shows AI preview (sample text) for Mock drafts", () => {
    render(
      <AiMockPreviewLabel providerName="Mock" label="AI preview (sample text)" />
    );

    expect(screen.getByTestId("ai-mock-preview-label")).toHaveTextContent(
      "AI preview (sample text)"
    );
  });

  it("hides the label for non-Mock providers", () => {
    const { container } = render(
      <AiMockPreviewLabel providerName="OpenAI" label="AI preview (sample text)" />
    );

    expect(container).toBeEmptyDOMElement();
    expect(screen.queryByTestId("ai-mock-preview-label")).not.toBeInTheDocument();
  });
});

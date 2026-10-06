import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DataText } from "@/components/data-text";

describe("DataText", () => {
  it("isolates stored text from the surrounding writing direction", () => {
    render(<DataText>the letter alone on one side</DataText>);

    const text = screen.getByText("the letter alone on one side");
    expect(text.tagName).toBe("BDI");
    expect(text).toHaveAttribute("dir", "auto");
  });
});

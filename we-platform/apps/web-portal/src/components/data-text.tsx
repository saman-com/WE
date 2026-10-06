import type { ReactNode } from "react";

/** Isolates names, titles, and other stored text so it does not join the surrounding sentence direction. */
export function DataText({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <bdi dir="auto" className={className}>
      {children}
    </bdi>
  );
}

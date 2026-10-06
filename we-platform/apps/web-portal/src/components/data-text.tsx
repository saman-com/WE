import type { ReactNode } from "react";

/** Isolates names, titles, and other stored text so it does not join the surrounding sentence direction. Do not use inside option: a select option cannot contain bdi, so set dir="auto" on the option instead. */
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

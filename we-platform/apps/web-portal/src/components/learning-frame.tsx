"use client";

import type { ReactNode } from "react";
import Link from "next/link";

export type LearningTab = {
  id: string;
  label: string;
};

export function LearningFrame({
  eyebrow,
  title,
  onSignOut,
  signOutLabel,
  tabs,
  activeTab,
  onTabChange,
  children,
}: {
  eyebrow: string;
  title: string;
  onSignOut?: () => void;
  signOutLabel?: string;
  tabs?: LearningTab[];
  activeTab?: string;
  onTabChange?: (id: string) => void;
  children: ReactNode;
}) {
  return (
    <div className="we-learning">
      <div className="mx-auto w-full max-w-5xl px-4 pt-2 md:px-8">
        <header className="flex items-baseline justify-between gap-4">
          <p className="text-sm font-semibold tracking-wide">{eyebrow}</p>
          <div className="flex items-baseline gap-4">
            <p className="text-sm text-black/60">{title}</p>
            {onSignOut && signOutLabel ? (
              <button
                type="button"
                onClick={onSignOut}
                className="text-sm text-black/60 underline-offset-2 hover:underline"
              >
                {signOutLabel}
              </button>
            ) : null}
          </div>
        </header>

        {tabs && onTabChange ? (
          <div className="mt-4 hidden gap-2 md:flex">
            {tabs.map((tab) => (
              <TabButton
                key={tab.id}
                label={tab.label}
                active={tab.id === activeTab}
                onClick={() => onTabChange(tab.id)}
              />
            ))}
          </div>
        ) : null}

        <div className="mt-6 pb-24 md:pb-12">{children}</div>
      </div>

      {tabs && onTabChange ? (
        <nav className="fixed inset-x-0 bottom-0 border-t border-black/10 bg-[#f4f1ea] px-3 py-2 md:hidden">
          <div className="mx-auto flex max-w-5xl gap-2 overflow-x-auto">
            {tabs.map((tab) => (
              <TabButton
                key={tab.id}
                label={tab.label}
                active={tab.id === activeTab}
                onClick={() => onTabChange(tab.id)}
              />
            ))}
          </div>
        </nav>
      ) : null}
    </div>
  );
}

function TabButton({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        active
          ? "shrink-0 rounded-full bg-[#1c1917] px-3 py-1.5 text-sm text-white"
          : "shrink-0 rounded-full px-3 py-1.5 text-sm text-black/60"
      }
    >
      {label}
    </button>
  );
}

export function FocusCard({ children }: { children: ReactNode }) {
  return <div className="rounded-xl bg-black/[0.04] p-4">{children}</div>;
}

export function PrimaryLink({
  href,
  children,
}: {
  href: string;
  children: ReactNode;
}) {
  return (
    <Link
      href={href}
      className="inline-flex rounded-lg bg-[#1c1917] px-4 py-2.5 text-sm font-semibold text-white"
    >
      {children}
    </Link>
  );
}

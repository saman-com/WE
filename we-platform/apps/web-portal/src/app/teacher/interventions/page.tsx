"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { fetchProfile, personName, type DirectoryUser, type UserProfile } from "@/lib/auth";
import { learningLabel, loadGapLabels, loadLearningNames, loadPeople, replaceVisibleIds } from "@/lib/display-names";
import { roleHome } from "@/lib/role-home";
import {
  fetchStudentInterventions,
  type Intervention,
  type InterventionStatus,
} from "@/lib/interventions";
import { listClasses, listOrganisations } from "@/lib/organisation";
import { useI18n } from "@/i18n/I18nProvider";
import { codeLabel } from "@/lib/code-labels";

const statusOrder: InterventionStatus[] = ["Planned", "Active", "Completed", "Closed"];

function statusBadgeClass(status: InterventionStatus): string {
  switch (status) {
    case "Planned":
      return "bg-blue-100 text-blue-800";
    case "Active":
      return "bg-amber-100 text-amber-800";
    case "Completed":
      return "bg-green-100 text-green-800";
    case "Closed":
      return "bg-black/10 text-black/70";
  }
}

export default function TeacherInterventionsPage() {
  const router = useRouter();
  const { t } = useI18n();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [people, setPeople] = useState<DirectoryUser[]>([]);
  const [labels, setLabels] = useState<Record<string, string>>({});
  const [learningNames, setLearningNames] = useState<Record<string, string>>({});
  const [interventions, setInterventions] = useState<Intervention[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<InterventionStatus | "All">("All");

  useEffect(() => {
    const token = localStorage.getItem("we_access_token");
    if (!token) {
      router.replace("/login");
      return;
    }

    fetchProfile(token)
      .then(async (loaded) => {
        if (!loaded.roles.includes("Teacher")) {
          router.replace(roleHome(loaded.roles));
          return;
        }

        setProfile(loaded);
        const organisations = await listOrganisations(token);
        const names: Record<string, string> = {};
        for (const organisation of organisations) {
          Object.assign(names, await loadLearningNames(token, organisation.id));
        }
        setPeople(await loadPeople(token));
        setLearningNames(names);
        const classesByOrg = await Promise.all(
          organisations.map(async (organisation) => {
            const classes = await listClasses(token, organisation.id);
            return classes.flatMap((schoolClass) =>
              (schoolClass.studentUserIds ?? []).map((studentUserId) => ({
                studentUserId,
                className: schoolClass.name,
              }))
            );
          })
        );

        const studentIds = [...new Set(classesByOrg.flat().map((entry) => entry.studentUserId))];
        const allInterventions = await Promise.all(
          studentIds.map(async (studentUserId) => {
            try {
              const response = await fetchStudentInterventions(token, studentUserId);
              return response.interventions;
            } catch {
              return [];
            }
          })
        );

        setInterventions(allInterventions.flat().sort(
          (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
        ));
        setLabels(await loadGapLabels(token, studentIds, names));
      })
      .catch(() => {
        setError(t("teacher.interventions.loadError"));
      });
  }, [router, t]);

  const filtered =
    statusFilter === "All"
      ? interventions
      : interventions.filter((item) => item.status === statusFilter);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <div className="space-y-4 text-center">
          <p className="text-red-600">{error}</p>
          <Link href="/teacher" className="underline">
            {t("teacher.interventions.backToWorkspace")}
          </Link>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6">
        <p>{t("teacher.interventions.loading")}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">{t("teacher.workspace.nav.interventions")}</h1>
            <p className="text-sm text-black/60 mt-1">
              {t("teacher.interventions.subtitle")}
            </p>
          </div>
          <Link href="/teacher" className="text-sm underline">
            {t("dashboard.nav.teacherWorkspace")}
          </Link>
        </div>

        <div className="flex flex-wrap gap-2 text-sm">
          <button
            type="button"
            className={`rounded px-3 py-1 border ${statusFilter === "All" ? "border-black" : "border-black/20"}`}
            onClick={() => setStatusFilter("All")}
          >
            {t("teacher.interventions.filterAll")}
          </button>
          {statusOrder.map((status) => (
            <button
              key={status}
              type="button"
              className={`rounded px-3 py-1 border ${statusFilter === status ? "border-black" : "border-black/20"}`}
              onClick={() => setStatusFilter(status)}
            >
              {codeLabel(t, status)}
            </button>
          ))}
        </div>

        {filtered.length === 0 ? (
          <div className="rounded-lg border border-black/10 p-6">
            <p className="text-sm text-black/60">
              {t("teacher.interventions.empty")}
            </p>
          </div>
        ) : (
          <ul className="space-y-3">
            {filtered.map((item) => (
              <li key={item.id} className="rounded-lg border border-black/10 p-4 space-y-2">
                <div className="flex items-start justify-between gap-4">
                  <Link href={`/teacher/interventions/${item.id}`} className="font-medium underline">
                    {replaceVisibleIds(
                      item.plannedActions,
                      people,
                      { ...learningNames, ...labels },
                      t("organisation.manage.unknownPerson"),
                      t("assessments.review.unknownSkill")
                    )}
                  </Link>
                  <span className={`text-xs rounded px-2 py-0.5 ${statusBadgeClass(item.status)}`}>
                    {codeLabel(t, item.status)}
                  </span>
                </div>
                <p className="text-xs text-black/60">
                  {t("teacher.interventions.studentLabel")}{" "}
                  <Link href={`/students/${item.studentUserId}/profile`} className="underline">
                    {personName(people, item.studentUserId, t("organisation.manage.unknownPerson"))}
                  </Link>
                  {" · "}
                  {t("teacher.interventions.gapLabel", {
                    id: learningLabel(labels, item.learningGapId, t("assessments.review.unknownSkill")),
                  })}
                </p>
                <p className="text-sm text-black/70 line-clamp-2">
                  {item.notes
                    ? replaceVisibleIds(
                        item.notes,
                        people,
                        { ...learningNames, ...labels },
                        t("organisation.manage.unknownPerson"),
                        t("assessments.review.unknownSkill")
                      )
                    : t("teacher.interventions.noNotesYet")}
                </p>
                <p className="text-xs text-black/50">
                  {t("teacher.interventions.updated", {
                    date: new Date(item.updatedAt).toLocaleString(),
                  })}
                </p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

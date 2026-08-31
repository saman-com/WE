const configurationApiUrl =
  process.env.NEXT_PUBLIC_CONFIGURATION_API_URL ?? "http://localhost:8098";

export type AcademicTerm = {
  name: string;
  startDate: string;
  endDate: string;
};

export type Holiday = {
  name: string;
  date: string;
};

export type AcademicCalendar = {
  terms: AcademicTerm[];
  holidays: Holiday[];
};

export type GradingLevel = {
  label: string;
  minScore: number;
  maxScore: number;
};

export type GradingScale = {
  name: string;
  levels: GradingLevel[];
};

export type AssessmentModel = {
  name: string;
  category: string;
};

export type ReportingTemplate = {
  id: string;
  name: string;
  format: string;
};

export type LocaleSettings = {
  languageCode: string;
  regionCode: string;
  dateFormat: string;
  timeZone: string;
};

export type RegionalConfiguration = {
  tenantId: string;
  academicCalendar: AcademicCalendar;
  gradingScale: GradingScale;
  assessmentModels: AssessmentModel[];
  reportingTemplates: ReportingTemplate[];
  localeSettings: LocaleSettings;
  updatedAt: string;
  updatedByUserId: string;
};

export type UpdateRegionalConfigurationRequest = {
  academicCalendar: AcademicCalendar;
  gradingScale: GradingScale;
  assessmentModels: AssessmentModel[];
  reportingTemplates: ReportingTemplate[];
  localeSettings: LocaleSettings;
};

async function configurationRequest<T>(
  token: string,
  path: string,
  init?: RequestInit
): Promise<T> {
  const response = await fetch(`${configurationApiUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(`Configuration request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export function fetchRegionalConfiguration(token: string): Promise<RegionalConfiguration> {
  return configurationRequest<RegionalConfiguration>(token, "/api/v1/regional-configuration");
}

export function updateRegionalConfiguration(
  token: string,
  body: UpdateRegionalConfigurationRequest
): Promise<RegionalConfiguration> {
  return configurationRequest<RegionalConfiguration>(token, "/api/v1/regional-configuration", {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

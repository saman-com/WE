# National reporting / ministry API

Secured aggregate reporting for ministry and education-authority consumers.
OpenAPI is published at `GET /api/v1/docs`.

## Small-count suppression (`CountCell`)

Aggregate **count** fields (students, teachers, sample sizes, schools reporting,
intervention totals) are returned as a `CountCell` object, not a bare integer:

```json
{
  "value": 42,
  "suppressed": false
}
```

When a cell has fewer than `NationalReporting:MinimumGroupSize` students
(default **5**), the API withholds the number:

```json
{
  "value": null,
  "suppressed": true
}
```

**Breaking change for ministry consumers** (introduced with small-count
suppression): any client that previously treated `studentCount` /
`totalStudents` / `sampleSize` / `schoolsReporting` / `totalCount` /
`successfulCount` as a JSON number must read `count.value` and honour
`count.suppressed`. Do **not** treat a suppressed cell as `0` — that would
enable recovering withheld values by subtraction from roll-up totals.

Roll-up totals suppress the parent whenever **any** child cell is suppressed
(SP-001 Ch.22.10).

Affected endpoints:

- `/api/v1/national/enrollment`
- `/api/v1/national/mastery-benchmarks`
- `/api/v1/national/curriculum-coverage`
- `/api/v1/policy-dashboards/trends`
- `/api/v1/policy-dashboards/equity`
- `/api/v1/policy-dashboards/curriculum-effectiveness`
- `/api/v1/policy-dashboards/intervention-impact`

The OpenAPI document at `/api/v1/docs` describes the `CountCell` schema
(`value` nullable integer, `suppressed` boolean).

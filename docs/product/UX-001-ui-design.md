# UX-001 — UI Design

Document code: UX-001
Edition: 1.0
Status: Design accepted for edition 1. This document is the source of truth for the interface.

Build issues:

- [`issues/047-student-home-ui.md`](../../issues/047-student-home-ui.md) — student home
- [`issues/048-teacher-home-ui.md`](../../issues/048-teacher-home-ui.md) — teacher home

## 1. Purpose

This edition designs the first screens a high school student and their teacher open in WE.

The student home answers one question: **what should I focus on today?** Curriculum, assessment, approved feedback, and the teacher’s chosen practice show up as that answer. They are not a menu of modules.

The teacher home uses the same type, spacing, and layout. The teacher sees the decision. The student sees the next step.

## 2. Who it is for

Primary reader: a high school student, about 16, on their phone.

Secondary reader: their teacher, usually on a wider screen, opening the same visual system.

This edition does not design parent, school leader, or authority screens.

## 3. Decisions

| Decision | Choice |
| --- | --- |
| Order | Student screens first. Teacher uses the same system. |
| Device | Phone is the student layout. Desktop is the same blocks with a second column. |
| Tone | Quiet. No points, streaks, celebrations, or class rankings. |
| Skill words for students | Strong, Solid, Getting there, Not yet. |
| Skill words for teachers | Not started, Developing, Proficient, Mastered, plus severity. |
| Sample subject | Mathematics, unit Algebra. This is the subject the platform’s own school examples teach, including the “Algebra check” assessment. The repository has no named pilot school. |
| Growth | The student is compared with their own earlier work. |

## 4. How WE appears

The student never opens a module list. Each system has one job.

| WE system | Student sees | Stays off the student screen |
| --- | --- | --- |
| Curriculum and learning objectives | What success looks like on Today | Curriculum codes and document structure |
| Micro-skills and mastery | Skills with Strong, Solid, Getting there, Not yet | Weighted averages, confidence scores, evidence counts |
| Assessments | The next task, its class, and its due date | A mark as the first thing on the page |
| Evidence after teacher approval | One sentence from the teacher | Draft marks and anything not yet approved |
| Learning gap engine | The Next screen | The words gap, failure, and weakness. Severity and urgency. |
| Interventions | “Your teacher set this” and the practice | Intervention status and admin fields |
| AI assistant | Wording the teacher has approved | A chatbot that writes the assessed answer |
| Longitudinal record | Growth against the student’s own earlier work | Comparison with other students |

Students cannot call the mastery service. Until a student-facing read exists, skill levels on these screens are read from marks on approved evidence, out of 5. Solid starts at 3. Getting there is 2. Not yet is below 2. Strong is 5.

## 5. Visual system

Quiet and flat.

- Background: warm paper, `#f4f1ea`.
- Text: ink, `#1c1917`.
- Muted text: black at 60% opacity.
- One filled action: ink background, white label, corner radius 8px. One action per screen.
- Supporting surfaces: black at 4% fill, corner radius 12px. Borders are black at 10% opacity.
- Type: the existing Geist sans. Page headline 24px semibold. Body 14px.
- No gradients, shadows, emoji, or a different colour on every skill.
- The wordmark is `WE`. Beside it, the student’s first name, or the teacher’s name.

### Layout

- Content width: up to 1024px, padded 16px on a phone and 32px on a wide screen.
- Phone: one column. Screen tabs are fixed along the bottom.
- Wide screen: the same tabs sit under the header. The main block and the supporting block sit side by side, about 1.2 and 0.8.
- Tabs, in order: Today, Skills, Next, Notes, Growth.
- The active tab is an ink pill. The others are muted text.

## 6. Sample used in the screens

These words are the sample for reviews. A real student’s screens use their own assessments and approved feedback.

- Student: Aria, Year 11 Mathematics.
- Teacher: Ms Chen.
- Task: Algebra check, due Thursday 9 October, about 18 minutes, in class.
- Success: get the variable alone on one side, and check that the answer works.
- Shaky skill: Isolate the variable — Getting there.
- Teacher sentence: “You can substitute a number. The next step is getting the letter alone on one side.”
- Practice: 12 minutes, set by Ms Chen.
- Other skills, in dependency order: Read an equation (Strong), Substitute a value (Solid), Use inverse operations (Solid), Check the solution (Getting there), Solve equations with brackets (Not yet).

## 7. Student — Today

Question the screen answers: what should I focus on today?

**Phone, top to bottom**

1. `WE` and the first name.
2. Headline. If a task is due and a skill is still shaky: “Two things matter before Thursday.” If only a task is due: the task title is the thing to finish. If nothing is due and a teacher note exists: show that note. If the workspace is empty: “Nothing is waiting.”
3. Task block. Class and subject, due date, title, and what success looks like.
4. The shaky skill, one line, with its student word (Getting there).
5. The primary action: **Open the task**. It opens that assessment, not a generic list.
6. The teacher’s approved sentence.
7. Up to three coming due dates, weekday and title.

**Wide screen**

The task block is the left column. The shaky skill, the action, and the teacher sentence are the right column. The due dates stay full width underneath.

## 8. Student — Skills

Question the screen answers: what am I learning, and where am I?

- Subject and unit, then the learning objective in plain language.
- One row per micro-skill, in the order the skills depend on each other.
- Each row shows the skill name, the student word, and a single ink bar. The bar length is the mark out of 5.
- Tapping a row shows the teacher’s sentence for that skill.
- If the selected skill is Getting there or Not yet, the action opens Next.

Empty: “Skills show up here after your teacher approves a piece of work.”

On a wide screen the rows are the left column and the selected skill is the right column.

## 9. Student — Next

Question the screen answers: what is the one thing to practise?

- One practice only. A second, lower-priority opportunity may sit beside it on a wide screen, in muted text, and under a divider on a phone.
- Eyebrow: who set it, and how long it takes, when those facts exist.
- Headline: the shaky skill, in the teacher’s words.
- “Done when”: the success line for that skill.
- Primary action: **Open the task**, when a task is actually due. The screen does not invent a practice the product cannot open.

Empty: “When a skill is still shaky, it will be the one practice on this screen.”

## 10. Student — Notes

Question the screen answers: what did my teacher say, and what will I try?

- Only approved evidence.
- The headline is the sentence for the shakiest skill on the latest approved piece of work, not the strongest skill.
- Under it, a quiet prompt: “What will you try differently on the skill that is still shaky?” This edition shows the prompt. Saving the reflection onto the learning profile is a later issue.
- Beside the sentence, or under it on a phone, each skill on that piece of work with its student word.
- A text link, **All notes**, opens the full feedback list in the same frame.

Empty: “Notes show up after your teacher approves your work.”

## 11. Student — Growth

Question the screen answers: am I further on than I was?

- Each point is one piece of approved work, in date order.
- The label is the title, the date, the student word, and the mark out of 5.
- The caption states that Solid starts at 3 out of 5.
- A text link, **Full timeline**, opens the learning-profile timeline in the same frame.
- No classmate, no rank, and no invented weeks.

Empty: “Growth shows up as approved work, compared with your earlier work.”

## 12. Teacher home

Question the screen answers: which class do I open?

Same frame as the student: `WE`, the teacher’s name, sign out, paper, ink, one filled action.

**Phone, top to bottom**

1. Headline: “The classes you teach.”
2. The first class: school name, class name and code, number of students.
3. Primary action: **Open {class name}**.
4. Muted text links: Assessments, Curriculum, Interventions, Parent messages.
5. Any further classes as a simple list: name, school, student count. Each row opens that class.

**Wide screen**

The first class is the left column. The action and the muted links are the right column. Further classes stay full width underneath.

Empty: “No classes assigned to you.”

The class page itself, where the teacher sees who is not ready and sets a practice, is the next design edition. It is not specified here.

## 13. Shared behaviour

- A student who signs in lands on Today.
- A teacher who signs in lands on the teacher home.
- Sign out clears the session and returns to login.
- Language follows the existing English and Arabic switch. Student skill words are translated. The system words Developing and Proficient are not shown to students.
- Loading and session-expired states use the same paper background and a single link back.

## 14. Not in this edition

Design these only after this edition is accepted:

- The task page: write an answer and hand it in.
- The teacher class page: who needs a decision, severity visible, practice set by the teacher.
- Parent, leadership, and authority screens.
- Calendar, messages, portfolio, and achievements.
- Saving a student’s reflection onto the learning profile.
- A student-facing mastery read, if approved marks are not enough.

## 15. Acceptance for a later build issue

A build issue for this edition is done when:

- Today, Skills, Next, Notes, and Growth match sections 7 to 11 on a phone and on a wide screen.
- The teacher home matches section 12.
- Student copy uses Strong, Solid, Getting there, and Not yet.
- The student does not see gap, failure, weakness, severity, or another student’s results.
- Skill rows and growth points come from that student’s approved evidence.
- The primary action on Today opens the due assessment.
- Empty states use the sentences in this document.

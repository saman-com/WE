## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the Notification Service end-to-end per SP-001 Ch.17. Platform sends in-app and email notifications for key events: assessment published, feedback available, intervention created, new message received. Users can view notification history and mark as read.

Includes: Notification Service, schema, event subscribers (assessment published, evidence approved, message sent), in-app notification UI, email adapter (or mock for local dev), and tests.

## Acceptance criteria

- [ ] In-app notifications created for: assessment published, feedback available, new message
- [ ] User can view notification list and mark as read
- [ ] Email notifications sent for configured events (mock adapter acceptable locally)
- [ ] Notifications scoped to recipient only
- [ ] Event-driven: notifications triggered by domain events, not polling
- [ ] Tests verify notification creation on key events

## Blocked by

- `issues/003-identity-auth.md`

## User stories addressed

- SP-001 Ch.17 (Communication — notifications)
- SP-001 Ch.2 §2.8 (Platform Events)

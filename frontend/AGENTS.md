<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

## UI / UX Guidelines

### Design direction

- Build a modern, clean SaaS-style interface.
- Prefer a minimal and professional appearance.
- Avoid overly colorful, flashy, or decorative UI.
- Use consistent spacing, typography, border radius, and shadows.
- Prioritize usability and visual hierarchy.

### Layout

- Use responsive layouts.
- Prefer max-width containers instead of full-width content everywhere.
- Keep consistent horizontal and vertical spacing.
- Avoid unnecessary nested cards.
- Use whitespace to separate sections.

### Typography

- Use clear typography hierarchy:
  - Page title
  - Section title
  - Body
  - Secondary text
- Do not use too many font sizes.
- Avoid excessive bold text.

### Components

- Reuse existing components before creating new ones.
- Prefer shadcn/ui components when available.
- Keep buttons, inputs, cards, dialogs, tables, etc. visually consistent.
- Do not create duplicated UI patterns.

### Colors

- Follow the existing theme.
- Do not introduce arbitrary colors.
- Prefer semantic colors for success, warning, error, and information.

### Responsive

- Desktop and mobile must both be usable.
- Avoid fixed widths unless necessary.
- Tables should have appropriate overflow behavior on small screens.

### UX

- Loading states should be visible.
- Empty states should be informative.
- Error states should clearly explain what happened.
- Forms should provide clear validation feedback.
- Destructive actions should require confirmation.

### Code

- Do not modify business logic unless explicitly requested.
- Do not change API contracts.
- Do not remove existing functionality just to improve UI.
- Before creating a component, check whether an existing component can be reused.

## Design

Follow the design system defined in DESIGN.md.

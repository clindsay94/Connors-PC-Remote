# Connor's PC Remote – GitHub & Google Jules Playbook

Use this handbook whenever you (or Google Jules) create/triage GitHub issues for this repository.

## 1. Preparation Checklist

1. Read `ARCHITECTURE_AND_DELIVERY_PLAN.md` to understand current roadmap phase.
2. Skim the relevant `AGENT_INSTRUCTIONS.md` (root + project-level) so the issue aligns with established guardrails.
3. Confirm your local tooling:
   - .NET SDK 10.0.100
   - Windows 11/10 22621+
   - Visual Studio 2022 17.10+ with WinUI workload (optional but helpful)

## 2. Issue Template (copy/paste)

```text
### Summary
<One sentence describing the change>

### Context
- Roadmap phase / section: <e.g., Phase 1 – Task 2>
- Related files/projects: <Core/Service/UI/Tests>

### Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

### Validation
- Tests to run: `dotnet test ...`
- Manual steps (if any): <list>

### Notes for Google Jules
- Suggested commands / scripts
- External dependencies (certificates, services, etc.)
```

## 3. Creating Issues Manually

1. Navigate to **Issues → New Issue** in GitHub.
2. Paste the template above and fill every section.
3. Assign labels:
   - `phase:1|2|3` (match roadmap phase)
   - `area:core`, `area:service`, `area:ui`, or `area:tests`
   - `type:feature`, `type:bug`, `type:doc`, etc.
4. Link to any relevant files/lines by referencing commits or using GitHub’s file picker.
5. Mention blockers (e.g., need code signing cert) so Jules/agents can resolve dependencies.

## 4. Driving Work with Google Jules

1. **Connect Repo** – Ensure Google Jules has access to the GitHub repository (already linked per your setup).
2. **Create Task** – In Jules, create a task referencing the GitHub issue URL. Include:
   - Issue summary & acceptance criteria
   - Required commands (e.g., `dotnet test CPCRemote.Tests/CPCRemote.Tests.csproj`)
   - Paths to modify (e.g., `CPCRemote.Service/Worker.cs`)
3. **Provide Context** – Attach snippets from the architecture plan or agent instructions if the task is nuanced.
4. **Review Output** – Once Jules proposes changes:
   - Verify the diff locally
   - Run the indicated tests
   - Update the issue with findings (pass/fail, follow-ups)
5. **Close Loop** – When the PR merges, mark the Jules task complete and close the GitHub issue with a summary referencing commit/PR numbers.

## 5. Prioritization Strategy

1. **Follow the Roadmap** – Work Phase 1 items before Phase 2 unless an urgent bug/security fix arises.
2. **Security > Features** – Authentication, HTTPS, and configuration safety trump UX polish.
3. **Docs & Tests Are Mandatory** – Every issue must specify documentation and testing updates in Acceptance Criteria.

## 6. Personal Workflow Tips (While Learning)

- Start with documentation or test-only issues to build context before editing service/UI code.
- Pair Jules with manual review: let Jules draft code, then inspect and learn from the diff.
- Keep a running log (e.g., `notes.md`) of lessons learned or follow-up questions.
- Don’t hesitate to open “Question” issues when architecture or tooling is unclear; the conversation itself becomes documentation.

With this playbook, each issue—whether handled by you or Google Jules—feeds directly into the repository’s structured roadmap, minimizing rework and keeping the project on a steady path toward the “best version” goal.

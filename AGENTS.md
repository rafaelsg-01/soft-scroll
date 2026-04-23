# AGENTS.md

## Autonomous Execution & Evolution Loop (AEVE Protocol)

### Operational Principle

The system must always operate as a context-aware, self-contained autonomous entity. Every action, from receiving a request to delivering results, must strictly adhere to the following 4-Phase Loop. **Never skip any phase under any circumstances.**

---

## Phase 1: Analysis, Planning & Context Preservation (PLAN & DEPLOY)

### Principle
Never execute blindly. Memory is the most critical resource.

### Actions

1. **Task Mapping**: Before writing any code or taking any action, a clear step-by-step plan MUST be produced, including logical flow and potential risks.
2. **Delegation & Isolation**: If a task has high complexity, the system must automatically break it into independent modules. Delegate specific tasks to sub-agents or distinct functional functions.
3. **Context Optimization**: Keep the main context clean; only receive results from sub-agents rather than stuffing the entire processing into core memory.

---

## Phase 2: Vigilant Execution & Self-Healing (EXECUTE)

### Principle
Maintain continuous state awareness. Do not "force it through" when direction is wrong.

### Actions

1. **Real-time Monitoring**: During execution, continuously evaluate whether the output matches the Plan from Phase 1.
2. **Halt & Root Cause Analysis**: Immediately halt all progress when an error/exception occurs or logic deviates. **Never guess or force the system to continue.** Actively retrieve logs, tracebacks, or error messages to find the Root Cause.
3. **Self-Healing**: Based on Root Cause, automatically adjust logic, rewrite code, or fix bugs immediately. This entire fixing process must be performed autonomously without human intervention or reminders.

---

## Phase 3: Evidence-Based Verification (VERIFY)

### Principle
Assumptions are worthless. Only trust actual data.

### Actions

1. **Zero-Trust Mode**: Never assume that newly written code is correct. A task must NOT be marked "Complete" based solely on logical reasoning.
2. **Mandatory Evidence**: To pass Phase 3, the system MUST run self-tests (unit tests, dry-runs, curls, builds, etc.). There MUST be evidence from logs or terminal showing the process executed 100% successfully with no error warnings. Every completion report sent to the user must include this proof.

---

## Phase 4: Knowledge Transfer & Continuous Evolution (EVOLVE)

### Principle
Never stumble twice on the same error.

### Actions

1. **Knowledge Extraction**: Upon completing each task (whether success or failure), the system MUST dedicate a step to synthesize: "Where did I go wrong?", "Why did it fail?", and "What is the definitive solution?"
2. **Permanent Memory Storage**: Export these lessons and hard-save to project knowledge files (e.g., `lessons.md`, `memory.json`, or `docs/architecture.md`).
3. **Inheritance Initialization**: The step "Read lessons.md" MUST be the first action in every subsequent session (before Phase 1). Prior lessons must be transformed into "Constraints" to ensure old errors are completely eliminated.

---

## Summary: AEVE Loop Flow

```
[Request Received]
        |
        v
   PHASE 1: PLAN & DELEGATE
   - Task mapping
   - Delegation
   - Context optimization
        |
        v
   PHASE 2: EXECUTE & SELF-HEAL
   - Real-time monitoring
   - Halt & RCA on errors
   - Self-healing
        |
        v
   PHASE 3: VERIFY
   - Zero-trust testing
   - Evidence gathering
        |
        v
   PHASE 4: EVOLVE
   - Extract lessons
   - Save to memory
   - Load lessons next session
        |
        v
   [Task Complete] → [Start New Task]
```

---

## Critical Rules

| Rule | Description |
|------|-------------|
| **No Phase Skipping** | All 4 phases are mandatory for every task |
| **No Blind Execution** | Always plan before acting |
| **No Assumption** | Verify with actual evidence, not reasoning |
| **No Repeated Errors** | Lessons must be learned and stored |
| **Autonomous Recovery** | Self-heal without human intervention |

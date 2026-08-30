#!/usr/bin/env node
// PreToolUse hook (Bash matcher) - blocks raw scripting one-liners used for data
// processing (python3 -c, python -c, node -e/--eval) instead of just reminding.
//
// Why this exists: context-mode's own PreToolUse hook already injects an advisory
// system-reminder recommending ctx_execute/ctx_batch_execute for exactly this class of
// command, but a reminder is advisory only - a session used raw `python3 -c "..."` for
// path-length arithmetic, parsing an oversized Jira changelog JSON, and JSON validation
// instead, despite the reminder firing every time. This hook makes the same guidance an
// actual permission denial instead of a suggestion that can be ignored under time
// pressure.
//
// Multiple PreToolUse hooks on the same matcher (this one + context-mode's own) all run;
// any hook returning permissionDecision "deny" blocks the call regardless of what other
// hooks return, so this composes safely alongside context-mode's hook without needing to
// touch its (vendored, not-ours-to-edit) files.

let raw = "";
process.stdin.on("data", (chunk) => {
  raw += chunk;
});

process.stdin.on("end", () => {
  let command = "";
  try {
    const input = JSON.parse(raw);
    command = (input.tool_input && input.tool_input.command) || "";
  } catch {
    process.exit(0);
  }

  // Matches `python3 -c`, `python -c`, `node -e`, `node --eval` as a standalone
  // invocation (start of command or after a shell separator), not as a substring of an
  // unrelated word.
  const pattern = /(^\s*|[;&]\s*|\|\|\s*|&&\s*|\|\s*)(python(?:3(?:\.\d+)?)?|node)\s+(-c\b|-e\b|--eval\b)/;
  if (pattern.test(command)) {
    console.log(
      JSON.stringify({
        hookSpecificOutput: {
          hookEventName: "PreToolUse",
          permissionDecision: "deny",
          permissionDecisionReason:
            "Raw python/node one-liners (-c/-e/--eval) are blocked for data " +
            "processing, parsing, or computation in this repo - use context-mode's " +
            "ctx_execute or ctx_batch_execute instead, which keeps raw tool output " +
            "out of the conversation (this is exactly the case context-mode exists " +
            "for). If this genuinely isn't data processing (e.g. checking an " +
            "installed version), rephrase the command without -c/-e/--eval.",
        },
      }),
    );
    return;
  }

  process.exit(0);
});

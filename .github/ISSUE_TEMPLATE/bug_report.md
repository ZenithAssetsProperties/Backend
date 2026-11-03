---
name: Bug Report
about: Create a report to help reproduce and fix a backend defect
title: '[BUG] '
labels: ['bug', 'triage']
assignees: ''
---

**Describe the Bug**
A clear and concise description of what the bug is.

**Endpoint & HTTP Method**
- Endpoint: e.g. `POST /api/v1/reservations`
- Status Code Received: e.g. `500 Internal Server Error`

**Expected Behavior**
A clear and concise description of what you expected to happen.

**Reproduction Steps**
1. Request payload or curl command
2. Headers sent (Authorization, Content-Type)
3. Observed error response (ProblemDetails payload)

**Correlation ID / Error Log Details**
```json
{
  "traceId": "...",
  "detail": "..."
}
```

**Environment**
- [ ] Local Docker
- [ ] Staging
- [ ] Production


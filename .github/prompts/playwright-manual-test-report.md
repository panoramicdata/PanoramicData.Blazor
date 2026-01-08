---
mode: agent
description: "Manually test a site and create a report"
tools: 
  - changes
  - search/codebase
  - edit/editFiles
  - fetch
  - openSimpleBrowser
  - problems
  - runCommands
  - runTests
  - search
  - search/searchResults
  - runCommands/terminalLastCommand
  - runCommands/terminalSelection
  - testFailure
  - microsoft/playwright-mcp/*
model: 'Claude Sonnet 4.5'
---

# Manual Testing Instructions

1.  **Exploration:** Use the Playwright MCP Server to navigate to the website, take a page snapshot, and analyze the key functionalities. Then, manually test the scenario provided by the user. If no scenario is provided, ask the user to provide one. Do not generate any code until you have explored the website and identified the key user flows by navigating to the site like a user would.
2.  **Interaction:** Navigate to the URL provided by the user and perform the described interactions. If no URL is provided, ask the user to provide one.
3.  **Verification:** Observe and verify the expected behavior, focusing on accessibility, UI structure, and user experience.
4.  **Reporting:** Report back in clear, natural language:
    - What steps you performed (navigation, interactions, assertions).
    - What you observed (outcomes, UI changes, accessibility results).
    - Any issues, unexpected behaviors, or accessibility concerns found.
5.  **Documentation:** Reference URLs, element roles, and relevant details to support your findings.

### Example Report Format
- **Scenario:** [Brief description]
- **Steps Taken:** [List of actions performed]
- **Outcome:** [What happened, including any assertions or accessibility checks]
- **Issues Found:** [List any problems or unexpected results]

---

**Post-Testing Requirements:**
* Generate a `.md` file with the report in the `manual-tests` directory and include any relevant screenshots or snapshots.
* Take screenshots or snapshots of the page if necessary to illustrate issues or confirm expected behavior.
* **Close the browser** after completing the manual test.
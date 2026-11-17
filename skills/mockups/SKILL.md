---
name: mockups
description: Interactive mockup page that shows the current UI and three design options side by side in one HTML file, before any app code changes. Use when the user asks for mocks or mockups, design options or alternatives, or a redesign of a screen or component.
---

# Mockups

A **mockup page** is one self-contained HTML file. It shows the current design and three options as working copies of the app, side by side. The user picks by using the copies, not by reading about them. The app code stays as it is until the user picks.

Project instructions win over this skill. When they give a demo folder, a design doc, or extra page rules, use them.

## Steps

Do the steps in order. Each step ends on its **done** line.

### 1. Load the design source

Find the values that the app uses. Look in this order and stop at the first source that covers colours, type, radii, spacing and motion:

1. A design doc that the project instructions name (for example `DESIGN.md`).
2. Token files: CSS custom properties on `:root`, a Tailwind `@theme` block or config, a theme file.
3. The global stylesheet and the CSS of the component that the task is about.

When a value has no source, use the nearest value that exists. When the project has no design system, take the values from the global CSS and say so in the report.

**Done:** you can write the page's `:root` block for light and dark with project values only, and you know which font files to copy.

### 2. Read the current UI

Find the component and every state it shows. Search for its user-visible texts and its state names (enums, `@switch` cases, status unions). Read only those files.

**Done:** a list of states, each with `file:line` and its exact text. The Current column copies this markup and CSS.

### 3. Write the problems

Write 3 to 6 problems with the current design. Each problem is a fact from step 2 or from the user's words, and names a thing that the user sees or does.

**Done:** every problem is checkable in the Current column of the page.

### 4. Choose the options

Make three options, unless the user asks for another number. The options differ in **structure**: layout, hierarchy, where the state lives (a full view, a block in the page, a chip, a notice), or how the content splits into views. Spacing or colour alone does not make a new option. Keep each option inside the design system, unless the user asks for a new visual direction.

**Done:** every option fixes every problem, and each option has a name ("1 · Tray") and one paragraph that says what it does differently.

### 5. Find the axes

An **axis** is a control in the settings bar that changes every copy at once.

- **Theme** (System, Light, Dark): always. It changes the whole page.
- **Width** (Phone, Desktop): when the layout changes with the width.
- **App settings** that the UI depends on (for example a content width or a role).
- **Time axis**: when a state appears, changes, or goes away (loading, errors, offline, progress, async results, notices). Add a scenario setting ("Opened while offline", "Lost while browsing"), a switch for the simulated backend ("Server: Off, On"), and a Replay button. Use short demo timings, and give the real timings in the intro.

Each copy runs its own state machine, so a click in one column changes only that column.

**Done:** the page can reach every state from step 2 and every new state of the options.

### 6. Build the page

Start from [`template.html`](template.html). It has the theme switch, the settings bar, the columns, the app frame and the state machine.

- **Place:** `<git-ignored tmp folder>/mockups/<name>/index.html`. Use the demo folder that the project instructions name. To find an ignored folder, read `.gitignore`. When no folder is ignored, ask.
- **One file:** inline CSS and JS, no build, no CDN, no network calls. Copy the font files into the same folder.
- **Order:** title, one paragraph, the problems, the settings, then the columns: "Current" first, then "1 · Name", "2 · Name", "3 · Name".
- **Copies:** real texts, real controls, every state. An action that would call the API, save or delete runs through the state machine, or shows "Demo only".
- **Frame:** each copy sits in a frame with `container-type: inline-size`. Write the copy's breakpoints as container queries, so the copy follows the frame width.
- **Stand-ins:** user content (photos, avatars, charts) is neutral blocks. Say in the intro that they are stand-ins.
- **Motion:** use the project's curves and durations. Give every animation a reduced-motion fallback that keeps only opacity.

**Done:** every step 5 axis and every state works in the file.

### 7. Check

Extract the `<script>` body to a temp file and run `node --check` on it. Then open the page: `start` on Windows, `open` on macOS, `xdg-open` on Linux. The user checks the page in the browser, so the check ends when the page opens.

**Done:** the syntax check passes and the page is open.

### 8. Report and wait

Report:

- the absolute path of the page;
- the problems;
- one line per option;
- the axes and what each one shows;
- that the blocks are stand-ins;
- that the app code and the design doc did not change.

Ask the user to pick an option or name a change. Then stop.

After the pick, build the option in the app with the project's normal UI rules: plan approval, visual checks, and the design doc update.

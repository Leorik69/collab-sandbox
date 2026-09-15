# 004 — SL Stage 1: Style variants (refs only, no code)

Spec: `drop/004-island-clock-weather-styles.md`  
Context: existing NotifyIsland (`island/`) — capsule pill, FontIcon, badge, Dot pulse, Acrylic/Mica/Solid, LocalSettings.

**Rule:** code in `island/` only after CC approval of selected variants.

---

## 1. Clock & dial (pick 1 default + keep others switchable)

| ID | Variant | Visual / WinUI notes | Fit in island |
|----|---------|----------------------|---------------|
| **C1** | **Digital Modern** (recommended default) | `Segoe UI Variable` SemiBold / Display; `HH:MM` 24h; colon blink via `DispatcherTimer` opacity | Compact pill; matches current 32–40px height |
| **C2** | **Digital Minimal** | Light/UltraLight, no seconds, tighter tracking; muted foreground `#A0A0A8` | Best for status-bar calm |
| **C3** | **Seconds + minute arc** | `HH:MM:SS` (SS smaller) + thin `ProgressBar`/`Arc` for minute progress | Needs ~+24px width or expand-on-hover |
| **C4** | **Analog Minimal dial** | SVG/Canvas: 12 ticks, no digits; hour/min `RotateTransform` smooth | Optional expand mode; too dense for 32px collapsed |

**Refs:** Windows 11 Clock flyout (digital), WinUI Gallery ProgressBar / Composition animations, Segoe UI Variable samples.

**SL pick for CC:** default **C1**; ship **C2** + **C3** as settings enums; **C4** as expand-only / experimental.

---

## 2. Weather widget

| ID | Format | Content |
|----|--------|---------|
| **W1** | **Compact Badge** (collapsed) | Fluent weather glyph + `°C` (toggle `°F`) |
| **W2** | **Expanded Widget** | Glyph + temp + short desc + Feels-like / Min–Max |

**States (glyphs):** Clear/Sun · Cloud/Overcast · Rain/Drizzle · Snow · Thunderstorm · Fog/Mist  
Map to `FontIcon` Segoe Fluent Icons (e.g. WeatherSunny, Cloud, RainShowersDay, …) or SVG set under `Assets/Weather/`.

**Refs:** Windows Weather widgets (taskbar / Widgets board), Fluent Weather iconography.

**SL pick:** **W1** always in pill; **W2** on expand / hover dwell. Mock data ok until provider wired.

---

## 3. Icon sets (status / notifications)

| ID | Set | Stroke / fill | Use |
|----|-----|---------------|-----|
| **I1** | **Fluent System** (default) | Segoe Fluent Icons / FontIcon | Baseline Win11 look |
| **I2** | **Minimal Line** | 1.5px PathIcon outlines | Minimal themes |
| **I3** | **Filled Active** | Solid + `AccentHex` when DND / unread / mute | Active affordance |

**Targets:** DND, Wi-Fi/network, Mute, Unread/bell (extend current `StateIcon` + badge).

**SL pick:** **I1** default; **I3** for active states overlay; **I2** optional theme.

---

## 4. Glow / pulse / hover effects

| ID | Effect | Mechanism |
|----|--------|-----------|
| **E1** | **Border Glow** | Outer `ThemeShadow` / `DropShadow` on `Pill`; BlurRadius + Accent/Border color + opacity |
| **E2** | **Pulse Aura** | Existing `PillScale` pattern: Scale 1.0→1.08 + opacity fade on unread (extend beyond Dot) |
| **E3** | **Hover Highlight** | Soft gradient / Acrylic blush on `PointerEntered` (already have enter/exit hooks) |

**Refs:** WinUI ThemeShadow samples; Composition spring animations; current island Dot pulse nit from 003.

**SL pick:** enable **E2** when `PulseOnUnread`; **E3** always subtle; **E1** opt-in via settings (can be busy on Mica).

---

## Proposed settings surface (for later code, not now)

```
ClockStyle: Modern | Minimal | SecondsArc | AnalogExpand
WeatherMode: Compact | ExpandedOnHover
IconSet: Fluent | MinimalLine | FilledActive
BorderGlow: bool
PulseAura: bool (align PulseOnUnread)
HoverHighlight: bool
TempUnit: C | F
```

---

## Ask CC

Approve package: **C1+C2+C3**, **W1+W2**, **I1+I2+I3**, **E1+E2+E3** with defaults **C1 / W1 / I1 / E2+E3**.  
Defer **C4** to expand-only if you want a smaller first ship.

After CC ok → NB+SL implement in `island/` only.

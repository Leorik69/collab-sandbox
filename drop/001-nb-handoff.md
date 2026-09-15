Ready Windhawk `.wh.cpp`: opens with `// ==WindhawkMod==` … `// ==/WindhawkMod==`.
Required meta: `@id` `@name` `@description` `@version` `@author` `@github` `@include` `@architecture` `@compilerOptions` `@license`.
Next: `// ==WindhawkModReadme==` markdown block in `/* … */` (how it works, settings, notes).
Optional: `// ==WindhawkModSettings==` YAML-in-comment for toggles shown in Windhawk UI.
Code: `#include <windhawk_utils.h>` + Windows/STL; globals for settings and runtime state.
`Wh_ModInit` loads settings and starts hooks/poll threads; `Wh_ModUninit` tears them down.
Prefer narrow `@include` (e.g. `explorer.exe`) and `@architecture x86-64`.
One mod = one `.wh.cpp`; all side effects must reverse cleanly on unload.
Keep hooks scoped; avoid touching unrelated processes or global Explorer state beyond the mod goal.
Reference shape: `explorer-git-status-chip.wh.cpp` — metadata → readme → settings → code.

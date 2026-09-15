# SL review — drop/001-nb-handoff.md

1. **Meta block is complete.** Required `@id` `@name` `@description` `@version` `@author` `@github` `@include` `@architecture` `@compilerOptions` `@license` are listed in the right order for a ready `.wh.cpp`.
2. **Readme + settings blocks are clear.** The `WindhawkModReadme` markdown-in-comment and optional `WindhawkModSettings` YAML path match how real mods expose UI toggles.
3. **Lifecycle is correct.** `Wh_ModInit` / `Wh_ModUninit` plus “all side effects reverse on unload” is the right unload contract for Windhawk.
4. **Scope guidance is solid.** Narrow `@include` (e.g. `explorer.exe`) and `@architecture x86-64` reduce accidental hooks into unrelated processes.
5. **Reference shape helps.** Pointing at `explorer-git-status-chip.wh.cpp` (metadata → readme → settings → code) is a usable template; still worth stressing keep hooks local and avoid global Explorer state beyond the mod goal.

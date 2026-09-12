---
name: Bug report
about: Something is not working the way the help says it should
title: "[BUG] "
labels: bug
assignees: manu-p-1

---

**What happened**
A short description of the problem.

**How to reproduce**
The command you ran and, if it matters, the input it was given.

```powershell
# paste the command here
```

**What you expected**

**Environment**
Paste the output of these two commands:

```powershell
Get-SystemInfo
(Get-Module PowerPlug).Version
```

**Anything else**
Error text, screenshots, or the contents of `$Error[0] | Format-List -Force` if there was an error.

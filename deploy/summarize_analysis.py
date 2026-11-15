"""Короткий звіт C4 із SARIF; сирі діагностики залишаються поряд без фільтрації."""

import json
import sys
from collections import Counter
from pathlib import Path


def main():
    files = sorted(Path(sys.argv[1]).glob("*.sarif"))
    if not files:
        raise SystemExit("SARIF files not found; run the build with AnalysisOutputDirectory first.")
    print("# Статичний аналіз .NET\n")
    print("Джерело: C# compiler / .NET analyzers, SARIF 2.1. "
          "`error` / `warning` не перейменовуються в категорії SonarQube. "
          "Suppressed-діагностики та інформаційні підказки збережені в сирих файлах.\n")
    print("| Проєкт | Активні error | Активні warning | Note / none | Suppressed |")
    print("|---|---:|---:|---:|---:|")
    total = Counter()
    for path in files:
        counts = Counter()
        for run in json.loads(path.read_text())["runs"]:
            for result in run.get("results", []):
                suppressed = any(s.get("status") != "rejected" for s in result.get("suppressions", []))
                counts["suppressed" if suppressed else result.get("level", "warning")] += 1
        total.update(counts)
        print(f"| {path.stem} | {counts['error']} | {counts['warning']} | "
              f"{counts['note'] + counts['none']} | {counts['suppressed']} |")
    print(f"| **Разом** | **{total['error']}** | **{total['warning']}** | "
          f"**{total['note'] + total['none']}** | **{total['suppressed']}** |")
    print("\nОстаточний результат збірки міститься в build.log. "
          "Цей підсумок не змінює правила аналізатора й не приглушує зауважень.")


if __name__ == "__main__":
    main()

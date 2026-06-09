import { describe, expect, it } from "vitest"
import { summarizeTrend } from "./trend"

describe("summarizeTrend", () => {
  it("should return zeros when there are no points", () => {
    expect(summarizeTrend([])).toEqual({ total: 0, lastDelta: 0, averageDelta: 0 })
  })

  it("should summarize totals and deltas when history exists", () => {
    const summary = summarizeTrend([
      { capturedAt: "2026-06-01", totalDownloads: 100, delta: 0 },
      { capturedAt: "2026-06-02", totalDownloads: 120, delta: 20 },
      { capturedAt: "2026-06-03", totalDownloads: 150, delta: 30 },
    ])

    expect(summary).toEqual({ total: 150, lastDelta: 30, averageDelta: 25 })
  })
})

import type { DownloadTrendPointDto } from "../api/client"

export interface TrendSummary {
  readonly total: number
  readonly lastDelta: number
  readonly averageDelta: number
}

export const summarizeTrend = (
  points: ReadonlyArray<DownloadTrendPointDto>,
): TrendSummary => {
  if (points.length === 0) {
    return { total: 0, lastDelta: 0, averageDelta: 0 }
  }

  const deltas = points.slice(1).map((p) => p.delta)
  const last = points[points.length - 1]

  return {
    total: last?.totalDownloads ?? 0,
    lastDelta: last?.delta ?? 0,
    averageDelta:
      deltas.length === 0
        ? 0
        : deltas.reduce((sum, d) => sum + d, 0) / deltas.length,
  }
}

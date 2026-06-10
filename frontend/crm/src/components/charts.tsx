import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import type {
  DailyDownloadReportDto,
  DailyTrafficPointDto,
  MonthlyRevenuePointDto,
  RepositorySnapshotDto,
} from "../api/client"
import { formatDate, formatMoney } from "./ui"

const PALETTE = ["#6366f1", "#34d399", "#fbbf24", "#60a5fa", "#f472b6", "#f87171"]

const tooltipStyle = {
  backgroundColor: "#18181b",
  border: "1px solid #3f3f46",
  borderRadius: 8,
  fontSize: 13,
} as const

const axisProps = {
  stroke: "#71717a",
  tick: { fill: "#a1a1aa", fontSize: 11 },
  tickLine: false,
} as const

/** Téléchargements quotidiens, barres empilées par source (registry:package). */
export const DailyDownloadsChart = ({ report }: { report: DailyDownloadReportDto }) => {
  const sourceKeys = report.sources.map((s) => `${s.registry}:${s.packageId}`)

  const rows = report.days.map((day, index) => {
    const row: Record<string, string | number> = { date: day.date }
    for (const source of report.sources) {
      row[`${source.registry}:${source.packageId}`] = source.days[index]?.downloads ?? 0
    }
    if (report.sources.length === 0) {
      row["Téléchargements"] = day.downloads
    }
    return row
  })

  const keys = sourceKeys.length > 0 ? sourceKeys : ["Téléchargements"]

  return (
    <div className="chart-wrap">
      <ResponsiveContainer>
        <BarChart data={rows}>
          <CartesianGrid strokeDasharray="3 3" stroke="#27272a" vertical={false} />
          <XAxis dataKey="date" {...axisProps} tickFormatter={formatDate} />
          <YAxis {...axisProps} allowDecimals={false} width={42} />
          <Tooltip contentStyle={tooltipStyle} labelFormatter={(v) => formatDate(String(v))} />
          {keys.length > 1 ? <Legend wrapperStyle={{ fontSize: 12 }} /> : null}
          {keys.map((key, i) => (
            <Bar key={key} dataKey={key} stackId="downloads" fill={PALETTE[i % PALETTE.length]} />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}

export const DailyTrafficChart = ({ days }: { days: ReadonlyArray<DailyTrafficPointDto> }) => (
  <div className="chart-wrap">
    <ResponsiveContainer>
      <AreaChart data={[...days]}>
        <defs>
          <linearGradient id="traffic-fill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#6366f1" stopOpacity={0.45} />
            <stop offset="100%" stopColor="#6366f1" stopOpacity={0.02} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#27272a" vertical={false} />
        <XAxis dataKey="date" {...axisProps} tickFormatter={formatDate} />
        <YAxis {...axisProps} allowDecimals={false} width={42} />
        <Tooltip contentStyle={tooltipStyle} labelFormatter={(v) => formatDate(String(v))} />
        <Area
          type="monotone"
          dataKey="pageViews"
          name="Pages vues"
          stroke="#818cf8"
          fill="url(#traffic-fill)"
          strokeWidth={2}
        />
      </AreaChart>
    </ResponsiveContainer>
  </div>
)

/**
 * Revenus mensuels (devise primaire) : barres empilées récurrent / one-shot,
 * ligne du cumul sur un second axe.
 */
export const MonthlyRevenueChart = ({
  series,
  currency,
}: {
  series: ReadonlyArray<MonthlyRevenuePointDto>
  currency: string
}) => {
  const rows = series.map((point) => ({
    month: point.month,
    Récurrent: point.recurring,
    "One-shot": point.total - point.recurring,
    Cumul: point.cumulative,
  }))

  return (
    <div className="chart-wrap">
      <ResponsiveContainer>
        <ComposedChart data={rows}>
          <CartesianGrid strokeDasharray="3 3" stroke="#27272a" vertical={false} />
          <XAxis dataKey="month" {...axisProps} />
          <YAxis yAxisId="month" {...axisProps} width={56} />
          <YAxis yAxisId="cumulative" orientation="right" {...axisProps} width={56} />
          <Tooltip
            contentStyle={tooltipStyle}
            formatter={(value) => formatMoney(Number(value), currency)}
          />
          <Legend wrapperStyle={{ fontSize: 12 }} />
          <Bar yAxisId="month" dataKey="Récurrent" stackId="revenue" fill="#34d399" />
          <Bar yAxisId="month" dataKey="One-shot" stackId="revenue" fill="#6366f1" />
          <Line
            yAxisId="cumulative"
            type="monotone"
            dataKey="Cumul"
            stroke="#fbbf24"
            strokeWidth={2}
            dot={false}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}

export const RepositoryHistoryChart = ({
  history,
}: {
  history: ReadonlyArray<RepositorySnapshotDto>
}) => (
  <div className="chart-wrap">
    <ResponsiveContainer>
      <LineChart data={[...history]}>
        <CartesianGrid strokeDasharray="3 3" stroke="#27272a" vertical={false} />
        <XAxis dataKey="capturedAt" {...axisProps} tickFormatter={formatDate} />
        <YAxis {...axisProps} allowDecimals={false} width={42} />
        <Tooltip contentStyle={tooltipStyle} labelFormatter={(v) => formatDate(String(v))} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        <Line type="monotone" dataKey="stars" name="Stars" stroke="#fbbf24" strokeWidth={2} dot={false} />
        <Line
          type="monotone"
          dataKey="openIssues"
          name="Issues ouvertes"
          stroke="#f87171"
          strokeWidth={2}
          dot={false}
        />
      </LineChart>
    </ResponsiveContainer>
  </div>
)

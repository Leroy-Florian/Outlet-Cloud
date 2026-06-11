// API client — plain fetch, contract = src/Hosts/Outlet.Crm.Web/Endpoints/CrmEndpoints.cs
// (camelCase JSON, enums sérialisés en string, DateOnly en "yyyy-MM-dd").

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message)
  }
}

interface ProblemDetails {
  readonly title?: string
  readonly detail?: string
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, init)
  if (!response.ok) {
    let message = `Requête ${path} échouée (${response.status})`
    try {
      const problem = (await response.json()) as ProblemDetails
      if (problem.detail !== undefined || problem.title !== undefined) {
        message = [problem.title, problem.detail].filter(Boolean).join(" : ")
      }
    } catch {
      // body non-JSON — on garde le message générique
    }
    throw new ApiError(response.status, message)
  }
  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}

const getJson = <T>(path: string) => request<T>(path)

const sendJson = <T>(method: string, path: string, body?: unknown) =>
  request<T>(path, {
    method,
    ...(body === undefined
      ? {}
      : {
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(body),
        }),
  })

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

export type PackageRegistry = "NuGet" | "Npm"

export interface TrackedPackageDto {
  readonly registry: string
  readonly packageId: string
}

export interface ProductDto {
  readonly id: string
  readonly name: string
  readonly description: string | null
  readonly packages: ReadonlyArray<TrackedPackageDto>
  readonly repositories: ReadonlyArray<string>
  readonly isArchived: boolean
  readonly createdAt: string
}

export interface OrganizationDto {
  readonly id: string
  readonly name: string
  readonly website: string | null
  readonly createdAt: string
}

export interface ProspectDto {
  readonly id: string
  readonly productId: string
  readonly organizationId: string | null
  readonly name: string
  readonly email: string
  readonly company: string | null
  readonly estimatedValue: number | null
  readonly estimatedValueCurrency: string | null
  readonly stage: string
  readonly lossReason: string | null
  readonly createdAt: string
}

export interface ProspectStageStatsDto {
  readonly stage: string
  readonly count: number
  readonly totalEstimatedValue: number
  readonly conversionRateToNext: number | null
}

export interface ProspectPipelineStatsDto {
  readonly totalProspects: number
  readonly totalEstimatedValue: number
  readonly stages: ReadonlyArray<ProspectStageStatsDto>
}

export interface PaymentDto {
  readonly id: string
  readonly productId: string
  readonly organizationId: string | null
  readonly amount: number
  readonly currency: string
  readonly source: string
  readonly externalReference: string
  readonly isRecurring: boolean
  readonly status: string
  readonly createdAt: string
}

export interface DailyDownloadPointDto {
  readonly date: string
  readonly downloads: number
}

export interface DownloadSourceBreakdownDto {
  readonly registry: string
  readonly packageId: string
  readonly downloads: number
  readonly days: ReadonlyArray<DailyDownloadPointDto>
}

/** Release publiée dans la plage du rapport — marqueur vertical sur le graphique. */
export interface ReleaseMarkerDto {
  /** "yyyy-MM-dd" */
  readonly date: string
  readonly tagName: string
  readonly repository: string
}

export interface DailyDownloadReportDto {
  readonly from: string
  readonly to: string
  readonly totalDownloads: number
  readonly days: ReadonlyArray<DailyDownloadPointDto>
  readonly sources: ReadonlyArray<DownloadSourceBreakdownDto>
  readonly releases: ReadonlyArray<ReleaseMarkerDto>
}

export interface DailyTrafficPointDto {
  readonly date: string
  readonly pageViews: number
}

export interface TrafficCountDto {
  readonly key: string
  readonly count: number
}

export interface DailyTrafficReportDto {
  readonly from: string
  readonly to: string
  readonly totalPageViews: number
  readonly days: ReadonlyArray<DailyTrafficPointDto>
  readonly topPaths: ReadonlyArray<TrafficCountDto>
  readonly topReferrers: ReadonlyArray<TrafficCountDto>
}

export interface PackageTotalDto {
  readonly registry: string
  readonly packageId: string
  readonly totalDownloads: number
  readonly latestVersion: string | null
  readonly capturedAt: string
}

export interface RepositoryTotalDto {
  readonly repository: string
  readonly stars: number
  readonly openIssues: number
  readonly forks: number
  readonly capturedAt: string
}

export type TrendDirection = "Flat" | "Up" | "Down"

export interface PeriodComparisonDto {
  readonly currentPeriod: number
  readonly previousPeriod: number
  readonly percentChange: number | null
  readonly direction: TrendDirection
}

export interface AnalyticsSummaryDto {
  readonly totalDownloads: number
  readonly downloadsLast7Days: number
  readonly downloadsLast30Days: number
  readonly pageViewsLast7Days: number
  readonly pageViewsLast30Days: number
  readonly periodDays: number
  readonly downloads: PeriodComparisonDto
  readonly pageViews: PeriodComparisonDto
  readonly packages: ReadonlyArray<PackageTotalDto>
  readonly repositories: ReadonlyArray<RepositoryTotalDto>
}

export interface PortfolioProductSummaryDto {
  readonly productId: string
  readonly name: string
  readonly packageCount: number
  readonly totalDownloads: number
  readonly latestStars: number
  readonly openFeedbackCount: number
  readonly downloads: PeriodComparisonDto
  readonly pageViews: PeriodComparisonDto
}

export interface PortfolioSummaryDto {
  readonly periodDays: number
  readonly products: ReadonlyArray<PortfolioProductSummaryDto>
}

export type FeedbackStatus = "New" | "Triaged" | "Resolved" | "Dismissed"
export type FeedbackCategory = "Bug" | "FeatureRequest" | "Question" | "Other"

export interface FeedbackItemDto {
  readonly id: string
  readonly productId: string
  readonly category: string
  readonly message: string
  readonly reporterEmail: string | null
  readonly sourceApp: string
  /** Score NPS 0-10, null si non renseigné. */
  readonly score: number | null
  readonly status: string
  readonly receivedAt: string
}

export interface FeedbackCountsDto {
  readonly new: number
  readonly triaged: number
  readonly resolved: number
  readonly dismissed: number
  readonly total: number
}

export interface FeedbackInboxDto {
  readonly items: ReadonlyArray<FeedbackItemDto>
  readonly counts: FeedbackCountsDto
}

export interface SnapshotCaptureReportDto {
  readonly target: string
  readonly succeeded: boolean
  readonly error: string | null
}

export interface DownloadTrendPointDto {
  readonly capturedAt: string
  readonly totalDownloads: number
  readonly delta: number
}

export interface RepositorySnapshotDto {
  readonly repository: string
  readonly openIssues: number
  readonly stars: number
  readonly forks: number
  readonly capturedAt: string
}

export interface ProductHealthInputsDto {
  readonly daysSinceLatestRelease: number | null
  readonly downloadsPercentChange: number | null
  readonly openIssuesGrowthPercent: number | null
  readonly starsGrowthPercent: number | null
  readonly recentCaptureFailures: number
}

export interface ProductHealthComponentsDto {
  readonly releaseFreshness: number
  readonly downloadTrend: number
  readonly repoActivity: number
  readonly snapshotReliability: number
}

export interface ProductHealthDto {
  readonly total: number
  readonly label: string
  readonly components: ProductHealthComponentsDto
  readonly inputs: ProductHealthInputsDto
}

export type ObjectiveMetric = "Downloads" | "PageViews" | "Revenue" | "Prospects"

export interface ObjectiveProgressDto {
  readonly id: string
  readonly productId: string | null
  readonly metric: string
  readonly targetValue: number
  readonly actualValue: number
  /** Pourcentage brut, non plafonné — l'affichage est clipé à 100. */
  readonly progressPercent: number
}

export interface ObjectivesProgressDto {
  /** "yyyy-MM" */
  readonly month: string
  readonly objectives: ReadonlyArray<ObjectiveProgressDto>
}




// ---------------------------------------------------------------------------
// Produits
// ---------------------------------------------------------------------------

export const listProducts = () => getJson<ReadonlyArray<ProductDto>>("/api/products/")

export const createProduct = (name: string, description: string | null) =>
  sendJson<{ id: string }>("POST", "/api/products/", { name, description })

export const updateProduct = (productId: string, name: string, description: string | null) =>
  sendJson<void>("PUT", `/api/products/${productId}`, { name, description })

export const archiveProduct = (productId: string) =>
  sendJson<void>("DELETE", `/api/products/${productId}`)

export const trackPackage = (productId: string, registry: PackageRegistry, packageId: string) =>
  sendJson<void>("POST", `/api/products/${productId}/packages`, { registry, packageId })

export const untrackPackage = (productId: string, registry: string, packageId: string) =>
  sendJson<void>(
    "DELETE",
    `/api/products/${productId}/packages/${registry}/${encodeURIComponent(packageId)}`,
  )

export const trackRepository = (productId: string, repository: string) =>
  sendJson<void>("POST", `/api/products/${productId}/repositories`, { repository })

export const untrackRepository = (productId: string, repository: string) =>
  sendJson<void>("DELETE", `/api/products/${productId}/repositories/${repository}`)

export const captureSnapshots = (productId: string) =>
  sendJson<ReadonlyArray<SnapshotCaptureReportDto>>(
    "POST",
    `/api/products/${productId}/snapshots`,
  )

// ---------------------------------------------------------------------------
// Releases
// ---------------------------------------------------------------------------

export interface ReleaseDto {
  readonly id: string
  readonly repository: string
  readonly tagName: string
  readonly name: string | null
  readonly publishedAt: string
}

export interface ReleaseSyncTargetDto {
  readonly repository: string
  readonly succeeded: boolean
  readonly newReleases: number
  readonly error: string | null
}

export interface ReleaseSyncSummaryDto {
  readonly newReleases: number
  readonly targets: ReadonlyArray<ReleaseSyncTargetDto>
}

export const listReleases = (productId: string) =>
  getJson<ReadonlyArray<ReleaseDto>>(`/api/products/${productId}/releases`)

export const syncReleases = (productId: string) =>
  sendJson<ReleaseSyncSummaryDto>("POST", `/api/products/${productId}/releases/sync`)

// ---------------------------------------------------------------------------
// Analytics
// ---------------------------------------------------------------------------

const rangeQuery = (from?: string, to?: string) => {
  const params = new URLSearchParams()
  if (from !== undefined) params.set("from", from)
  if (to !== undefined) params.set("to", to)
  const query = params.toString()
  return query === "" ? "" : `?${query}`
}

export const getDailyDownloads = (productId: string, from?: string, to?: string) =>
  getJson<DailyDownloadReportDto>(
    `/api/products/${productId}/analytics/downloads/daily${rangeQuery(from, to)}`,
  )

export const getDailyTraffic = (productId: string, from?: string, to?: string) =>
  getJson<DailyTrafficReportDto>(
    `/api/products/${productId}/analytics/traffic/daily${rangeQuery(from, to)}`,
  )

export const getAnalyticsSummary = (productId: string, days?: number) =>
  getJson<AnalyticsSummaryDto>(
    `/api/products/${productId}/analytics/summary${days === undefined ? "" : `?days=${days}`}`,
  )

export const getPortfolio = (days?: number) =>
  getJson<PortfolioSummaryDto>(
    `/api/analytics/portfolio${days === undefined ? "" : `?days=${days}`}`,
  )

export const getDownloadTrend = (productId: string, registry: string, packageId: string) =>
  getJson<ReadonlyArray<DownloadTrendPointDto>>(
    `/api/products/${productId}/packages/${registry}/${encodeURIComponent(packageId)}/trend`,
  )

export const getRepositoryHistory = (productId: string, repository: string) =>
  getJson<ReadonlyArray<RepositorySnapshotDto>>(
    `/api/products/${productId}/repositories/${repository}/history`,
  )

// ---------------------------------------------------------------------------
// Organisations / Prospects / Paiements
// ---------------------------------------------------------------------------

export const listOrganizations = () =>
  getJson<ReadonlyArray<OrganizationDto>>("/api/organizations/")

export const listProspects = () => getJson<ReadonlyArray<ProspectDto>>("/api/prospects/")

export interface CreateProspectInput {
  readonly productId: string
  readonly organizationId: string | null
  readonly name: string
  readonly email: string
  readonly company: string | null
}

export const createProspect = (input: CreateProspectInput) =>
  sendJson<{ id: string }>("POST", "/api/prospects/", input)

export const advanceProspectStage = (prospectId: string, target: string) =>
  sendJson<void>("POST", `/api/prospects/${prospectId}/stage?target=${target}`)

export interface UpdateProspectInput {
  readonly estimatedValue: number | null
  readonly estimatedValueCurrency: string | null
  readonly company: string | null
}

export const updateProspect = (prospectId: string, input: UpdateProspectInput) =>
  sendJson<void>("PATCH", `/api/prospects/${prospectId}`, input)

export const getProspectPipelineStats = () =>
  getJson<ProspectPipelineStatsDto>("/api/prospects/stats")

// ---------------------------------------------------------------------------
// Feedback
// ---------------------------------------------------------------------------

export interface FeedbackFilters {
  readonly productId?: string | undefined
  readonly status?: FeedbackStatus | undefined
  readonly category?: FeedbackCategory | undefined
}

export const getFeedbackInbox = (filters: FeedbackFilters = {}) => {
  const params = new URLSearchParams()
  if (filters.productId !== undefined) params.set("productId", filters.productId)
  if (filters.status !== undefined) params.set("status", filters.status)
  if (filters.category !== undefined) params.set("category", filters.category)
  const query = params.toString()
  return getJson<FeedbackInboxDto>(`/api/feedback/${query === "" ? "" : `?${query}`}`)
}

export const triageFeedback = (feedbackId: string) =>
  sendJson<void>("POST", `/api/feedback/${feedbackId}/triage`)

export const resolveFeedback = (feedbackId: string) =>
  sendJson<void>("POST", `/api/feedback/${feedbackId}/resolve`)

export const dismissFeedback = (feedbackId: string) =>
  sendJson<void>("POST", `/api/feedback/${feedbackId}/dismiss`)

export interface NpsReportDto {
  readonly nps: number | null
  readonly promoters: number
  readonly passives: number
  readonly detractors: number
  readonly total: number
  readonly days: number
}

export const getNps = (productId?: string, days?: number) => {
  const params = new URLSearchParams()
  if (productId !== undefined) params.set("productId", productId)
  if (days !== undefined) params.set("days", String(days))
  const query = params.toString()
  return getJson<NpsReportDto>(`/api/feedback/nps${query === "" ? "" : `?${query}`}`)
}

export const listPayments = () => getJson<ReadonlyArray<PaymentDto>>("/api/payments/")

export interface RecordPaymentInput {
  readonly productId: string
  readonly organizationId: string | null
  readonly amount: number
  readonly currency: string
  readonly source: string
  readonly externalReference: string
  readonly isRecurring: boolean
}

export const recordPayment = (input: RecordPaymentInput) =>
  sendJson<{ id: string }>("POST", "/api/payments/", input)

export const settlePayment = (paymentId: string) =>
  sendJson<void>("POST", `/api/payments/${paymentId}/settle`)

// ---------------------------------------------------------------------------
// Revenus
// ---------------------------------------------------------------------------

export interface MonthlyProductRevenueDto {
  readonly productId: string
  readonly amount: number
}

export interface MonthlyRevenuePointDto {
  /** "yyyy-MM" */
  readonly month: string
  readonly total: number
  readonly recurring: number
  readonly cumulative: number
  readonly byProduct: ReadonlyArray<MonthlyProductRevenueDto>
}

export interface CurrencyTotalDto {
  readonly currency: string
  readonly total: number
}

export interface RevenueMetricsDto {
  readonly primaryCurrency: string
  readonly months: number
  readonly mrr: number
  readonly churnMonths: number
  readonly series: ReadonlyArray<MonthlyRevenuePointDto>
  readonly currencyTotals: ReadonlyArray<CurrencyTotalDto>
}

export const getRevenueMetrics = (months?: number) =>
  getJson<RevenueMetricsDto>(
    `/api/revenue/metrics${months === undefined ? "" : `?months=${months}`}`,
  )

// ---------------------------------------------------------------------------
// Alertes
// ---------------------------------------------------------------------------

export type AlertType = "DownloadsSpike" | "DownloadsDrop" | "StarsMilestone" | "SnapshotFailure"

export interface AlertDto {
  readonly id: string
  readonly productId: string
  readonly type: string
  readonly message: string
  readonly triggeredAt: string
  readonly acknowledged: boolean
}

export interface AlertFilters {
  readonly productId?: string | undefined
  readonly acknowledged?: boolean | undefined
}

export const listAlerts = (filters: AlertFilters = {}) => {
  const params = new URLSearchParams()
  if (filters.productId !== undefined) params.set("productId", filters.productId)
  if (filters.acknowledged !== undefined) params.set("acknowledged", String(filters.acknowledged))
  const query = params.toString()
  return getJson<ReadonlyArray<AlertDto>>(`/api/alerts/${query === "" ? "" : `?${query}`}`)
}

export const acknowledgeAlert = (alertId: string) =>
  sendJson<void>("POST", `/api/alerts/${alertId}/acknowledge`)

export const evaluateAlerts = (productId: string) =>
  sendJson<ReadonlyArray<AlertDto>>("POST", `/api/products/${productId}/alerts/evaluate`)

// ---------------------------------------------------------------------------
// Santé produit
// ---------------------------------------------------------------------------

export const getProductHealth = (productId: string) =>
  getJson<ProductHealthDto>(`/api/products/${productId}/health`)

// ---------------------------------------------------------------------------
// Objectifs
// ---------------------------------------------------------------------------

export interface SetObjectiveInput {
  readonly productId: string | null
  readonly metric: ObjectiveMetric
  /** "yyyy-MM" */
  readonly month: string
  readonly targetValue: number
}

export const setObjective = (input: SetObjectiveInput) =>
  sendJson<{ id: string }>("PUT", "/api/objectives/", input)

export const deleteObjective = (objectiveId: string) =>
  sendJson<void>("DELETE", `/api/objectives/${objectiveId}`)

export const getObjectivesProgress = (month?: string) =>
  getJson<ObjectivesProgressDto>(
    `/api/objectives/progress${month === undefined ? "" : `?month=${month}`}`,
  )

// ---------------------------------------------------------------------------
// Factures
// ---------------------------------------------------------------------------








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
  readonly stage: string
  readonly createdAt: string
}

export interface PaymentDto {
  readonly id: string
  readonly productId: string
  readonly organizationId: string | null
  readonly amount: number
  readonly currency: string
  readonly source: string
  readonly externalReference: string
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

export interface DailyDownloadReportDto {
  readonly from: string
  readonly to: string
  readonly totalDownloads: number
  readonly days: ReadonlyArray<DailyDownloadPointDto>
  readonly sources: ReadonlyArray<DownloadSourceBreakdownDto>
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
  readonly capturedAt: string
}

export interface RepositoryTotalDto {
  readonly repository: string
  readonly stars: number
  readonly openIssues: number
  readonly forks: number
  readonly capturedAt: string
}

export interface AnalyticsSummaryDto {
  readonly totalDownloads: number
  readonly downloadsLast7Days: number
  readonly downloadsLast30Days: number
  readonly pageViewsLast7Days: number
  readonly pageViewsLast30Days: number
  readonly packages: ReadonlyArray<PackageTotalDto>
  readonly repositories: ReadonlyArray<RepositoryTotalDto>
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

export const getAnalyticsSummary = (productId: string) =>
  getJson<AnalyticsSummaryDto>(`/api/products/${productId}/analytics/summary`)

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

export const listPayments = () => getJson<ReadonlyArray<PaymentDto>>("/api/payments/")

export interface RecordPaymentInput {
  readonly productId: string
  readonly organizationId: string | null
  readonly amount: number
  readonly currency: string
  readonly source: string
  readonly externalReference: string
}

export const recordPayment = (input: RecordPaymentInput) =>
  sendJson<{ id: string }>("POST", "/api/payments/", input)

export const settlePayment = (paymentId: string) =>
  sendJson<void>("POST", `/api/payments/${paymentId}/settle`)

import { NavLink, Outlet, useLocation } from "react-router-dom"
import { getFeedbackInbox, listAlerts } from "../api/client"
import { useQuery } from "../hooks/useQuery"

const links = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/produits", label: "Produits", end: false },
  { to: "/prospects", label: "Prospects", end: true },
  { to: "/paiements", label: "Paiements", end: true },
  { to: "/revenus", label: "Revenus", end: true },
  { to: "/objectifs", label: "Objectifs", end: true },
  { to: "/alertes", label: "Alertes", end: true },
  { to: "/feedback", label: "Feedback", end: true },
]

export const Layout = () => {
  // Rechargés à chaque navigation pour garder les compteurs à jour.
  const location = useLocation()
  const inbox = useQuery(() => getFeedbackInbox(), [location.pathname])
  const openCount =
    inbox.data === null ? 0 : inbox.data.counts.new + inbox.data.counts.triaged
  const alerts = useQuery(() => listAlerts({ acknowledged: false }), [location.pathname])
  const alertCount = alerts.data?.length ?? 0

  return (
    <div className="app">
      <aside className="sidebar">
        <div className="sidebar-brand">
          Outlet <span>CRM</span>
        </div>
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            end={link.end}
            className={({ isActive }) => (isActive ? "nav-link active" : "nav-link")}
          >
            {link.label}
            {link.to === "/feedback" && openCount > 0 ? (
              <span className="nav-count">{openCount}</span>
            ) : null}
            {link.to === "/alertes" && alertCount > 0 ? (
              <span className="nav-count">{alertCount}</span>
            ) : null}
          </NavLink>
        ))}
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}

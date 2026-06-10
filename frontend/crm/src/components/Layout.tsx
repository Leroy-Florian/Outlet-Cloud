import { NavLink, Outlet, useLocation } from "react-router-dom"
import { getFeedbackInbox } from "../api/client"
import { useQuery } from "../hooks/useQuery"

const links = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/produits", label: "Produits", end: false },
  { to: "/prospects", label: "Prospects", end: true },
  { to: "/paiements", label: "Paiements", end: true },
  { to: "/feedback", label: "Feedback", end: true },
]

export const Layout = () => {
  // Rechargé à chaque navigation pour garder le compteur « ouvert » à jour.
  const location = useLocation()
  const inbox = useQuery(() => getFeedbackInbox(), [location.pathname])
  const openCount =
    inbox.data === null ? 0 : inbox.data.counts.new + inbox.data.counts.triaged

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
          </NavLink>
        ))}
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}

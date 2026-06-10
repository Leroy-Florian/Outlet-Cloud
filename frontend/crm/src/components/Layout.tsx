import { NavLink, Outlet } from "react-router-dom"

const links = [
  { to: "/", label: "Dashboard", end: true },
  { to: "/produits", label: "Produits", end: false },
  { to: "/prospects", label: "Prospects", end: true },
  { to: "/paiements", label: "Paiements", end: true },
]

export const Layout = () => (
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
        </NavLink>
      ))}
    </aside>
    <main className="content">
      <Outlet />
    </main>
  </div>
)

import { BrowserRouter, Route, Routes } from "react-router-dom"
import { Layout } from "./components/Layout"
import { AlertsPage } from "./pages/AlertsPage"
import { DashboardPage } from "./pages/DashboardPage"
import { RevenuePage } from "./pages/RevenuePage"
import { FeedbackPage } from "./pages/FeedbackPage"
import { PaymentsPage } from "./pages/PaymentsPage"
import { ProductDetailPage } from "./pages/ProductDetailPage"
import { ProductsPage } from "./pages/ProductsPage"
import { ProspectsPage } from "./pages/ProspectsPage"

export const App = () => (
  <BrowserRouter>
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<DashboardPage />} />
        <Route path="/produits" element={<ProductsPage />} />
        <Route path="/produits/:productId" element={<ProductDetailPage />} />
        <Route path="/prospects" element={<ProspectsPage />} />
        <Route path="/paiements" element={<PaymentsPage />} />
        <Route path="/revenus" element={<RevenuePage />} />
        <Route path="/alertes" element={<AlertsPage />} />
        <Route path="/feedback" element={<FeedbackPage />} />
      </Route>
    </Routes>
  </BrowserRouter>
)

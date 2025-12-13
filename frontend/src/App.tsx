import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { useAuthInit } from './hooks/useAuth';
import { Layout } from './components/layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { PublicRoute } from './components/PublicRoute';
import { ToastProvider } from './components/ui/toast';
import Home from './pages/Home';
import { Login, Register, ForgotPassword, ResetPassword } from './pages/auth';
import { Settings } from './pages/settings';
import { AdminDashboard, AdminUsers, AdminSharedLocations } from './pages/admin';

// Lazy load pages for better code splitting
import { lazy, Suspense } from 'react';

// Rifles
const RiflesList = lazy(() => import('./pages/rifles/RiflesList'));
const RifleDetail = lazy(() => import('./pages/rifles/RifleDetail'));
const RifleCreate = lazy(() => import('./pages/rifles/RifleCreate'));
const RifleEdit = lazy(() => import('./pages/rifles/RifleEdit'));

// Ammunition
const AmmoList = lazy(() => import('./pages/ammunition/AmmoList'));
const AmmoDetail = lazy(() => import('./pages/ammunition/AmmoDetail'));
const AmmoCreate = lazy(() => import('./pages/ammunition/AmmoCreate'));
const AmmoEdit = lazy(() => import('./pages/ammunition/AmmoEdit'));
const LotForm = lazy(() => import('./pages/ammunition/LotForm'));

// Locations
const LocationsList = lazy(() => import('./pages/locations/LocationsList'));
const LocationDetail = lazy(() => import('./pages/locations/LocationDetail'));
const LocationCreate = lazy(() => import('./pages/locations/LocationCreate'));
const LocationEdit = lazy(() => import('./pages/locations/LocationEdit'));

// Sessions
const SessionsList = lazy(() => import('./pages/sessions/SessionsList'));
const SessionDetail = lazy(() => import('./pages/sessions/SessionDetail'));
const SessionCreate = lazy(() => import('./pages/sessions/SessionCreate'));
const SessionEdit = lazy(() => import('./pages/sessions/SessionEdit'));

// Analytics
const AnalyticsDashboard = lazy(() => import('./pages/analytics/AnalyticsDashboard'));
const DopeChart = lazy(() => import('./pages/analytics/DopeChart'));
const VelocityTrends = lazy(() => import('./pages/analytics/VelocityTrends'));
const AmmoComparison = lazy(() => import('./pages/analytics/AmmoComparison'));
const LotComparison = lazy(() => import('./pages/analytics/LotComparison'));
const CostAnalysis = lazy(() => import('./pages/analytics/CostAnalysis'));

// Loading fallback
function PageLoader() {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
    </div>
  );
}

function AppContent() {
  // Initialize auth state on app load
  useAuthInit();

  return (
    <Routes>
      {/* Public auth routes (redirect if logged in) */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <Login />
          </PublicRoute>
        }
      />
      <Route
        path="/register"
        element={
          <PublicRoute>
            <Register />
          </PublicRoute>
        }
      />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/reset-password" element={<ResetPassword />} />

      {/* Routes with Layout (Header) */}
      <Route element={<Layout />}>
        <Route path="/" element={<Home />} />

        {/* Protected routes */}
        <Route
          path="/settings"
          element={
            <ProtectedRoute>
              <Settings />
            </ProtectedRoute>
          }
        />

        {/* Rifles */}
        <Route
          path="/rifles"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <RiflesList />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/rifles/new"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <RifleCreate />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/rifles/:id"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <RifleDetail />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/rifles/:id/edit"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <RifleEdit />
              </Suspense>
            </ProtectedRoute>
          }
        />

        {/* Ammunition */}
        <Route
          path="/ammunition"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <AmmoList />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/ammunition/new"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <AmmoCreate />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/ammunition/:id"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <AmmoDetail />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/ammunition/:id/edit"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <AmmoEdit />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/ammunition/:id/lots/new"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <LotForm />
              </Suspense>
            </ProtectedRoute>
          }
        />

        {/* Locations */}
        <Route
          path="/locations"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <LocationsList />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/locations/new"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <LocationCreate />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/locations/:id"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <LocationDetail />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/locations/:id/edit"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <LocationEdit />
              </Suspense>
            </ProtectedRoute>
          }
        />

        {/* Sessions */}
        <Route
          path="/sessions"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <SessionsList />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/sessions/new"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <SessionCreate />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/sessions/:id"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <SessionDetail />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/sessions/:id/edit"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <SessionEdit />
              </Suspense>
            </ProtectedRoute>
          }
        />

        {/* Analytics */}
        <Route
          path="/analytics"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <AnalyticsDashboard />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/analytics/dope-chart"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <DopeChart />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/analytics/velocity-trends"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <VelocityTrends />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/analytics/ammo-comparison"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <AmmoComparison />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/analytics/lot-comparison"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <LotComparison />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/analytics/cost-analysis"
          element={
            <ProtectedRoute>
              <Suspense fallback={<PageLoader />}>
                <CostAnalysis />
              </Suspense>
            </ProtectedRoute>
          }
        />

        {/* Admin routes */}
        <Route
          path="/admin"
          element={
            <ProtectedRoute requireAdmin>
              <AdminDashboard />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute requireAdmin>
              <AdminUsers />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/shared-locations"
          element={
            <ProtectedRoute requireAdmin>
              <AdminSharedLocations />
            </ProtectedRoute>
          }
        />
      </Route>
    </Routes>
  );
}

function App() {
  return (
    <Router>
      <ToastProvider>
        <AppContent />
      </ToastProvider>
    </Router>
  );
}

export default App;

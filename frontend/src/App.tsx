import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { useAuthInit } from './hooks/useAuth';
import { Layout } from './components/layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { PublicRoute } from './components/PublicRoute';
import Home from './pages/Home';
import { Login, Register, ForgotPassword, ResetPassword } from './pages/auth';
import { Settings } from './pages/settings';
import { AdminUsers } from './pages/admin';

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

        {/* Admin routes */}
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute requireAdmin>
              <AdminUsers />
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
      <AppContent />
    </Router>
  );
}

export default App;

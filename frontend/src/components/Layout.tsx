import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-950 flex flex-col">
      <nav className="bg-gray-900 border-b border-gray-800 px-6 py-4">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <Link to="/" className="text-xl font-bold text-white tracking-tight">
            🏈 Survivor Pool
          </Link>
          <div className="flex items-center gap-6">
            <Link to="/leagues" className="text-gray-400 hover:text-white text-sm transition-colors">
              Leagues
            </Link>
            {user ? (
              <>
                <Link to="/dashboard" className="text-gray-400 hover:text-white text-sm transition-colors">
                  Dashboard
                </Link>
                {user.role === 'SuperAdmin' && (
                  <Link to="/admin" className="text-yellow-400 hover:text-yellow-300 text-sm transition-colors">
                    Admin
                  </Link>
                )}
                <span className="text-gray-500 text-sm">{user.username}</span>
                <button
                  onClick={handleLogout}
                  className="bg-gray-800 hover:bg-gray-700 text-gray-300 text-sm px-3 py-1.5 rounded-md transition-colors"
                >
                  Logout
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-gray-400 hover:text-white text-sm transition-colors">
                  Login
                </Link>
                <Link
                  to="/register"
                  className="bg-green-600 hover:bg-green-500 text-white text-sm px-4 py-1.5 rounded-md transition-colors"
                >
                  Sign Up
                </Link>
              </>
            )}
          </div>
        </div>
      </nav>
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  );
}

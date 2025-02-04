import React from 'react';
import LoginForm from './components/LoginForm';
import AppointmentCalendar from './components/AppointmentCalendar';
import useAuthStore from './store/store';

const App: React.FC = () => {
  const { accessToken, resetTokens } = useAuthStore();

  const handleLogout = () => {
    resetTokens();
  }

  return (
    <div className="container-fluid">
      {!accessToken ? (
        <LoginForm />
      ) : (
        <>
          <nav className="navbar navbar-expand-lg navbar-light bg-light mb-4">
            <div className="container-fluid">
              <span className="navbar-brand">Booksy Dashboard</span>
              <button className="btn btn-outline-danger ms-auto" onClick={handleLogout}>
                Logout
              </button>
            </div>
          </nav>
          <AppointmentCalendar />
        </>
      )}
    </div>
  );
};

export default App;
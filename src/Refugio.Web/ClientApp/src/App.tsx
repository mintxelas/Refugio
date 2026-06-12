import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider, RequireAuth, RequireManager } from './auth/AuthContext';
import { Layout } from './components/Layout';

// Pages
import { Login } from './pages/Login';
import { Home } from './pages/Home';
import { Dogs } from './pages/Dogs';
import { DogDetail } from './pages/DogDetail';
import { DogCheckin } from './pages/DogCheckin';
import { DogEdit } from './pages/DogEdit';
import { MedicalRecordEdit } from './pages/MedicalRecordEdit';
import { MedicationEdit } from './pages/MedicationEdit';
import { Health } from './pages/Health';
import { Adoptions } from './pages/Adoptions';
import { AdoptionEdit } from './pages/AdoptionEdit';
import { Calendar } from './pages/Calendar';
import { EventEdit } from './pages/EventEdit';
import { Funds } from './pages/Funds';
import { DonationEdit } from './pages/DonationEdit';
import { ExpenseEdit } from './pages/ExpenseEdit';
import { GoalEdit } from './pages/GoalEdit';
import { Volunteers } from './pages/Volunteers';
import { VolunteerEdit } from './pages/VolunteerEdit';
import { Reports } from './pages/Reports';
import { AdminDeleted } from './pages/AdminDeleted';
import { Settings } from './pages/Settings';
import { ChangePassword } from './pages/ChangePassword';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public */}
          <Route path="/login" element={<Login />} />

          {/* Authenticated */}
          <Route element={<RequireAuth><Layout /></RequireAuth>}>
            <Route path="/" element={<Home />} />
            <Route path="/dogs" element={<Dogs />} />
            <Route path="/dogs/new" element={<DogCheckin />} />
            <Route path="/dogs/:id" element={<DogDetail />} />
            <Route path="/dogs/:id/edit" element={<DogEdit />} />
            <Route path="/dogs/:dogId/medical/:id" element={<MedicalRecordEdit />} />
            <Route path="/dogs/:dogId/medications/:id" element={<MedicationEdit />} />
            <Route path="/health" element={<Health />} />
            <Route path="/adoptions" element={<Adoptions />} />
            <Route path="/adoptions/:id" element={<AdoptionEdit />} />
            <Route path="/calendar" element={<Calendar />} />
            <Route path="/calendar/events/:id" element={<EventEdit />} />
            <Route path="/funds" element={<Funds />} />
            <Route path="/funds/donations/:id" element={<DonationEdit />} />
            <Route path="/funds/expenses/:id" element={<ExpenseEdit />} />
            <Route path="/funds/goals/:id" element={<GoalEdit />} />
            <Route path="/volunteers" element={<Volunteers />} />
            <Route path="/volunteers/:id" element={<VolunteerEdit />} />
            <Route path="/reports" element={<Reports />} />
            <Route path="/change-password" element={<ChangePassword />} />

            {/* Manager-only */}
            <Route path="/admin/deleted" element={<RequireManager><AdminDeleted /></RequireManager>} />
            <Route path="/settings" element={<RequireManager><Settings /></RequireManager>} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

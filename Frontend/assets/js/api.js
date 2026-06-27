const API_BASE = 'http://localhost:5181/api';

const Api = {
  getToken() { return localStorage.getItem('clinic_token'); },
  getUser() { const u = localStorage.getItem('clinic_user'); return u ? JSON.parse(u) : null; },
  isLoggedIn() { return !!this.getToken(); },
  getRole() { const u = this.getUser(); return u ? u.role : null; },

  saveAuth(token, user, refreshToken) {
    localStorage.setItem('clinic_token', token);
    localStorage.setItem('clinic_user', JSON.stringify(user));
    if (refreshToken) localStorage.setItem('clinic_refresh_token', refreshToken);
  },

  clearAuth() {
    localStorage.removeItem('clinic_token');
    localStorage.removeItem('clinic_user');
    localStorage.removeItem('clinic_refresh_token');
  },

  logout() { this.clearAuth(); window.location.href = 'login.html'; },

  requireAuth(role) {
    if (!this.isLoggedIn()) { window.location.href = 'login.html'; return false; }
    if (role && this.getRole() !== role) { window.location.href = 'index.html'; return false; }
    return true;
  },

  async request(method, path, body) {
    const headers = { 'Content-Type': 'application/json' };
    const token = this.getToken();
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const opts = { method, headers };
    if (body) opts.body = JSON.stringify(body);

    const res = await fetch(`${API_BASE}${path}`, opts);
    if (res.status === 401) { this.logout(); return null; }
    const data = await res.json();
    if (!res.ok) throw new Error(data.message || 'Request failed');
    return data;
  },

  get(path) { return this.request('GET', path); },
  post(path, body) { return this.request('POST', path, body); },
  put(path, body) { return this.request('PUT', path, body); },
  delete(path) { return this.request('DELETE', path); },

  login(email, password) { return this.post('/auth/login', { email, password }); },
  register(data) { return this.post('/auth/register', data); },
  getMe() { return this.get('/auth/me'); },
  changePassword(oldPassword, newPassword) { return this.put('/auth/change-password', { oldPassword, newPassword }); },

  getDoctors() { return this.get('/doctors'); },
  getDoctorsFiltered(filters) { const q = filters ? '?' + new URLSearchParams(filters).toString() : ''; return this.get(`/doctors${q}`); },
  getDoctor(id) { return this.get(`/doctors/${id}`); },
  getDoctorClinics(id) { return this.get(`/doctors/${id}/clinics`); },
  getSpecializations() { return this.get('/specializations'); },
  getClinics() { return this.get('/clinics'); },

  adminDashboard() { return this.get('/admin/dashboard'); },
  adminGetDoctors() { return this.get('/admin/doctors'); },
  adminCreateDoctor(data) { return this.post('/admin/doctors', data); },
  adminUpdateDoctor(id, data) { return this.put(`/admin/doctors/${id}`, data); },
  adminDeleteDoctor(id) { return this.delete(`/admin/doctors/${id}`); },
  adminGetPatients() { return this.get('/admin/patients'); },
  adminCreatePatient(data) { return this.post('/admin/patients', data); },
  adminUpdatePatient(id, data) { return this.put(`/admin/patients/${id}`, data); },
  adminDeletePatient(id) { return this.delete(`/admin/patients/${id}`); },
  adminGetAppointments(filters) { const q = filters ? '?' + new URLSearchParams(filters).toString() : ''; return this.get(`/admin/appointments${q}`); },
  adminGetClinics() { return this.get('/admin/clinics'); },
  adminCreateClinic(data) { return this.post('/admin/clinics', data); },
  adminUpdateClinic(id, data) { return this.put(`/admin/clinics/${id}`, data); },
  adminDeleteClinic(id) { return this.delete(`/admin/clinics/${id}`); },
  adminGetSpecializations() { return this.get('/admin/specializations'); },
  adminCreateSpecialization(data) { return this.post('/admin/specializations', data); },
  adminUpdateSpecialization(id, data) { return this.put(`/admin/specializations/${id}`, data); },
  adminDeleteSpecialization(id) { return this.delete(`/admin/specializations/${id}`); },
  adminGetCertificates() { return this.get('/admin/certificates'); },
  adminCreateCertificate(data) { return this.post('/admin/certificates', data); },
  adminDeleteCertificate(id) { return this.delete(`/admin/certificates/${id}`); },
  adminAssignDoctorClinic(doctorId, clinicId) { return this.post(`/admin/doctors/${doctorId}/clinics`, { clinicId }); },
  adminRemoveDoctorClinic(doctorId, clinicId) { return this.delete(`/admin/doctors/${doctorId}/clinics/${clinicId}`); },
  adminAssignDoctorCert(doctorId, certId) { return this.post(`/admin/doctors/${doctorId}/certificates`, { certificateId: certId }); },
  adminRemoveDoctorCert(doctorId, certId) { return this.delete(`/admin/doctors/${doctorId}/certificates/${certId}`); },

  doctorProfile() { return this.get('/doctor/profile'); },
  doctorUpdateProfile(data) { return this.put('/doctor/profile', data); },
  doctorUpcoming() { return this.get('/doctor/appointments/upcoming'); },
  doctorCompleted() { return this.get('/doctor/appointments/completed'); },
  doctorCompleteAppt(id) { return this.put(`/doctor/appointments/${id}/complete`); },
  doctorCancelAppt(id) { return this.put(`/doctor/appointments/${id}/cancel`); },
  doctorPatients() { return this.get('/doctor/patients'); },
  doctorPatientHistory(id) { return this.get(`/doctor/patients/${id}/history`); },
  doctorCreateDiagnosis(data) { return this.post('/doctor/diagnoses', data); },
  doctorUpdateDiagnosis(id, data) { return this.put(`/doctor/diagnoses/${id}`, data); },
  doctorCertificates() { return this.get('/doctor/certificates'); },
  doctorClinics() { return this.get('/doctor/clinics'); },

  patientProfile() { return this.get('/patient/profile'); },
  patientUpdateProfile(data) { return this.put('/patient/profile', data); },
  patientAppointments(status) { const q = status ? `?status=${status}` : ''; return this.get(`/patient/appointments${q}`); },
  patientCancelAppt(id) { return this.put(`/patient/appointments/${id}/cancel`); },
  patientDiagnoses() { return this.get('/patient/diagnoses'); },
  patientHistory() { return this.get('/patient/history'); },

  bookAppointment(data) { return this.post('/appointments/book', data); },
};

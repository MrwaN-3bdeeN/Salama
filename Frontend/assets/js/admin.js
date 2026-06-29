let currentSection = 'dashboard';

async function initAdmin() {
  if (!Api.requireAuth('Admin')) return;

  const user = Api.getUser();
  const nameEl = document.getElementById('userName');
  if (nameEl && user) nameEl.textContent = user.name || user.email;

  try {
    const profile = await Api.getMe();
    if (profile.profilePicturePath) {
      const badge = document.querySelector('.user-badge');
      if (badge) {
        const picUrl = Api.getProfilePictureUrl(profile.profilePicturePath);
        badge.innerHTML = `<img src="${picUrl}" alt="" style="width:28px;height:28px;border-radius:50%;object-fit:cover"> <span id="userName">${escapeHtml(user?.name || user?.email || 'Admin')}</span>`;
      }
    }
  } catch (e) { /* ignore */ }

  document.querySelectorAll('.sidebar-nav a[data-section]').forEach(link => {
    link.addEventListener('click', (e) => {
      e.preventDefault();
      showSection(link.dataset.section);
    });
  });

  showSection('dashboard');
}

function showSection(name) {
  currentSection = name;

  document.querySelectorAll('#dashContent .admin-section').forEach(s => s.classList.add('d-none'));
  const target = document.getElementById('section-' + name);
  if (target) target.classList.remove('d-none');

  document.querySelectorAll('.sidebar-nav a[data-section]').forEach(a => {
    a.classList.toggle('active', a.dataset.section === name);
  });

  const titles = {
    dashboard: 'Dashboard', doctors: 'Manage Doctors', patients: 'Manage Patients',
    appointments: 'Appointments', clinics: 'Clinics', specializations: 'Specializations', certificates: 'Certificates', profile: 'My Profile'
  };
  document.getElementById('pageTitle').textContent = titles[name] || name;

  switch (name) {
    case 'dashboard': loadDashboard(); break;
    case 'doctors': loadDoctors(); break;
    case 'patients': loadPatients(); break;
    case 'appointments': loadAppointments(); break;
    case 'clinics': loadClinics(); break;
    case 'specializations': loadSpecializations(); break;
    case 'certificates': loadCertificates(); break;
    case 'profile': loadProfile(); break;
  }
}

// ─── DASHBOARD ──────────────────────────────────────────────
async function loadDashboard() {
  try {
    const dash = await Api.adminDashboard();
    const c = document.getElementById('dashStats');
    if (c) {
      c.innerHTML = `
        ${statCard('bi-people', 'blue', dash.totalDoctors ?? 0, 'Total Doctors')}
        ${statCard('bi-person', 'green', dash.totalPatients ?? 0, 'Total Patients')}
        ${statCard('bi-calendar-event', 'orange', dash.totalAppointments ?? 0, 'Total Appointments')}
        ${statCard('bi-clock-history', 'cyan', dash.upcomingAppointments ?? 0, 'Upcoming')}
        ${statCard('bi-check-circle', 'purple', dash.completedAppointments ?? 0, 'Completed')}`;
    }
  } catch (err) { showToast(err.message, 'error'); }

  try {
    const appts = await Api.adminGetAppointments();
    const list = Array.isArray(appts) ? appts : [];
    renderRecentAppointments(list.slice(0, 5));
  } catch (err) { showToast(err.message, 'error'); }
}

function statCard(icon, color, value, label) {
  return `<div class="col-xl-3 col-md-6 mb-4">
    <div class="stat-card">
      <div class="stat-icon ${color}"><i class="bi ${icon}"></i></div>
      <div class="stat-info"><h3>${value}</h3><p>${label}</p></div>
    </div></div>`;
}

function renderRecentAppointments(appts) {
  const tbody = document.getElementById('recentAppointmentsBody');
  if (!tbody) return;
  if (!appts.length) { tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">No recent appointments</td></tr>'; return; }
  tbody.innerHTML = appts.map(a => `<tr>
    <td>${a.id}</td>
    <td>${a.date}</td>
    <td>${a.patientName || '—'}</td>
    <td>${a.doctorName || '—'}</td>
    <td>${a.clinicName || '—'}</td>
    <td>${statusBadge(a.appointmentStatus)}</td>
  </tr>`).join('');
}

// ─── DOCTORS ────────────────────────────────────────────────
async function loadDoctors() {
  try {
    const doctors = await Api.adminGetDoctors();
    const list = Array.isArray(doctors) ? doctors : [];
    const tbody = document.getElementById('doctorsTableBody');
    if (!tbody) return;
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">No doctors found</td></tr>'; return; }
    tbody.innerHTML = list.map(d => `<tr>
      <td>${d.id}</td>
      <td>${d.name || '—'}</td>
      <td>${d.email || '—'}</td>
      <td>${d.phone || '—'}</td>
      <td>${d.specializationName || '—'}</td>
      <td>
        <button class="btn btn-sm btn-outline-primary me-1" onclick="editDoctor(${d.id})"><i class="bi bi-pencil"></i></button>
        <button class="btn btn-sm btn-outline-danger" onclick="deleteDoctor(${d.id})"><i class="bi bi-trash"></i></button>
      </td></tr>`).join('');
  } catch (err) { showToast(err.message, 'error'); }
}

async function openAddDoctorModal() {
  const specs = await Api.adminGetSpecializations();
  const specList = Array.isArray(specs) ? specs : [];
  const specOptions = specList.map(s => `<option value="${s.id}">${s.specializationName}</option>`).join('');

  showModal('Add Doctor', `
    <form id="doctorForm">
      <div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="name" required></div>
      <div class="mb-3"><label class="form-label">Email</label><input type="email" class="form-control" name="email" required></div>
      <div class="mb-3"><label class="form-label">Phone</label><input type="number" class="form-control" name="phone" required></div>
      <div class="mb-3"><label class="form-label">Password</label><input type="password" class="form-control" name="password" required></div>
      <div class="mb-3"><label class="form-label">Specialization</label><select class="form-select" name="specializationId"><option value="">None</option>${specOptions}</select></div>
      <div class="mb-3"><label class="form-label">About</label><textarea class="form-control" name="about" rows="2"></textarea></div>
      <div class="mb-3"><label class="form-label">Experience</label><input type="text" class="form-control" name="experience"></div>
    </form>`, async () => {
    const f = document.getElementById('doctorForm');
    await Api.adminCreateDoctor({
      name: f.name.value, email: f.email.value, phone: parseInt(f.phone.value),
      password: f.password.value, specializationId: f.specializationId.value ? parseInt(f.specializationId.value) : undefined,
      about: f.about.value || undefined, experience: f.experience.value || undefined
    });
    showToast('Doctor created', 'success'); hideModal(); loadDoctors();
  });
}

async function editDoctor(id) {
  try {
    const list = await Api.adminGetDoctors();
    const d = (Array.isArray(list) ? list : []).find(x => x.id === id);
    if (!d) return showToast('Doctor not found', 'error');
    const specs = await Api.adminGetSpecializations();
    const specList = Array.isArray(specs) ? specs : [];
    const specOptions = specList.map(s => `<option value="${s.id}" ${d.specializationId === s.id ? 'selected' : ''}>${s.specializationName}</option>`).join('');

    showModal('Edit Doctor', `
      <form id="doctorForm">
        <div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="name" value="${d.name || ''}" required></div>
        <div class="mb-3"><label class="form-label">Email</label><input type="email" class="form-control" name="email" value="${d.email || ''}" required></div>
        <div class="mb-3"><label class="form-label">Phone</label><input type="number" class="form-control" name="phone" value="${d.phone || ''}" required></div>
        <div class="mb-3"><label class="form-label">Specialization</label><select class="form-select" name="specializationId"><option value="">None</option>${specOptions}</select></div>
        <div class="mb-3"><label class="form-label">About</label><textarea class="form-control" name="about" rows="2">${d.about || ''}</textarea></div>
        <div class="mb-3"><label class="form-label">Experience</label><input type="text" class="form-control" name="experience" value="${d.experience || ''}"></div>
      </form>`, async () => {
      const f = document.getElementById('doctorForm');
      await Api.adminUpdateDoctor(id, {
        name: f.name.value, email: f.email.value, phone: parseInt(f.phone.value),
        specializationId: f.specializationId.value ? parseInt(f.specializationId.value) : undefined,
        about: f.about.value || undefined, experience: f.experience.value || undefined
      });
      showToast('Doctor updated', 'success'); hideModal(); loadDoctors();
    });
  } catch (err) { showToast(err.message, 'error'); }
}

async function deleteDoctor(id) {
  if (!confirm('Delete this doctor?')) return;
  try { await Api.adminDeleteDoctor(id); showToast('Doctor deleted', 'success'); loadDoctors(); }
  catch (err) { showToast(err.message, 'error'); }
}

// ─── PATIENTS ───────────────────────────────────────────────
async function loadPatients() {
  try {
    const patients = await Api.adminGetPatients();
    const list = Array.isArray(patients) ? patients : [];
    const tbody = document.getElementById('patientsTableBody');
    if (!tbody) return;
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No patients found</td></tr>'; return; }
    tbody.innerHTML = list.map(p => `<tr>
      <td>${p.id}</td>
      <td>${p.name || '—'}</td>
      <td>${p.email || '—'}</td>
      <td>${p.phone || '—'}</td>
      <td>
        <button class="btn btn-sm btn-outline-primary me-1" onclick="editPatient(${p.id})"><i class="bi bi-pencil"></i></button>
        <button class="btn btn-sm btn-outline-danger" onclick="deletePatient(${p.id})"><i class="bi bi-trash"></i></button>
      </td></tr>`).join('');
  } catch (err) { showToast(err.message, 'error'); }
}

function openAddPatientModal() {
  showModal('Add Patient', `
    <form id="patientForm">
      <div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="name" required></div>
      <div class="mb-3"><label class="form-label">Email</label><input type="email" class="form-control" name="email" required></div>
      <div class="mb-3"><label class="form-label">Phone</label><input type="number" class="form-control" name="phone" required></div>
      <div class="mb-3"><label class="form-label">Password</label><input type="password" class="form-control" name="password" required></div>
      <div class="mb-3"><label class="form-label">Address</label><input type="text" class="form-control" name="address"></div>
    </form>`, async () => {
    const f = document.getElementById('patientForm');
    await Api.adminCreatePatient({
      name: f.name.value, email: f.email.value, phone: parseInt(f.phone.value),
      password: f.password.value, address: f.address.value || undefined
    });
    showToast('Patient created', 'success'); hideModal(); loadPatients();
  });
}

async function editPatient(id) {
  try {
    const list = await Api.adminGetPatients();
    const p = (Array.isArray(list) ? list : []).find(x => x.id === id);
    if (!p) return showToast('Patient not found', 'error');
    showModal('Edit Patient', `
      <form id="patientForm">
        <div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="name" value="${p.name || ''}" required></div>
        <div class="mb-3"><label class="form-label">Email</label><input type="email" class="form-control" name="email" value="${p.email || ''}" required></div>
        <div class="mb-3"><label class="form-label">Phone</label><input type="number" class="form-control" name="phone" value="${p.phone || ''}" required></div>
        <div class="mb-3"><label class="form-label">Address</label><input type="text" class="form-control" name="address" value="${p.address || ''}"></div>
      </form>`, async () => {
      const f = document.getElementById('patientForm');
      await Api.adminUpdatePatient(id, {
        name: f.name.value, email: f.email.value, phone: parseInt(f.phone.value), address: f.address.value || undefined
      });
      showToast('Patient updated', 'success'); hideModal(); loadPatients();
    });
  } catch (err) { showToast(err.message, 'error'); }
}

async function deletePatient(id) {
  if (!confirm('Delete this patient?')) return;
  try { await Api.adminDeletePatient(id); showToast('Patient deleted', 'success'); loadPatients(); }
  catch (err) { showToast(err.message, 'error'); }
}

// ─── APPOINTMENTS ───────────────────────────────────────────
async function loadAppointments(filters) {
  try {
    const appts = await Api.adminGetAppointments(filters);
    const list = Array.isArray(appts) ? appts : [];
    const tbody = document.getElementById('appointmentsTableBody');
    if (!tbody) return;
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">No appointments found</td></tr>'; return; }
    tbody.innerHTML = list.map(a => `<tr>
      <td>${a.id}</td>
      <td>${a.date}</td>
      <td>${a.patientName || '—'}</td>
      <td>${a.doctorName || '—'}</td>
      <td>${a.clinicName || '—'}</td>
      <td>${statusBadge(a.appointmentStatus)}</td>
    </tr>`).join('');
  } catch (err) { showToast(err.message, 'error'); }
}

function filterAppointments() {
  const status = document.getElementById('filterStatus')?.value;
  const date = document.getElementById('filterDate')?.value;
  const filters = {};
  if (status) filters.status = status;
  if (date) filters.date = date;
  loadAppointments(Object.keys(filters).length ? filters : undefined);
}

// ─── CLINICS ────────────────────────────────────────────────
async function loadClinics() {
  try {
    const clinics = await Api.adminGetClinics();
    const list = Array.isArray(clinics) ? clinics : [];
    const tbody = document.getElementById('clinicsTableBody');
    if (!tbody) return;
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">No clinics found</td></tr>'; return; }
    tbody.innerHTML = list.map(c => `<tr>
      <td>${c.id}</td>
      <td>${c.clinicName || '—'}</td>
      <td>${c.address || '—'}</td>
      <td>${c.phone || '—'}</td>
      <td>${c.specializationName || '—'}</td>
      <td>
        <button class="btn btn-sm btn-outline-primary me-1" onclick="editClinic(${c.id})"><i class="bi bi-pencil"></i></button>
        <button class="btn btn-sm btn-outline-danger" onclick="deleteClinic(${c.id})"><i class="bi bi-trash"></i></button>
      </td></tr>`).join('');
  } catch (err) { showToast(err.message, 'error'); }
}

async function openAddClinicModal() {
  const specs = await Api.adminGetSpecializations();
  const specList = Array.isArray(specs) ? specs : [];
  const specOptions = specList.map(s => `<option value="${s.id}">${s.specializationName}</option>`).join('');

  showModal('Add Clinic', `
    <form id="clinicForm">
      <div class="mb-3"><label class="form-label">Clinic Name</label><input type="text" class="form-control" name="clinicName" required></div>
      <div class="mb-3"><label class="form-label">Address</label><input type="text" class="form-control" name="address"></div>
      <div class="mb-3"><label class="form-label">Phone</label><input type="text" class="form-control" name="phone"></div>
      <div class="mb-3"><label class="form-label">Specialization</label><select class="form-select" name="specializationId"><option value="">None</option>${specOptions}</select></div>
    </form>`, async () => {
    const f = document.getElementById('clinicForm');
    await Api.adminCreateClinic({
      clinicName: f.clinicName.value, address: f.address.value || undefined,
      phone: f.phone.value || undefined, specializationId: f.specializationId.value ? parseInt(f.specializationId.value) : undefined
    });
    showToast('Clinic created', 'success'); hideModal(); loadClinics();
  });
}

async function editClinic(id) {
  try {
    const list = await Api.adminGetClinics();
    const c = (Array.isArray(list) ? list : []).find(x => x.id === id);
    if (!c) return showToast('Clinic not found', 'error');
    const specs = await Api.adminGetSpecializations();
    const specList = Array.isArray(specs) ? specs : [];
    const specOptions = specList.map(s => `<option value="${s.id}" ${c.specializationId === s.id ? 'selected' : ''}>${s.specializationName}</option>`).join('');

    showModal('Edit Clinic', `
      <form id="clinicForm">
        <div class="mb-3"><label class="form-label">Clinic Name</label><input type="text" class="form-control" name="clinicName" value="${c.clinicName || ''}" required></div>
        <div class="mb-3"><label class="form-label">Address</label><input type="text" class="form-control" name="address" value="${c.address || ''}"></div>
        <div class="mb-3"><label class="form-label">Phone</label><input type="text" class="form-control" name="phone" value="${c.phone || ''}"></div>
        <div class="mb-3"><label class="form-label">Specialization</label><select class="form-select" name="specializationId"><option value="">None</option>${specOptions}</select></div>
      </form>`, async () => {
      const f = document.getElementById('clinicForm');
      await Api.adminUpdateClinic(id, {
        clinicName: f.clinicName.value, address: f.address.value || undefined,
        phone: f.phone.value || undefined, specializationId: f.specializationId.value ? parseInt(f.specializationId.value) : undefined
      });
      showToast('Clinic updated', 'success'); hideModal(); loadClinics();
    });
  } catch (err) { showToast(err.message, 'error'); }
}

async function deleteClinic(id) {
  if (!confirm('Delete this clinic?')) return;
  try { await Api.adminDeleteClinic(id); showToast('Clinic deleted', 'success'); loadClinics(); }
  catch (err) { showToast(err.message, 'error'); }
}

// ─── SPECIALIZATIONS ────────────────────────────────────────
async function loadSpecializations() {
  try {
    const specs = await Api.adminGetSpecializations();
    const list = Array.isArray(specs) ? specs : [];
    const tbody = document.getElementById('specializationsTableBody');
    if (!tbody) return;
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="3" class="text-center text-muted">No specializations found</td></tr>'; return; }
    tbody.innerHTML = list.map(s => `<tr>
      <td>${s.id}</td>
      <td>${s.specializationName || '—'}</td>
      <td>
        <button class="btn btn-sm btn-outline-primary me-1" onclick="editSpecialization(${s.id})"><i class="bi bi-pencil"></i></button>
        <button class="btn btn-sm btn-outline-danger" onclick="deleteSpecialization(${s.id})"><i class="bi bi-trash"></i></button>
      </td></tr>`).join('');
  } catch (err) { showToast(err.message, 'error'); }
}

function openAddSpecializationModal() {
  showModal('Add Specialization', `
    <form id="specForm"><div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="specializationName" required></div></form>`,
    async () => {
      const f = document.getElementById('specForm');
      await Api.adminCreateSpecialization({ specializationName: f.specializationName.value });
      showToast('Specialization created', 'success'); hideModal(); loadSpecializations();
    });
}

async function editSpecialization(id) {
  try {
    const list = await Api.adminGetSpecializations();
    const s = (Array.isArray(list) ? list : []).find(x => x.id === id);
    if (!s) return showToast('Specialization not found', 'error');
    showModal('Edit Specialization', `
      <form id="specForm"><div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="specializationName" value="${s.specializationName || ''}" required></div></form>`,
      async () => {
        const f = document.getElementById('specForm');
        await Api.adminUpdateSpecialization(id, { specializationName: f.specializationName.value });
        showToast('Specialization updated', 'success'); hideModal(); loadSpecializations();
      });
  } catch (err) { showToast(err.message, 'error'); }
}

async function deleteSpecialization(id) {
  if (!confirm('Delete this specialization?')) return;
  try { await Api.adminDeleteSpecialization(id); showToast('Specialization deleted', 'success'); loadSpecializations(); }
  catch (err) { showToast(err.message, 'error'); }
}

// ─── CERTIFICATES ───────────────────────────────────────────
async function loadCertificates() {
  try {
    const certs = await Api.adminGetCertificates();
    const list = Array.isArray(certs) ? certs : [];
    const tbody = document.getElementById('certificatesTableBody');
    if (!tbody) return;
    if (!list.length) { tbody.innerHTML = '<tr><td colspan="3" class="text-center text-muted">No certificates found</td></tr>'; return; }
    tbody.innerHTML = list.map(c => `<tr>
      <td>${c.id}</td>
      <td>${c.certificateName || '—'}</td>
      <td>
        <button class="btn btn-sm btn-outline-danger" onclick="deleteCertificate(${c.id})"><i class="bi bi-trash"></i></button>
      </td></tr>`).join('');
  } catch (err) { showToast(err.message, 'error'); }
}

function openAddCertificateModal() {
  showModal('Add Certificate', `
    <form id="certForm"><div class="mb-3"><label class="form-label">Name</label><input type="text" class="form-control" name="certificateName" required></div></form>`,
    async () => {
      const f = document.getElementById('certForm');
      await Api.adminCreateCertificate({ certificateName: f.certificateName.value });
      showToast('Certificate created', 'success'); hideModal(); loadCertificates();
    });
}

async function deleteCertificate(id) {
  if (!confirm('Delete this certificate?')) return;
  try { await Api.adminDeleteCertificate(id); showToast('Certificate deleted', 'success'); loadCertificates(); }
  catch (err) { showToast(err.message, 'error'); }
}

// ─── HELPERS ──────────────────────────────────────────────
function escapeHtml(str) {
  if (!str) return '';
  const div = document.createElement('div');
  div.textContent = String(str);
  return div.innerHTML;
}

function escapeAttr(str) {
  if (!str) return '';
  return String(str).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/'/g, '&#39;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// ─── MODAL HELPERS ──────────────────────────────────────────
let activeModal = null;

function showModal(title, bodyHtml, onSave) {
  hideModal();
  const backdrop = document.createElement('div');
  backdrop.className = 'modal-backdrop fade show';
  document.body.appendChild(backdrop);

  const modal = document.createElement('div');
  modal.className = 'modal fade show d-block';
  modal.tabIndex = -1;
  modal.innerHTML = `<div class="modal-dialog modal-dialog-centered"><div class="modal-content">
    <div class="modal-header"><h5 class="modal-title">${title}</h5><button type="button" class="btn-close" data-dismiss="modal"></button></div>
    <div class="modal-body">${bodyHtml}</div>
    <div class="modal-footer">
      <button type="button" class="btn btn-secondary" id="modalCancelBtn">Cancel</button>
      <button type="button" class="btn btn-primary" id="modalSaveBtn">Save</button>
    </div></div></div>`;
  document.body.appendChild(modal);
  activeModal = modal;

  modal.querySelector('.btn-close').addEventListener('click', hideModal);
  modal.querySelector('#modalCancelBtn').addEventListener('click', hideModal);
  backdrop.addEventListener('click', hideModal);
  modal.querySelector('#modalSaveBtn').addEventListener('click', async () => {
    const btn = modal.querySelector('#modalSaveBtn');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';
    try { await onSave(); } catch (err) { showToast(err.message, 'error'); btn.disabled = false; btn.textContent = 'Save'; }
  });
}

function hideModal() {
  if (activeModal) { activeModal.remove(); activeModal = null; }
  const backdrop = document.querySelector('.modal-backdrop');
  if (backdrop) backdrop.remove();
  document.body.classList.remove('modal-open');
}

function showToast(message, type = 'success') {
  let container = document.getElementById('toastContainer');
  if (!container) {
    container = document.createElement('div');
    container.id = 'toastContainer';
    container.style.cssText = 'position:fixed;top:20px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:10px;';
    document.body.appendChild(container);
  }
  const bgMap = { success: 'bg-success', error: 'bg-danger', warning: 'bg-warning text-dark', info: 'bg-info text-dark' };
  const iconMap = { success: 'bi-check-circle-fill', error: 'bi-exclamation-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
  const toast = document.createElement('div');
  toast.className = 'toast show align-items-center text-white border-0';
  toast.style.cssText = 'min-width:300px;box-shadow:0 4px 12px rgba(0,0,0,.15);';
  toast.innerHTML = `<div class="d-flex"><div class="toast-body ${bgMap[type] || 'bg-secondary'}"><i class="bi ${iconMap[type] || 'bi-info-circle'} me-2"></i>${message}</div>
    <button type="button" class="btn-close btn-close-white me-2 m-auto"></button></div>`;
  toast.querySelector('.btn-close').addEventListener('click', () => toast.remove());
  container.appendChild(toast);
  setTimeout(() => { if (toast.parentElement) toast.remove(); }, 4000);
}

function statusBadge(status) {
  if (!status) return '<span class="badge bg-secondary">—</span>';
  const map = { Scheduled: 'bg-primary', Upcoming: 'bg-info', Completed: 'bg-success', Cancelled: 'bg-danger' };
  return `<span class="badge ${map[status] || 'bg-secondary'}">${status}</span>`;
}

// ─── PROFILE ───────────────────────────────────────────────
async function loadProfile() {
  const area = document.getElementById('section-profile');
  try {
    const data = await Api.getMe();
    const picUrl = Api.getProfilePictureUrl(data.profilePicturePath);

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom"><h5><i class="bi bi-person-circle me-2"></i>My Profile</h5></div>
        <div class="card-body-custom">
          <div id="profileMsg"></div>
          <div class="d-flex align-items-center gap-4 mb-4">
            <div id="profilePicWrapper" style="position:relative;width:100px;height:100px;flex-shrink:0">
              ${picUrl
                ? `<img id="profilePic" src="${picUrl}" alt="Profile" style="width:100px;height:100px;border-radius:50%;object-fit:cover;border:3px solid #dee2e6">`
                : `<div id="profilePic" style="width:100px;height:100px;border-radius:50%;background:#e9ecef;display:flex;align-items:center;justify-content:center;border:3px solid #dee2e6"><i class="bi bi-person" style="font-size:2.5rem;color:#adb5bd"></i></div>`
              }
              <label for="profilePicInput" style="position:absolute;bottom:0;right:0;width:32px;height:32px;border-radius:50%;background:#0d6efd;color:#fff;display:flex;align-items:center;justify-content:center;cursor:pointer;border:2px solid #fff;font-size:.9rem"><i class="bi bi-camera"></i></label>
              <input type="file" id="profilePicInput" accept="image/jpeg,image/png,image/webp" style="display:none">
            </div>
            <div>
              <h5 class="mb-1">${escapeHtml(data.name || '')}</h5>
              <p class="text-muted mb-0">${escapeHtml(data.email || '')}</p>
              <small class="text-muted">Click the camera icon to upload a profile picture</small>
            </div>
          </div>
          <form id="profileForm">
            <div class="row g-3">
              <div class="col-md-6"><label class="form-label">Full Name</label><input type="text" class="form-control" id="profileName" value="${escapeAttr(data.name || '')}" required></div>
              <div class="col-md-6"><label class="form-label">Email</label><input type="email" class="form-control" value="${escapeAttr(data.email || '')}" disabled></div>
              <div class="col-md-6"><label class="form-label">Phone</label><input type="text" class="form-control" id="profilePhone" value="${escapeAttr(String(data.phone || ''))}"></div>
            </div>
            <div class="mt-4"><button type="submit" class="btn btn-primary"><i class="bi bi-check-lg me-1"></i>Save Changes</button></div>
          </form>
          <hr class="my-4">
          <h6 class="mb-3"><i class="bi bi-key me-2"></i>Change Password</h6>
          <div id="pwdStep1">
            <p class="text-muted small mb-3">Enter your current password to proceed.</p>
            <form id="verifyPwdForm" class="d-flex gap-2 align-items-end">
              <div class="flex-grow-1"><label class="form-label">Current Password</label><input type="password" class="form-control" id="oldPassword" required></div>
              <div><button type="submit" class="btn btn-outline-primary"><i class="bi bi-check-lg me-1"></i>Verify</button></div>
            </form>
          </div>
          <div id="pwdStep2" style="display:none">
            <p class="text-success small mb-3"><i class="bi bi-check-circle me-1"></i>Password verified. Enter your new password.</p>
            <form id="changePwdForm">
              <div class="row g-3">
                <div class="col-md-6"><label class="form-label">New Password</label><input type="password" class="form-control" id="newPassword" required minlength="6"></div>
                <div class="col-md-6"><label class="form-label">Confirm New Password</label><input type="password" class="form-control" id="confirmPassword" required minlength="6"></div>
              </div>
              <div class="mt-3"><button type="submit" class="btn btn-primary"><i class="bi bi-shield-lock me-1"></i>Update Password</button></div>
            </form>
          </div>
        </div>
      </div>`;

    document.getElementById('profilePicInput').addEventListener('change', async (e) => {
      const file = e.target.files[0];
      if (!file) return;
      const msgEl = document.getElementById('profileMsg');
      if (file.size > 5 * 1024 * 1024) { msgEl.innerHTML = '<div class="alert alert-danger">File must be under 5MB.</div>'; return; }
      try {
        msgEl.innerHTML = '<div class="alert alert-info">Uploading...</div>';
        const result = await Api.uploadProfilePicture(file);
        const wrapper = document.getElementById('profilePicWrapper');
        const newImg = document.createElement('img');
        newImg.id = 'profilePic';
        newImg.src = Api.getProfilePictureUrl(result.fileName);
        newImg.alt = 'Profile';
        newImg.style.cssText = 'width:100px;height:100px;border-radius:50%;object-fit:cover;border:3px solid #dee2e6';
        const old = document.getElementById('profilePic');
        wrapper.replaceChild(newImg, old);
        msgEl.innerHTML = '<div class="alert alert-success">Profile picture updated!</div>';
      } catch (err) { msgEl.innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`; }
    });

    document.getElementById('profileForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      const msgEl = document.getElementById('profileMsg');
      try {
        const body = {
          name: document.getElementById('profileName').value.trim(),
          phone: parseInt(document.getElementById('profilePhone').value) || undefined
        };
        await Api.put('/admin/profile', body);
        msgEl.innerHTML = '<div class="alert alert-success">Profile updated successfully.</div>';
        const user = Api.getUser();
        if (user) { user.name = body.name; localStorage.setItem('clinic_user', JSON.stringify(user)); document.getElementById('userName').textContent = body.name; }
      } catch (err) { msgEl.innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`; }
    });

    document.getElementById('verifyPwdForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      const msgEl = document.getElementById('profileMsg');
      const oldPwd = document.getElementById('oldPassword').value;
      try {
        await Api.verifyPassword(oldPwd);
        document.getElementById('pwdStep1').style.display = 'none';
        document.getElementById('pwdStep2').style.display = 'block';
        msgEl.innerHTML = '';
      } catch (err) {
        msgEl.innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`;
      }
    });

    document.getElementById('changePwdForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      const msgEl = document.getElementById('profileMsg');
      const newPwd = document.getElementById('newPassword').value;
      const confirmPwd = document.getElementById('confirmPassword').value;

      if (newPwd !== confirmPwd) {
        msgEl.innerHTML = '<div class="alert alert-danger">New passwords do not match.</div>';
        return;
      }
      if (newPwd.length < 6) {
        msgEl.innerHTML = '<div class="alert alert-danger">New password must be at least 6 characters.</div>';
        return;
      }

      try {
        await Api.changePassword(document.getElementById('oldPassword').value, newPwd);
        msgEl.innerHTML = '<div class="alert alert-success">Password changed successfully.</div>';
        document.getElementById('pwdStep1').style.display = 'block';
        document.getElementById('pwdStep2').style.display = 'none';
        document.getElementById('verifyPwdForm').reset();
        document.getElementById('changePwdForm').reset();
      } catch (err) {
        msgEl.innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`;
      }
    });
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

let currentSection = 'dashboard';

async function initPatient() {
  if (!Api.requireAuth('Patient')) return;
  const user = Api.getUser();
  if (user) {
    document.getElementById('userName').textContent = user.name || user.email || 'Patient';
  }

  try {
    const profile = await Api.patientProfile();
    if (profile.profilePicturePath) {
      const badge = document.querySelector('.user-badge');
      if (badge) {
        const picUrl = Api.getProfilePictureUrl(profile.profilePicturePath);
        badge.innerHTML = `<img src="${picUrl}" alt="" style="width:28px;height:28px;border-radius:50%;object-fit:cover"> <span id="userName">${escapeHtml(user?.name || user?.email || 'Patient')}</span>`;
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

  document.querySelectorAll('.sidebar-nav a[data-section]').forEach(a => a.classList.remove('active'));
  const activeLink = document.querySelector(`.sidebar-nav a[data-section="${name}"]`);
  if (activeLink) activeLink.classList.add('active');

  const titles = {
    dashboard: 'Dashboard',
    book: 'Book Appointment',
    appointments: 'My Appointments',
    diagnoses: 'My Diagnoses',
    history: 'Medical History',
    profile: 'My Profile'
  };
  document.getElementById('pageTitle').textContent = titles[name] || name;

  const content = document.getElementById('contentArea');
  content.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary"></div><p class="mt-2 text-muted">Loading...</p></div>';

  switch (name) {
    case 'dashboard': loadDashboard(); break;
    case 'book': loadBook(); break;
    case 'appointments': loadAppointments(); break;
    case 'diagnoses': loadDiagnoses(); break;
    case 'history': loadHistory(); break;
    case 'profile': loadProfile(); break;
    default: loadDashboard();
  }
}

// ─── DASHBOARD ─────────────────────────────────────────────
async function loadDashboard() {
  try {
    const appointments = await Api.patientAppointments();
    const all = Array.isArray(appointments) ? appointments : [];
    const total = all.length;
    const upcoming = all.filter(a => a.appointmentStatus === 'Scheduled').length;
    const completed = all.filter(a => a.appointmentStatus === 'Completed').length;
    const cancelled = all.filter(a => a.appointmentStatus === 'Cancelled').length;
    const recent = all.slice(0, 5);

    document.getElementById('contentArea').innerHTML = `
      <div class="row g-3 mb-4">
        <div class="col-sm-6 col-xl-3"><div class="stat-card"><div class="stat-icon blue"><i class="bi bi-calendar3"></i></div><div class="stat-info"><h3>${total}</h3><p>Total Appointments</p></div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="stat-card"><div class="stat-icon orange"><i class="bi bi-clock"></i></div><div class="stat-info"><h3>${upcoming}</h3><p>Upcoming</p></div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="stat-card"><div class="stat-icon green"><i class="bi bi-check-circle"></i></div><div class="stat-info"><h3>${completed}</h3><p>Completed</p></div></div></div>
        <div class="col-sm-6 col-xl-3"><div class="stat-card"><div class="stat-icon red"><i class="bi bi-x-circle"></i></div><div class="stat-info"><h3>${cancelled}</h3><p>Cancelled</p></div></div></div>
      </div>
      <div class="dash-card">
        <div class="card-header-custom">
          <h5><i class="bi bi-clock-history me-2"></i>Recent Appointments</h5>
          <div>
            <a href="#" class="btn btn-sm btn-primary me-2" onclick="showSection('book'); return false;"><i class="bi bi-calendar-plus me-1"></i>Book New</a>
            <a href="#" class="btn btn-sm btn-outline-primary" onclick="showSection('appointments'); return false;">View All</a>
          </div>
        </div>
        <div class="card-body-custom">
          ${recent.length > 0 ? `<div class="table-responsive"><table class="table table-hover mb-0"><thead><tr><th>Date</th><th>Doctor</th><th>Clinic</th><th>Status</th><th>Diagnosis</th></tr></thead><tbody>
            ${recent.map(a => `<tr>
              <td>${a.date}</td>
              <td>${escapeHtml(a.doctorName || '-')}</td>
              <td>${escapeHtml(a.clinicName || '-')}</td>
              <td><span class="badge-status badge-${(a.appointmentStatus || '').toLowerCase()}">${escapeHtml(a.appointmentStatus || '-')}</span></td>
              <td>${escapeHtml(a.diagnosis || '-')}</td>
            </tr>`).join('')}
          </tbody></table></div>` : '<div class="empty-state"><i class="bi bi-calendar-x"></i><p>No appointments yet.</p></div>'}
        </div>
      </div>`;
  } catch (err) {
    document.getElementById('contentArea').innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

// ─── BOOK APPOINTMENT ──────────────────────────────────────
let bookSpecs = [];

async function loadBook() {
  const area = document.getElementById('contentArea');
  try {
    const specs = await Api.getSpecializations().catch(() => []);

    bookSpecs = Array.isArray(specs) ? specs : [];

    area.innerHTML = `
      <div class="dash-card mb-4">
        <div class="card-header-custom">
          <h5><i class="bi bi-search me-2"></i>Find a Doctor</h5>
        </div>
        <div class="card-body-custom">
          <div class="row g-3">
            <div class="col-md-4 col-sm-6">
              <label class="form-label fw-semibold">Doctor Name</label>
              <input type="text" class="form-control" id="bookSearchName" placeholder="Search by name...">
            </div>
            <div class="col-md-4 col-sm-6">
              <label class="form-label fw-semibold">Specialization</label>
              <select class="form-select" id="bookSpecFilter">
                <option value="">All Specializations</option>
                ${bookSpecs.map(s => `<option value="${s.id}">${escapeHtml(s.specializationName)}</option>`).join('')}
              </select>
            </div>
            <div class="col-md-4 col-sm-6">
              <label class="form-label fw-semibold">Clinic Name</label>
              <input type="text" class="form-control" id="bookSearchClinic" placeholder="Search by clinic...">
            </div>
            <div class="col-md-4 col-sm-6">
              <label class="form-label fw-semibold">Location</label>
              <input type="text" class="form-control" id="bookSearchLocation" placeholder="Search by location...">
            </div>
            <div class="col-md-4 col-sm-6 d-flex align-items-end gap-2">
              <button class="btn btn-primary flex-grow-1" onclick="fetchBookDoctors()"><i class="bi bi-search me-1"></i>Search</button>
              <button class="btn btn-outline-secondary" onclick="resetBookFilters()"><i class="bi bi-arrow-counterclockwise"></i></button>
            </div>
          </div>
        </div>
      </div>
      <div id="bookDoctorsGrid" class="row g-3">
        <div class="col-12 text-center py-5"><div class="spinner-border text-primary"></div><p class="mt-2 text-muted">Loading doctors...</p></div>
      </div>`;

    ['bookSearchName', 'bookSearchClinic', 'bookSearchLocation'].forEach(id => {
      document.getElementById(id).addEventListener('keydown', (e) => { if (e.key === 'Enter') fetchBookDoctors(); });
    });
    document.getElementById('bookSpecFilter').addEventListener('change', () => fetchBookDoctors());

    fetchBookDoctors();
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

async function fetchBookDoctors() {
  const grid = document.getElementById('bookDoctorsGrid');
  if (!grid) return;
  grid.innerHTML = '<div class="col-12 text-center py-5"><div class="spinner-border text-primary"></div><p class="mt-2 text-muted">Searching...</p></div>';

  const filters = {};
  const specId = document.getElementById('bookSpecFilter')?.value;
  const searchName = document.getElementById('bookSearchName')?.value?.trim();
  const searchClinic = document.getElementById('bookSearchClinic')?.value?.trim();
  const searchLocation = document.getElementById('bookSearchLocation')?.value?.trim();

  if (specId) filters.specializationId = specId;
  if (searchName) filters.search = searchName;
  if (searchClinic) filters.clinicName = searchClinic;
  if (searchLocation) filters.location = searchLocation;

  try {
    const doctors = await Api.getDoctorsFiltered(Object.keys(filters).length ? filters : null);
    const list = Array.isArray(doctors) ? doctors : [];
    renderBookDoctors(list);
  } catch (err) {
    grid.innerHTML = `<div class="col-12 text-center py-5"><i class="bi bi-exclamation-triangle d-block mb-2" style="font-size:2rem;color:#dc3545"></i><p class="text-danger">${escapeHtml(err.message)}</p></div>`;
  }
}

function resetBookFilters() {
  ['bookSearchName', 'bookSearchClinic', 'bookSearchLocation'].forEach(id => {
    const el = document.getElementById(id);
    if (el) el.value = '';
  });
  const specEl = document.getElementById('bookSpecFilter');
  if (specEl) specEl.value = '';
  fetchBookDoctors();
}

function renderBookDoctors(doctors) {
  const grid = document.getElementById('bookDoctorsGrid');
  if (!grid) return;
  if (!doctors.length) {
    grid.innerHTML = '<div class="col-12 text-center py-5"><i class="bi bi-search d-block mb-2" style="font-size:2rem;color:#6c757d"></i><p class="text-muted">No doctors match your search criteria.</p></div>';
    return;
  }

  const images = ['staff-1.webp','staff-2.webp','staff-3.webp','staff-4.webp','staff-5.webp','staff-6.webp','staff-7.webp','staff-8.webp'];
  grid.innerHTML = doctors.map((d, i) => `
    <div class="col-lg-4 col-md-6">
      <div class="dash-card h-100" style="transition:transform .2s;cursor:default">
        <div class="card-body-custom text-center">
          <img src="assets/img/health/${images[i % images.length]}" alt="Dr. ${escapeAttr(d.userName)}" class="rounded-circle mb-3" style="width:80px;height:80px;object-fit:cover">
          <h5 class="mb-1">Dr. ${escapeHtml(d.userName)}</h5>
          <p class="text-primary mb-1">${escapeHtml(d.specializationName || 'General')}</p>
          ${d.address ? `<p class="text-muted small mb-1"><i class="bi bi-geo-alt me-1"></i>${escapeHtml(d.address)}</p>` : ''}
          ${d.clinicName ? `<p class="text-muted small mb-2"><i class="bi bi-building me-1"></i>${escapeHtml(d.clinicName)}</p>` : ''}
          ${d.about ? `<p class="text-muted small mb-3">${escapeHtml(d.about.substring(0, 100))}${d.about.length > 100 ? '...' : ''}</p>` : '<div class="mb-3"></div>'}
          <button class="btn btn-primary btn-sm w-100" onclick="openBookingModal(${d.id}, '${escapeAttr(d.userName)}', '${escapeAttr(d.specializationName || '')}')">
            <i class="bi bi-calendar-plus me-1"></i>Book Appointment
          </button>
        </div>
      </div>
    </div>`).join('');
}

function openBookingModal(doctorId, doctorName, specName) {
  const existing = document.getElementById('bookingModal');
  if (existing) existing.remove();

  const user = Api.getUser();
  const minDate = new Date().toISOString().split('T')[0];

  const modal = document.createElement('div');
  modal.id = 'bookingModal';
  modal.className = 'modal fade show d-block';
  modal.tabIndex = -1;
  modal.innerHTML = `
    <div class="modal-dialog modal-dialog-centered">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title"><i class="bi bi-calendar-plus me-2"></i>Book Appointment</h5>
          <button type="button" class="btn-close" onclick="closeBookingModal()"></button>
        </div>
        <div class="modal-body">
          <div class="mb-3 p-3 rounded" style="background:#f8f9fa">
            <strong>Dr. ${escapeHtml(doctorName)}</strong><br>
            <small class="text-muted">${escapeHtml(specName)}</small>
          </div>
          <div id="bookModalMsg"></div>
          <form id="bookingForm">
            <input type="hidden" name="doctorId" value="${doctorId}">
            <div class="mb-3">
              <label class="form-label fw-semibold">Clinic (optional)</label>
              <select class="form-select" id="bookClinicSelect" name="clinicId">
                <option value="">Select Clinic</option>
              </select>
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Appointment Date</label>
              <input type="date" class="form-control" name="date" min="${minDate}" required>
            </div>
            <div class="mb-3">
              <label class="form-label fw-semibold">Notes / Reason (optional)</label>
              <textarea class="form-control" name="diagnosis" rows="3" placeholder="Describe your symptoms or reason for visit"></textarea>
            </div>
          </form>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" onclick="closeBookingModal()">Cancel</button>
          <button type="button" class="btn btn-primary" id="bookConfirmBtn" onclick="submitBooking(${doctorId}, ${user ? user.id : 0})">
            <i class="bi bi-check-lg me-1"></i>Confirm Booking
          </button>
        </div>
      </div>
    </div>
    <div class="modal-backdrop fade show" onclick="closeBookingModal()"></div>`;

  document.body.appendChild(modal);
  document.body.classList.add('modal-open');

  loadDoctorClinics(doctorId);
}

async function loadDoctorClinics(doctorId) {
  try {
    const clinics = await Api.getDoctorClinics(doctorId);
    const list = Array.isArray(clinics) ? clinics : [];
    const select = document.getElementById('bookClinicSelect');
    if (!select) return;
    list.forEach(c => {
      const opt = document.createElement('option');
      opt.value = c.id;
      opt.textContent = c.clinicName + (c.address ? ` - ${c.address}` : '');
      select.appendChild(opt);
    });
  } catch (e) {
    console.log('Could not load doctor clinics:', e);
  }
}

async function submitBooking(doctorId, patientId) {
  const form = document.getElementById('bookingForm');
  const msgEl = document.getElementById('bookModalMsg');
  const btn = document.getElementById('bookConfirmBtn');

  const date = form.date.value;
  if (!date) {
    msgEl.innerHTML = '<div class="alert alert-danger py-2">Please select a date.</div>';
    return;
  }

  btn.disabled = true;
  btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Booking...';

  try {
    await Api.bookAppointment({
      doctorId: doctorId,
      patientId: patientId,
      clinicId: form.clinicId.value ? parseInt(form.clinicId.value) : null,
      diagnosis: form.diagnosis.value || '',
      appointmentDate: date
    });

    msgEl.innerHTML = '<div class="alert alert-success py-2"><i class="bi bi-check-circle me-1"></i>Appointment booked successfully!</div>';
    btn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Booked!';
    btn.className = 'btn btn-success';

    setTimeout(() => {
      closeBookingModal();
      showToast('Appointment booked successfully!', 'success');
      showSection('appointments');
    }, 1500);
  } catch (err) {
    msgEl.innerHTML = `<div class="alert alert-danger py-2">${escapeHtml(err.message)}</div>`;
    btn.disabled = false;
    btn.innerHTML = '<i class="bi bi-check-lg me-1"></i>Confirm Booking';
  }
}

function closeBookingModal() {
  const modal = document.getElementById('bookingModal');
  if (modal) modal.remove();
  const backdrop = document.querySelector('.modal-backdrop');
  if (backdrop) backdrop.remove();
  document.body.classList.remove('modal-open');
}

// ─── APPOINTMENTS ──────────────────────────────────────────
async function loadAppointments(statusFilter) {
  try {
    const appointments = await Api.patientAppointments(statusFilter);
    const all = Array.isArray(appointments) ? appointments : [];

    document.getElementById('contentArea').innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5><i class="bi bi-calendar-check me-2"></i>All Appointments</h5>
          <select class="form-select form-select-sm" id="statusFilter" style="width:auto">
            <option value="">All Status</option>
            <option value="Scheduled" ${statusFilter === 'Scheduled' ? 'selected' : ''}>Scheduled</option>
            <option value="Completed" ${statusFilter === 'Completed' ? 'selected' : ''}>Completed</option>
            <option value="Cancelled" ${statusFilter === 'Cancelled' ? 'selected' : ''}>Cancelled</option>
          </select>
        </div>
        <div class="card-body-custom">
          ${all.length > 0 ? `<div class="table-responsive"><table class="table table-hover mb-0"><thead><tr><th>Date</th><th>Doctor</th><th>Clinic</th><th>Status</th><th>Diagnosis</th><th>Actions</th></tr></thead><tbody>
            ${all.map(a => `<tr>
              <td>${a.date}</td>
              <td>${escapeHtml(a.doctorName || '-')}</td>
              <td>${escapeHtml(a.clinicName || '-')}</td>
              <td><span class="badge-status badge-${(a.appointmentStatus || '').toLowerCase()}">${escapeHtml(a.appointmentStatus || '-')}</span></td>
              <td>${escapeHtml(a.diagnosis || '-')}</td>
              <td>${a.appointmentStatus === 'Scheduled' ? `<button class="btn-action delete" onclick="cancelAppointment(${a.appointmentId})"><i class="bi bi-x-circle me-1"></i>Cancel</button>` : '-'}</td>
            </tr>`).join('')}
          </tbody></table></div>` : '<div class="empty-state"><i class="bi bi-calendar-x"></i><p>No appointments found.</p></div>'}
        </div>
      </div>`;

    document.getElementById('statusFilter').addEventListener('change', (e) => {
      loadAppointments(e.target.value || undefined);
    });
  } catch (err) {
    document.getElementById('contentArea').innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

async function cancelAppointment(id) {
  if (!confirm('Are you sure you want to cancel this appointment?')) return;
  try {
    await Api.patientCancelAppt(id);
    showToast('Appointment cancelled', 'success');
    showSection('appointments');
  } catch (err) { showToast(err.message, 'error'); }
}

// ─── DIAGNOSES ─────────────────────────────────────────────
async function loadDiagnoses() {
  try {
    const diagnoses = await Api.patientDiagnoses();
    const all = Array.isArray(diagnoses) ? diagnoses : [];

    document.getElementById('contentArea').innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom"><h5><i class="bi bi-clipboard2-pulse me-2"></i>My Diagnoses</h5></div>
        <div class="card-body-custom">
          ${all.length > 0 ? `<div class="table-responsive"><table class="table table-hover mb-0"><thead><tr><th>Date</th><th>Doctor</th><th>Diagnosis</th></tr></thead><tbody>
            ${all.map(d => `<tr>
              <td>${d.diagnosisDate || '—'}</td>
              <td>${escapeHtml(d.doctorName || '-')}</td>
              <td>${escapeHtml(d.diagnosis1 || '-')}</td>
            </tr>`).join('')}
          </tbody></table></div>` : '<div class="empty-state"><i class="bi bi-clipboard-x"></i><p>No diagnoses yet.</p></div>'}
        </div>
      </div>`;
  } catch (err) {
    document.getElementById('contentArea').innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

// ─── MEDICAL HISTORY ───────────────────────────────────────
async function loadHistory() {
  try {
    const history = await Api.patientHistory();
    const all = Array.isArray(history) ? history : [];

    document.getElementById('contentArea').innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom"><h5><i class="bi bi-clock-history me-2"></i>Medical History</h5></div>
        <div class="card-body-custom">
          ${all.length > 0 ? `<div class="table-responsive"><table class="table table-hover mb-0"><thead><tr><th>Date</th><th>Doctor</th><th>Specialization</th><th>Clinic</th><th>Status</th><th>Diagnosis</th></tr></thead><tbody>
            ${all.map(h => `<tr>
              <td>${h.date}</td>
              <td>${escapeHtml(h.doctorName || '-')}</td>
              <td>${escapeHtml(h.specialization || '-')}</td>
              <td>${escapeHtml(h.clinicName || '-')}</td>
              <td><span class="badge-status badge-${(h.appointmentStatus || '').toLowerCase()}">${escapeHtml(h.appointmentStatus || '-')}</span></td>
              <td>${h.diagnosis ? escapeHtml(h.diagnosis.diagnosis1 || '-') : '-'}</td>
            </tr>`).join('')}
          </tbody></table></div>` : '<div class="empty-state"><i class="bi bi-clock-history"></i><p>No medical history yet.</p></div>'}
        </div>
      </div>`;
  } catch (err) {
    document.getElementById('contentArea').innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

// ─── PROFILE ───────────────────────────────────────────────
async function loadProfile() {
  try {
    const p = await Api.patientProfile();
    const picUrl = Api.getProfilePictureUrl(p.profilePicturePath);

    document.getElementById('contentArea').innerHTML = `
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
              <h5 class="mb-1">${escapeHtml(p.name || '')}</h5>
              <p class="text-muted mb-0">${escapeHtml(p.email || '')}</p>
              <small class="text-muted">Click the camera icon to upload a profile picture</small>
            </div>
          </div>
          <form id="profileForm">
            <div class="row g-3">
              <div class="col-md-6"><label class="form-label">Full Name</label><input type="text" class="form-control" id="profileName" value="${escapeAttr(p.name || '')}" required></div>
              <div class="col-md-6"><label class="form-label">Email</label><input type="email" class="form-control" value="${escapeAttr(p.email || '')}" disabled></div>
              <div class="col-md-6"><label class="form-label">Phone</label><input type="text" class="form-control" id="profilePhone" value="${escapeAttr(String(p.phone || ''))}"></div>
              <div class="col-md-6"><label class="form-label">Address</label><input type="text" class="form-control" id="profileAddress" value="${escapeAttr(p.address || '')}"></div>
            </div>
            <div class="mt-4"><button type="submit" class="btn btn-primary"><i class="bi bi-check-lg me-1"></i>Save Changes</button></div>
          </form>
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
        const data = {
          name: document.getElementById('profileName').value.trim(),
          phone: parseInt(document.getElementById('profilePhone').value) || undefined,
          address: document.getElementById('profileAddress').value.trim() || undefined
        };
        await Api.patientUpdateProfile(data);
        msgEl.innerHTML = '<div class="alert alert-success">Profile updated successfully.</div>';
        const user = Api.getUser();
        if (user) { user.name = data.name; localStorage.setItem('clinic_user', JSON.stringify(user)); document.getElementById('userName').textContent = data.name; }
      } catch (err) { msgEl.innerHTML = `<div class="alert alert-danger">${escapeHtml(err.message)}</div>`; }
    });
  } catch (err) {
    document.getElementById('contentArea').innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle me-2"></i>${escapeHtml(err.message)}</div>`;
  }
}

// ─── HELPERS ───────────────────────────────────────────────
function formatDate(dateStr) {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (isNaN(d.getTime())) return dateStr;
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

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

function showToast(message, type = 'success') {
  let container = document.getElementById('toastContainer');
  if (!container) {
    container = document.createElement('div');
    container.id = 'toastContainer';
    container.style.cssText = 'position:fixed;top:20px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:10px;';
    document.body.appendChild(container);
  }
  const bgMap = { success: 'bg-success', error: 'bg-danger' };
  const toast = document.createElement('div');
  toast.className = 'toast show align-items-center text-white border-0';
  toast.style.cssText = 'min-width:300px;box-shadow:0 4px 12px rgba(0,0,0,.15);';
  toast.innerHTML = `<div class="d-flex"><div class="toast-body ${bgMap[type] || 'bg-secondary'}">${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto"></button></div>`;
  toast.querySelector('.btn-close').addEventListener('click', () => toast.remove());
  container.appendChild(toast);
  setTimeout(() => { if (toast.parentElement) toast.remove(); }, 4000);
}

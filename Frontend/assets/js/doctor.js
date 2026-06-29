let currentSection = 'dashboard';

async function initDoctor() {
  if (!Api.requireAuth('Doctor')) return;
  const user = Api.getUser();
  if (user) document.getElementById('userName').textContent = user.name || user.email;

  try {
    const profile = await Api.doctorProfile();
    if (profile.profilePicturePath) {
      const badge = document.querySelector('.user-badge');
      if (badge) {
        const picUrl = Api.getProfilePictureUrl(profile.profilePicturePath);
        badge.innerHTML = `<img src="${picUrl}" alt="" style="width:28px;height:28px;border-radius:50%;object-fit:cover"> <span id="userName">${escapeHtml(user?.name || user?.email || 'Doctor')}</span>`;
      }
    }
  } catch (e) { /* ignore */ }

  document.querySelectorAll('.sidebar-nav a').forEach(link => {
    link.addEventListener('click', (e) => {
      e.preventDefault();
      const section = link.dataset.section;
      showSection(section);
    });
  });

  showSection('dashboard');
}

function showSection(name) {
  currentSection = name;
  document.querySelectorAll('.sidebar-nav a').forEach(a => a.classList.remove('active'));
  const activeLink = document.querySelector(`.sidebar-nav a[data-section="${name}"]`);
  if (activeLink) activeLink.classList.add('active');

  const titles = {
    dashboard: 'Dashboard',
    upcoming: 'Upcoming Appointments',
    completed: 'Completed Appointments',
    patients: 'My Patients',
    profile: 'My Profile',
    clinics: 'My Clinics',
    certificates: 'My Certificates'
  };
  document.getElementById('pageTitle').textContent = titles[name] || 'Dashboard';

  const area = document.getElementById('contentArea');
  area.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>';

  switch (name) {
    case 'dashboard': loadDashboard(); break;
    case 'upcoming': loadUpcoming(); break;
    case 'completed': loadCompleted(); break;
    case 'patients': loadPatients(); break;
    case 'profile': loadProfile(); break;
    case 'clinics': loadClinics(); break;
    case 'certificates': loadCertificates(); break;
    default: loadDashboard();
  }
}

// ─── DASHBOARD ───────────────────────────────────────────
async function loadDashboard() {
  const area = document.getElementById('contentArea');
  try {
    const [upcoming, completed, patients] = await Promise.all([
      Api.doctorUpcoming(),
      Api.doctorCompleted(),
      Api.doctorPatients()
    ]);

    area.innerHTML = `
      <div class="row g-3 mb-4">
        <div class="col-md-4">
          <div class="stat-card">
            <div class="stat-icon blue"><i class="bi bi-calendar-event"></i></div>
            <div class="stat-info">
              <h3>${upcoming.length}</h3>
              <p>Upcoming</p>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="stat-card">
            <div class="stat-icon green"><i class="bi bi-check-circle"></i></div>
            <div class="stat-info">
              <h3>${completed.length}</h3>
              <p>Completed</p>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="stat-card">
            <div class="stat-icon purple"><i class="bi bi-people"></i></div>
            <div class="stat-info">
              <h3>${patients.length}</h3>
              <p>Total Patients</p>
            </div>
          </div>
        </div>
      </div>

      <div class="row g-3">
        <div class="col-lg-6">
          <div class="dash-card">
            <div class="card-header-custom">
              <h5><i class="bi bi-calendar-check me-2 text-primary"></i>Upcoming Appointments</h5>
              <a href="#" class="text-decoration-none" onclick="showSection('upcoming'); return false;">View All</a>
            </div>
            <div class="card-body-custom">
              ${upcoming.length === 0
                ? '<div class="empty-state"><i class="bi bi-calendar-x"></i><p>No upcoming appointments</p></div>'
                : `<div class="table-responsive"><table class="table table-hover mb-0">
                  <thead><tr><th>Date</th><th>Patient</th><th>Clinic</th></tr></thead>
                  <tbody>${upcoming.slice(0, 5).map(a => `
                    <tr>
                      <td>${formatDate(a.date)}</td>
                      <td>${a.patientName}</td>
                      <td>${a.clinicName || '-'}</td>
                    </tr>`).join('')}
                  </tbody>
                </table></div>`
              }
            </div>
          </div>
        </div>
        <div class="col-lg-6">
          <div class="dash-card">
            <div class="card-header-custom">
              <h5><i class="bi bi-check-circle me-2 text-success"></i>Recent Completed</h5>
              <a href="#" class="text-decoration-none" onclick="showSection('completed'); return false;">View All</a>
            </div>
            <div class="card-body-custom">
              ${completed.length === 0
                ? '<div class="empty-state"><i class="bi bi-inbox"></i><p>No completed appointments</p></div>'
                : `<div class="table-responsive"><table class="table table-hover mb-0">
                  <thead><tr><th>Date</th><th>Patient</th><th>Diagnosis</th></tr></thead>
                  <tbody>${completed.slice(0, 5).map(a => `
                    <tr>
                      <td>${formatDate(a.date)}</td>
                      <td>${a.patientName}</td>
                      <td>${a.diagnosis || '-'}</td>
                    </tr>`).join('')}
                  </tbody>
                </table></div>`
              }
            </div>
          </div>
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load dashboard'}</div>`;
  }
}

// ─── UPCOMING APPOINTMENTS ───────────────────────────────
async function loadUpcoming() {
  const area = document.getElementById('contentArea');
  try {
    const data = await Api.doctorUpcoming();

    if (data.length === 0) {
      area.innerHTML = '<div class="empty-state"><i class="bi bi-calendar-x"></i><p>No upcoming appointments</p></div>';
      return;
    }

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5>Upcoming Appointments</h5>
          <span class="badge bg-primary">${data.length} total</span>
        </div>
        <div class="card-body-custom">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Patient</th>
                  <th>Clinic</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                ${data.map(a => `
                  <tr>
                    <td>${formatDate(a.date)}</td>
                    <td>${a.patientName}</td>
                    <td>${a.clinicName || '-'}</td>
                    <td><span class="badge-status badge-scheduled">${a.appointmentStatus}</span></td>
                    <td>
                      <button class="btn-action view me-1" onclick="completeAppointment(${a.appointmentId})">
                        <i class="bi bi-check-lg"></i> Complete
                      </button>
                      <button class="btn-action delete" onclick="cancelAppointment(${a.appointmentId})">
                        <i class="bi bi-x-lg"></i> Cancel
                      </button>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load appointments'}</div>`;
  }
}

// ─── COMPLETED APPOINTMENTS ──────────────────────────────
async function loadCompleted() {
  const area = document.getElementById('contentArea');
  try {
    const data = await Api.doctorCompleted();

    if (data.length === 0) {
      area.innerHTML = '<div class="empty-state"><i class="bi bi-inbox"></i><p>No completed appointments</p></div>';
      return;
    }

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5>Completed Appointments</h5>
          <span class="badge bg-success">${data.length} total</span>
        </div>
        <div class="card-body-custom">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Patient</th>
                  <th>Clinic</th>
                  <th>Diagnosis</th>
                </tr>
              </thead>
              <tbody>
                ${data.map(a => `
                  <tr>
                    <td>${formatDate(a.date)}</td>
                    <td>${a.patientName}</td>
                    <td>${a.clinicName || '-'}</td>
                    <td>${a.diagnosis || '<em class="text-muted">No diagnosis</em>'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load completed appointments'}</div>`;
  }
}

// ─── MY PATIENTS ─────────────────────────────────────────
async function loadPatients() {
  const area = document.getElementById('contentArea');
  try {
    const data = await Api.doctorPatients();

    if (data.length === 0) {
      area.innerHTML = '<div class="empty-state"><i class="bi bi-people"></i><p>No patients yet</p></div>';
      return;
    }

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5>My Patients</h5>
          <span class="badge bg-primary">${data.length} total</span>
        </div>
        <div class="card-body-custom">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Phone</th>
                  <th>Last Visit</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                ${data.map(p => `
                  <tr>
                    <td>${p.name}</td>
                    <td>${p.email}</td>
                    <td>${p.phone || '-'}</td>
                    <td>${formatDate(p.lastVisitDate)}</td>
                    <td>
                      <button class="btn-action view" onclick="viewPatientHistory(${p.id}, '${p.name}')">
                        <i class="bi bi-clock-history"></i> History
                      </button>
                    </td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load patients'}</div>`;
  }
}

// ─── VIEW PATIENT HISTORY ────────────────────────────────
async function viewPatientHistory(patientId, patientName) {
  const area = document.getElementById('contentArea');
  document.getElementById('pageTitle').textContent = `History: ${patientName}`;
  area.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-primary"></div></div>';

  try {
    const data = await Api.doctorPatientHistory(patientId);

    area.innerHTML = `
      <div class="d-flex align-items-center mb-3">
        <button class="btn btn-sm btn-outline-secondary me-3" onclick="showSection('patients')">
          <i class="bi bi-arrow-left"></i> Back
        </button>
        <h5 class="mb-0">Visit History: ${patientName}</h5>
      </div>
      <div class="dash-card">
        <div class="card-body-custom">
          ${data.length === 0
            ? '<div class="empty-state"><i class="bi bi-inbox"></i><p>No visit history</p></div>'
            : `<div class="table-responsive"><table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Clinic</th>
                  <th>Diagnosis</th>
                </tr>
              </thead>
              <tbody>
                ${data.map(h => `
                  <tr>
                    <td>${formatDate(h.date)}</td>
                    <td><span class="badge-status badge-${h.appointmentStatus.toLowerCase()}">${h.appointmentStatus}</span></td>
                    <td>${h.clinicName || '-'}</td>
                    <td>${h.diagnosis ? h.diagnosis.diagnosis1 || '-' : '-'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table></div>`
          }
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load patient history'}</div>`;
  }
}

// ─── MY PROFILE ──────────────────────────────────────────
async function loadProfile() {
  const area = document.getElementById('contentArea');
  try {
    const data = await Api.doctorProfile();
    const picUrl = Api.getProfilePictureUrl(data.profilePicturePath);

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5>My Profile</h5>
        </div>
        <div class="card-body-custom">
          <div id="profileMsg" class="d-none"></div>
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
              <h5 class="mb-1">Dr. ${escapeHtml(data.name || '')}</h5>
              <p class="text-muted mb-0">${escapeHtml(data.email || '')}</p>
              <small class="text-muted">Click the camera icon to upload a profile picture</small>
            </div>
          </div>
          <form id="profileForm">
            <div class="row g-3">
              <div class="col-md-6">
                <label class="form-label">Full Name</label>
                <input type="text" class="form-control" name="name" value="${data.name || ''}">
              </div>
              <div class="col-md-6">
                <label class="form-label">Email</label>
                <input type="email" class="form-control" value="${data.email || ''}" disabled>
              </div>
              <div class="col-md-6">
                <label class="form-label">Phone</label>
                <input type="number" class="form-control" name="phone" value="${data.phone || ''}">
              </div>
              <div class="col-md-6">
                <label class="form-label">Address</label>
                <input type="text" class="form-control" name="address" value="${data.address || ''}">
              </div>
              <div class="col-md-6">
                <label class="form-label">Specialization</label>
                <input type="text" class="form-control" value="${data.specializationName || ''}" disabled>
              </div>
              <div class="col-md-6">
                <label class="form-label">Experience (years)</label>
                <input type="number" class="form-control" name="experience" value="${data.experience || ''}">
              </div>
              <div class="col-12">
                <label class="form-label">About</label>
                <textarea class="form-control" name="about" rows="4">${data.about || ''}</textarea>
              </div>
            </div>
            <div class="mt-3">
              <button type="submit" class="btn btn-primary">
                <i class="bi bi-save me-1"></i> Save Changes
              </button>
            </div>
          </form>
        </div>
      </div>
    `;

    document.getElementById('profilePicInput').addEventListener('change', async (e) => {
      const file = e.target.files[0];
      if (!file) return;
      const msg = document.getElementById('profileMsg');
      if (file.size > 5 * 1024 * 1024) { msg.className = 'alert alert-danger'; msg.textContent = 'File must be under 5MB.'; msg.classList.remove('d-none'); return; }
      try {
        msg.className = 'alert alert-info'; msg.textContent = 'Uploading...'; msg.classList.remove('d-none');
        const result = await Api.uploadProfilePicture(file);
        const wrapper = document.getElementById('profilePicWrapper');
        const newImg = document.createElement('img');
        newImg.id = 'profilePic';
        newImg.src = Api.getProfilePictureUrl(result.fileName);
        newImg.alt = 'Profile';
        newImg.style.cssText = 'width:100px;height:100px;border-radius:50%;object-fit:cover;border:3px solid #dee2e6';
        const old = document.getElementById('profilePic');
        wrapper.replaceChild(newImg, old);
        msg.className = 'alert alert-success'; msg.textContent = 'Profile picture updated!'; msg.classList.remove('d-none');
      } catch (err) { msg.className = 'alert alert-danger'; msg.textContent = err.message || 'Upload failed'; msg.classList.remove('d-none'); }
    });

    document.getElementById('profileForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      const msg = document.getElementById('profileMsg');
      const btn = e.target.querySelector('button[type="submit"]');
      btn.disabled = true;
      btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

      try {
        const body = {};
        if (e.target.name.value) body.name = e.target.name.value;
        if (e.target.phone.value) body.phone = parseInt(e.target.phone.value);
        if (e.target.address.value) body.address = e.target.address.value;
        if (e.target.about.value) body.about = e.target.about.value;
        if (e.target.experience.value) body.experience = e.target.experience.value;

        await Api.doctorUpdateProfile(body);
        msg.className = 'alert alert-success';
        msg.textContent = 'Profile updated successfully!';
        msg.classList.remove('d-none');

        const user = Api.getUser();
        if (user && body.name) {
          user.name = body.name;
          localStorage.setItem('clinic_user', JSON.stringify(user));
          document.getElementById('userName').textContent = body.name;
        }
      } catch (err) {
        msg.className = 'alert alert-danger';
        msg.textContent = err.message || 'Failed to update profile';
        msg.classList.remove('d-none');
      } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-save me-1"></i> Save Changes';
      }
    });
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load profile'}</div>`;
  }
}

// ─── MY CLINICS ──────────────────────────────────────────
async function loadClinics() {
  const area = document.getElementById('contentArea');
  try {
    const data = await Api.doctorClinics();

    if (data.length === 0) {
      area.innerHTML = '<div class="empty-state"><i class="bi bi-building"></i><p>No clinics assigned</p></div>';
      return;
    }

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5>My Clinics</h5>
          <span class="badge bg-primary">${data.length} total</span>
        </div>
        <div class="card-body-custom">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Address</th>
                  <th>Phone</th>
                </tr>
              </thead>
              <tbody>
                ${data.map(c => `
                  <tr>
                    <td><i class="bi bi-building me-2 text-primary"></i>${c.clinicName}</td>
                    <td>${c.address || '-'}</td>
                    <td>${c.phone || '-'}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load clinics'}</div>`;
  }
}

// ─── MY CERTIFICATES ─────────────────────────────────────
async function loadCertificates() {
  const area = document.getElementById('contentArea');
  try {
    const data = await Api.doctorCertificates();

    if (data.length === 0) {
      area.innerHTML = '<div class="empty-state"><i class="bi bi-award"></i><p>No certificates</p></div>';
      return;
    }

    area.innerHTML = `
      <div class="dash-card">
        <div class="card-header-custom">
          <h5>My Certificates</h5>
          <span class="badge bg-primary">${data.length} total</span>
        </div>
        <div class="card-body-custom">
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Certificate Name</th>
                </tr>
              </thead>
              <tbody>
                ${data.map((c, i) => `
                  <tr>
                    <td>${i + 1}</td>
                    <td><i class="bi bi-award me-2 text-warning"></i>${c.certificateName}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    `;
  } catch (err) {
    area.innerHTML = `<div class="alert alert-danger">${err.message || 'Failed to load certificates'}</div>`;
  }
}

// ─── ACTIONS ─────────────────────────────────────────────
async function completeAppointment(id) {
  if (!confirm('Mark this appointment as completed?')) return;
  try {
    await Api.doctorCompleteAppt(id);
    showSection(currentSection);
  } catch (err) {
    alert(err.message || 'Failed to complete appointment');
  }
}

async function cancelAppointment(id) {
  if (!confirm('Cancel this appointment?')) return;
  try {
    await Api.doctorCancelAppt(id);
    showSection(currentSection);
  } catch (err) {
    alert(err.message || 'Failed to cancel appointment');
  }
}

// ─── UTILITIES ───────────────────────────────────────────
function formatDate(dateStr) {
  if (!dateStr) return '-';
  const d = new Date(dateStr);
  if (isNaN(d)) return dateStr;
  return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
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

function authNav() {
  const nav = document.getElementById('nav-auth');
  if (!nav) return;
  const user = Api.getUser();
  if (user) {
    const dashUrl = user.role === 'Admin' ? 'admin.html' : user.role === 'Doctor' ? 'doctor-dashboard.html' : 'patient-dashboard.html';
    nav.innerHTML = `
      <li class="dropdown"><a href="#"><span><i class="bi bi-person-circle me-1"></i>${user.name || user.email}</span> <i class="bi bi-chevron-down toggle-dropdown"></i></a>
      <ul>
        <li><a href="${dashUrl}"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a></li>
        <li><a href="#" onclick="Api.logout(); return false;"><i class="bi bi-box-arrow-right me-1"></i>Logout</a></li>
      </ul></li>`;
  } else {
    nav.innerHTML = `
      <li><a href="login.html" class="btn-login-nav">Login</a></li>
      <li><a href="signup.html" class="btn-signup-nav">Sign Up</a></li>`;
  }
}

function setupLoginForm() {
  const form = document.getElementById('loginForm');
  if (!form) return;
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = form.querySelector('button[type="submit"]');
    const msg = document.getElementById('loginMsg');
    btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Logging in...';
    msg.className = 'd-none';
    try {
      const data = await Api.login(form.email.value, form.password.value);
      Api.saveAuth(data.token, data.user, data.refreshToken);
      const dashUrl = data.user.role === 'Admin' ? 'admin.html' : data.user.role === 'Doctor' ? 'doctor-dashboard.html' : 'patient-dashboard.html';
      window.location.href = dashUrl;
    } catch (err) {
      msg.className = 'alert alert-danger';
      msg.textContent = err.message || 'Login failed';
    } finally {
      btn.disabled = false; btn.innerHTML = '<i class="bi bi-box-arrow-in-right me-2"></i>Login';
    }
  });
}

function setupSignupForm() {
  const form = document.getElementById('signupForm');
  if (!form) return;
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = form.querySelector('button[type="submit"]');
    const msg = document.getElementById('signupMsg');
    btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Creating account...';
    msg.className = 'd-none';
    try {
      const data = await Api.register({
        name: form.name.value,
        email: form.email.value,
        phone: parseInt(form.phone.value),
        password: form.password.value,
        role: 'Patient'
      });
      Api.saveAuth(data.token, data.user, data.refreshToken);
      const dashUrl = data.user.role === 'Admin' ? 'admin.html' : data.user.role === 'Doctor' ? 'doctor-dashboard.html' : 'patient-dashboard.html';
      window.location.href = dashUrl;
    } catch (err) {
      msg.className = 'alert alert-danger';
      msg.textContent = err.message || 'Registration failed';
    } finally {
      btn.disabled = false; btn.innerHTML = '<i class="bi bi-person-plus me-2"></i>Create Account';
    }
  });
}

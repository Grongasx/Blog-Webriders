// THROTTLE — site.js (Blog Público)

document.addEventListener('DOMContentLoaded', () => {

  // ── Fade-up on scroll (IntersectionObserver) ──────────────
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(e => {
      if (e.isIntersecting) {
        e.target.classList.add('visible');
        observer.unobserve(e.target);
      }
    });
  }, { threshold: 0.12 });

  document.querySelectorAll('.fade-up').forEach(el => observer.observe(el));

  // ── Smooth active nav link ─────────────────────────────────
  const path = window.location.pathname;
  document.querySelectorAll('.nav-links a').forEach(a => {
    if (a.getAttribute('href') === path) a.classList.add('active');
  });

  // ── Post view count auto-increment (fire & forget) ─────────
  const postSlug = document.body.dataset.postSlug;
  if (postSlug) {
    fetch(`/post/${postSlug}/view`, { method: 'POST' }).catch(() => {});
  }

  // ── Flash message auto-dismiss ─────────────────────────────
  const flash = document.querySelector('.flash');
  if (flash) {
    setTimeout(() => flash.style.transition = 'opacity .5s', 100);
    setTimeout(() => { flash.style.opacity = '0'; }, 4000);
    setTimeout(() => flash.remove(), 4600);
  }

  // ── Newsletter forms (AJAX) ─────────────────────────────────
  document.querySelectorAll('.newsletter-form-ajax').forEach(nlForm => {
    const msgId = nlForm.dataset.messageId;
    const msgEl = msgId ? document.getElementById(msgId) : null;

    nlForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const btn = nlForm.querySelector('button[type="submit"]');
      const original = btn.textContent;
      btn.textContent = '...';
      btn.disabled = true;
      if (msgEl) { msgEl.textContent = ''; msgEl.className = 'newsletter-message'; }

      try {
        const res = await fetch(nlForm.action, {
          method: 'POST',
          body: new FormData(nlForm),
          credentials: 'include',
          headers: { 'X-Requested-With': 'XMLHttpRequest' },
        });

        const data = await res.json().catch(() => null);
        if (data && msgEl) {
          msgEl.textContent = data.message;
          msgEl.className = 'newsletter-message ' + (data.success ? 'success' : 'error');
          if (data.success) nlForm.reset();
        } else if (res.redirected) {
          window.location.href = res.url;
          return;
        }
      } catch {
        if (msgEl) {
          msgEl.textContent = 'Erro de conexão. Tente novamente.';
          msgEl.className = 'newsletter-message error';
        }
      } finally {
        btn.textContent = original;
        btn.disabled = false;
      }
    });
  });
});
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

  // ── Newsletter form submit via fetch (sem reload) ───────────
  const nlForm = document.querySelector('.newsletter-form');
  if (nlForm) {
    nlForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const btn = nlForm.querySelector('button[type="submit"]');
      const original = btn.textContent;
      btn.textContent = '...';
      btn.disabled = true;

      try {
        const res = await fetch(nlForm.action, {
          method: 'POST',
          body: new FormData(nlForm),
        });
        // redireciona se o servidor retornar redirect
        if (res.redirected) {
          window.location.href = res.url;
          return;
        }
        btn.textContent = '✓ Inscrito!';
        nlForm.reset();
      } catch {
        btn.textContent = 'Erro. Tente novamente.';
      } finally {
        setTimeout(() => { btn.textContent = original; btn.disabled = false; }, 3000);
      }
    });
  }
});
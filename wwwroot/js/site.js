// THROTTLE — site.js (Blog Público)

document.addEventListener('DOMContentLoaded', () => {

  // ── Menu mobile (hamburger) ──────────────────────────────────
  document.addEventListener('click', (e) => {
    const toggle   = document.getElementById('nav-toggle');
    const collapse = document.getElementById('nav-collapse');
    if (!toggle || !collapse) return;

    if (e.target.closest('#nav-toggle')) {
      const isOpen = collapse.classList.toggle('open');
      toggle.setAttribute('aria-expanded', isOpen);
      return;
    }

    if (collapse.classList.contains('open')
        && (e.target.closest('#nav-collapse a') || !collapse.contains(e.target))) {
      collapse.classList.remove('open');
      toggle.setAttribute('aria-expanded', 'false');
    }
  });

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


window.Loading = {
    isAnimating: false,
    startTime: 0,

    // Buscamos os elementos DENTRO das funções, não no topo
    start: function() {
        const overlay = document.getElementById('loading-overlay');
        const moto = document.getElementById('motorcycle-loader');
        const bar = document.getElementById('progress-bar');

        if (!overlay || this.isAnimating) return;
        
        this.isAnimating = true;
        this.startTime = Date.now();

        // Remove a classe hidden (o display: none)
        overlay.classList.remove('loading-hidden');
        if (moto) moto.classList.remove('speed-off');

        // Reset visual
        bar.style.transition = 'none';
        bar.style.width = '0%';
        bar.offsetWidth; // Force reflow
        bar.style.transition = 'width 2s linear';
        bar.style.width = '80%';
    },

    stop: async function() {
        const overlay = document.getElementById('loading-overlay');
        const moto = document.getElementById('motorcycle-loader');
        const bar = document.getElementById('progress-bar');

        if (!overlay) return;

        const elapsed = Date.now() - this.startTime;
        const remaining = Math.max(0, 2000 - elapsed);

        await new Promise(resolve => setTimeout(resolve, remaining));

        bar.style.transition = 'width 0.5s ease-out';
        bar.style.width = '100%';

        setTimeout(() => {
            if (moto) moto.classList.add('speed-off');
            setTimeout(() => {
                // Aplica a classe que tem o display: none !important
                overlay.classList.add('loading-hidden');
                this.isAnimating = false;
            }, 600);
        }, 300);
    }
};

// ==========================================
// INTEGRAÇÃO COM TURBO DRIVE
// ==========================================

// Quando o usuário clica em um link, o Turbo intercepta e inicia
document.addEventListener("turbo:before-visit", () => {
    Loading.start();
});

// Quando o Turbo termina de renderizar a página nova
document.addEventListener("turbo:load", () => {
    initFadeUpAnimations(); 
    
    // 2. Garanta que o loading finalize (o que já fizemos)
    window.Loading.stop();
});

function initFadeUpAnimations() {
    // Coloque aqui o código que detecta as classes .fade-up e remove a invisibilidade
    const elements = document.querySelectorAll('.fade-up');
    elements.forEach(el => {
        el.style.opacity = '1'; // Exemplo simples
        el.classList.add('visible'); // Ou o que seu script original faz
    });
}

// Opcional: Se houver erro ou o usuário cancelar a navegação
document.addEventListener("turbo:visit-error", () => {
    // Esconde o loading caso dê erro no servidor
    document.getElementById('loading-overlay').classList.add('loading-hidden');
    Loading.isAnimating = false;
});
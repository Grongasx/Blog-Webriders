// THROTTLE — admin.js (Painel Admin)

// ── TOAST ──────────────────────────────────────────────────────
let _toastTimer;
function showToast(msg, icon = '✓', isError = false) {
  let t = document.getElementById('toast');
  if (!t) {
    t = document.createElement('div');
    t.id = 'toast';
    t.style.cssText = `
      position:fixed;bottom:28px;right:28px;
      background:var(--card);border:1px solid var(--border);
      border-left:3px solid var(--success);padding:14px 20px;
      font-size:13px;display:flex;align-items:center;gap:10px;
      z-index:9000;transform:translateY(80px);opacity:0;
      transition:all .35s cubic-bezier(.34,1.56,.64,1);max-width:320px;
      font-family:'Barlow',sans-serif;
    `;
    t.innerHTML = '<span id="toast-icon"></span><span id="toast-msg"></span>';
    document.body.appendChild(t);
  }
  document.getElementById('toast-icon').textContent = icon;
  document.getElementById('toast-msg').textContent  = msg;
  t.style.borderLeftColor = isError ? 'var(--red)' : 'var(--success)';
  t.style.transform = 'translateY(0)';
  t.style.opacity   = '1';
  clearTimeout(_toastTimer);
  _toastTimer = setTimeout(() => {
    t.style.transform = 'translateY(80px)';
    t.style.opacity   = '0';
  }, 3000);
}

// ── AUTO-SHOW alerts como toast ────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {

  document.querySelectorAll('.alert-success').forEach(el => {
    showToast(el.textContent.trim(), '✓', false);
    el.style.display = 'none';
  });
  document.querySelectorAll('.alert-error').forEach(el => {
    showToast(el.textContent.trim(), '⚠', true);
  });

  // ── Tecla Escape fecha modal ────────────────────────────────
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape')
      document.querySelectorAll('.modal-overlay.open')
              .forEach(m => m.classList.remove('open'));
  });

  // ── Sidebar mobile toggle ───────────────────────────────────
  const sidebar = document.getElementById('sidebar');
  const menuBtn = document.getElementById('menu-toggle');
  if (menuBtn && sidebar) {
    menuBtn.addEventListener('click', () => sidebar.classList.toggle('open'));
    document.addEventListener('click', e => {
      if (sidebar.classList.contains('open')
          && !sidebar.contains(e.target)
          && e.target !== menuBtn)
        sidebar.classList.remove('open');
    });
  }

  // ── Confirma exclusão nos forms ─────────────────────────────
  document.querySelectorAll('form[data-confirm]').forEach(f => {
    f.addEventListener('submit', e => {
      if (!confirm(f.dataset.confirm)) e.preventDefault();
    });
  });

  // ── Image URL preview no editor ─────────────────────────────
  const imgInput = document.getElementById('img-url-input');
  if (imgInput) {
    imgInput.addEventListener('input', () => previewAdminImage(imgInput.value));
  }

  // ── Upload de imagem (drag & drop + file picker) ────────────
  initUploadZone();

  // ── Copy URL button ─────────────────────────────────────────
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const url = btn.previousElementSibling?.textContent;
      if (url) {
        navigator.clipboard.writeText(url.trim());
        showToast('URL copiada!', '📋');
      }
    });
  });
});

// ── ADMIN IMAGE PREVIEW ────────────────────────────────────────
function previewAdminImage(url) {
  const prev = document.getElementById('img-preview');
  const ph   = document.getElementById('img-placeholder');
  if (url && url.startsWith('http')) {
    if (prev) { prev.src = url; prev.style.display = 'block'; }
    else {
      const img = document.createElement('img');
      img.id = 'img-preview'; img.className = 'img-preview'; img.src = url;
      img.onerror = () => { img.remove(); if (ph) ph.style.display = 'flex'; };
      ph?.parentNode.insertBefore(img, ph);
    }
    if (ph) ph.style.display = 'none';
  } else {
    if (prev) prev.style.display = 'none';
    if (ph)   ph.style.display   = 'flex';
  }
}

// ── UPLOAD ZONE ───────────────────────────────────────────────
function initUploadZone() {
  const zone = document.getElementById('upload-zone');
  if (!zone) return;

  const fileInput  = document.getElementById('upload-input');
  const progress   = document.getElementById('upload-progress');
  const progressBar= document.getElementById('upload-progress-bar');
  const previewGrid= document.getElementById('upload-preview-grid');

  zone.addEventListener('click', () => fileInput?.click());

  zone.addEventListener('dragover', e => {
    e.preventDefault(); zone.classList.add('drag-over');
  });
  zone.addEventListener('dragleave', () => zone.classList.remove('drag-over'));
  zone.addEventListener('drop', e => {
    e.preventDefault(); zone.classList.remove('drag-over');
    handleFiles(e.dataTransfer.files);
  });

  fileInput?.addEventListener('change', () => handleFiles(fileInput.files));

  async function handleFiles(files) {
    for (const file of files) {
      if (!file.type.startsWith('image/')) continue;

      const form = new FormData();
      form.append('file', file);
      form.append('__RequestVerificationToken',
        document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '');

      if (progress)    progress.style.display = 'block';
      if (progressBar) progressBar.style.width = '0%';

      try {
        // Simula progresso enquanto faz upload
        let pct = 0;
        const ticker = setInterval(() => {
          pct = Math.min(pct + 10, 85);
          if (progressBar) progressBar.style.width = pct + '%';
        }, 80);

        const res  = await fetch('/admin/upload', { method: 'POST', body: form });
        clearInterval(ticker);
        if (progressBar) progressBar.style.width = '100%';

        if (!res.ok) throw new Error('Upload falhou');
        const { url } = await res.json();

        addPreviewItem(url, previewGrid);
        showToast('Imagem enviada!', '✓');

        // Preenche o campo de URL do editor se existir
        const imgField = document.getElementById('FeaturedImage') || document.getElementById('img-url-input');
        if (imgField) { imgField.value = url; previewAdminImage(url); }

      } catch (err) {
        showToast('Erro no upload: ' + err.message, '⚠', true);
      } finally {
        setTimeout(() => { if (progress) progress.style.display = 'none'; }, 800);
      }
    }
  }

  function addPreviewItem(url, grid) {
    if (!grid) return;
    const div = document.createElement('div');
    div.className = 'upload-preview-item';
    div.innerHTML = `
      <img src="${url}" alt="">
      <button type="button" onclick="this.parentElement.remove()" title="Remover">✕</button>
      <div style="display:flex;gap:4px;margin-top:4px;">
        <div class="uploaded-url">${url}</div>
        <button type="button" class="copy-btn" onclick="navigator.clipboard.writeText('${url}');showToast('URL copiada!','📋')">⎘</button>
      </div>
    `;
    grid.appendChild(div);
  }
}

// ── FEATURED TOGGLE ────────────────────────────────────────────
function toggleFeatured(el) {
  el.classList.toggle('on');
  const cb = document.getElementById('featured-cb');
  if (cb) cb.checked = el.classList.contains('on');
}

// ── FEATURE TOGGLE (configurações) ────────────────────────────
function toggleCheck(el, name) {
  el.classList.toggle('on');
  const cb = document.getElementById('cb-' + name);
  if (cb) cb.checked = el.classList.contains('on');
}

// ── PUBLISH NOW (editor) ───────────────────────────────────────
function publishNow() {
  const sel = document.querySelector('select[name="Status"]');
  if (sel) sel.value = 'Published';
  document.getElementById('post-form')?.submit();
}
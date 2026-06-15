// markets.js - fetch and render paginated market list with sorting & filters

  document.addEventListener('DOMContentLoaded', () => {
    // Ensure pagination starts from page 1 even if URL contains a stale ?page= value
    const url = new URL(window.location);
    if (url.searchParams.has('page')) {
      url.searchParams.delete('page');
      window.history.replaceState({}, '', url);
    }
    const tableBody = document.getElementById('markets-body');
    const paginationDiv = document.getElementById('pagination');
    const searchInput = document.getElementById('search-input');
  const categoryFilter = document.getElementById('category-filter');
  const statusFilter = document.getElementById('status-filter');
  const applyBtn = document.getElementById('apply-filters');

  let currentPage = 1;
  const pageSize = 10;
  let sortBy = '';
  let sortDesc = false;

  const loadMarkets = async () => {
    const params = new URLSearchParams({
      page: currentPage,
      pageSize,
    });
    if (searchInput.value) params.append('search', searchInput.value);
    if (categoryFilter.value) params.append('category', categoryFilter.value);
    if (statusFilter.value) params.append('status', statusFilter.value);
    if (sortBy) {
      params.append('sortBy', sortBy);
      params.append('sortDesc', sortDesc);
    }
    try {
      const res = await fetch(`/api/dashboard/markets?${params.toString()}`);
      if (!res.ok) throw new Error('Failed to load markets');
      const data = await res.json();
      renderTable(data.items);
      renderPagination(data.totalCount);
      populateCategoryOptions(data.items);
    } catch (e) {
      tableBody.innerHTML = `<tr><td colspan="6" class="error-message">${e.message}</td></tr>`;
    }
  };

  const renderTable = (items) => {
    if (!items.length) {
      tableBody.innerHTML = '<tr><td colspan="6">No markets found.</td></tr>';
      return;
    }
    tableBody.innerHTML = items.map(m => `
      <tr>
        <td>${m.question}</td>
        <td>${m.category}</td>
        <td>${m.source || ''}</td>
        <td>${new Date(m.createdDate).toLocaleDateString()}</td>
        <td>${m.endDate ? new Date(m.endDate).toLocaleDateString() : ''}</td>
        <td>${m.resolutionDate ? new Date(m.resolutionDate).toLocaleDateString() : ''}</td>
        <td>${m.status}</td>
      </tr>`).join('');
  };

  const renderPagination = (totalCount) => {
    const totalPages = Math.ceil(totalCount / pageSize);
    if (totalPages <= 1) {
      paginationDiv.innerHTML = '';
      return;
    }
    let html = '';
    
    // Always show first page
    html += `<button ${1 === currentPage ? 'disabled' : ''} data-page="1">1</button>`;
    
    let start = Math.max(2, currentPage - 2);
    let end = Math.min(totalPages - 1, currentPage + 2);
    
    if (start > 2) {
      html += `<span style="align-self: center; padding: 0 0.2rem; opacity: 0.5;">...</span>`;
    }
    
    for (let i = start; i <= end; i++) {
      html += `<button ${i === currentPage ? 'disabled' : ''} data-page="${i}">${i}</button>`;
    }
    
    if (end < totalPages - 1) {
      html += `<span style="align-self: center; padding: 0 0.2rem; opacity: 0.5;">...</span>`;
    }
    
    // Always show last page
    html += `<button ${totalPages === currentPage ? 'disabled' : ''} data-page="${totalPages}">${totalPages}</button>`;
    
    paginationDiv.innerHTML = html;
    paginationDiv.querySelectorAll('button').forEach(btn => {
      btn.addEventListener('click', () => {
        currentPage = Number(btn.dataset.page);
        loadMarkets();
      });
    });
  };

  const populateCategoryOptions = (items) => {
    const seen = new Set();
    items.forEach(m => {
      if (m.category && !seen.has(m.category)) {
        const opt = document.createElement('option');
        opt.value = m.category;
        opt.textContent = m.category;
        categoryFilter.appendChild(opt);
        seen.add(m.category);
      }
    });
  };

  // Header sort handling
  document.querySelectorAll('#markets-table th[data-sort]').forEach(th => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', () => {
      const field = th.dataset.sort;
      if (sortBy === field) {
        sortDesc = !sortDesc; // toggle
      } else {
        sortBy = field;
        sortDesc = false;
      }
      loadMarkets();
    });
  });

  applyBtn.addEventListener('click', () => {
    currentPage = 1;
    loadMarkets();
  });

  // Initial load
  loadMarkets();
});

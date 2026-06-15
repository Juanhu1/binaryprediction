// opportunities.js - fetch and render paginated opportunities with filters & sorting

document.addEventListener('DOMContentLoaded', () => {
  // Ensure pagination starts from page 1 even if URL contains a stale ?page= value
  const url = new URL(window.location);
  // Remove any existing query parameters to ensure pagination starts fresh
  window.history.replaceState({}, '', url.pathname);

  const tbody = document.getElementById('opportunities-body');
  const pagination = document.getElementById('pagination');
  const searchInput = document.getElementById('search-input');
  const statusFilter = document.getElementById('status-filter');
  const minGapInput = document.getElementById('mingap-input');
  const maxGapInput = document.getElementById('maxgap-input');
  const applyBtn = document.getElementById('apply-filters');

  let currentPage = 1;
  const pageSize = 10;
  let sortBy = '';
  let sortDesc = false;

  const load = async () => {
    const params = new URLSearchParams({ page: currentPage, pageSize });
    if (searchInput.value) params.append('search', searchInput.value);
    if (statusFilter.value) params.append('status', statusFilter.value);
    if (minGapInput.value) params.append('minGap', minGapInput.value);
    if (maxGapInput.value) params.append('maxGap', maxGapInput.value);
    if (sortBy) {
      params.append('sortBy', sortBy);
      params.append('sortDesc', sortDesc);
    }
    try {
      const res = await fetch(`/api/dashboard/opportunities?${params.toString()}`);
      if (!res.ok) throw new Error('Failed to load opportunities');
      const data = await res.json();
      render(data.items);
      renderPagination(data.totalCount);
    } catch (e) {
      tbody.innerHTML = `<tr><td colspan="8" class="error-message">${e.message}</td></tr>`;
    }
  };

    const render = (items) => {
        if (!items.length) {
            tbody.innerHTML = `<tr><td colspan="8">No opportunities found.</td></tr>`;
            return;
        }
        tbody.innerHTML = items.map(o => {
            const formatPercent = v => {
              const num = Number(v);
              if (isNaN(num)) return '';
              const pct = num > 1 ? num : num * 100;
              return pct.toFixed(2) + '%';
            };
            const marketProb = (o.marketProbability ?? o.MarketProbability ?? 0);
            const aiProb = (o.aiProbability ?? o.AiProbability ?? 0);
            const gap = o.probabilityGap ?? o.ProbabilityGap ?? 0;
            const direction = o.direction ?? o.Direction ?? '';
            const question = o.question ?? o.Question ?? '';
            const category = o.category ?? o.Category ?? '';
            const url = o.polymarketUrl ?? o.PolymarketUrl ?? '#';
            const detected = (o.detectedAtUtc ?? o.DetectedAtUtc) ? new Date(o.detectedAtUtc ?? o.DetectedAtUtc).toLocaleDateString() : '';
            const endDate = (o.endDate ?? o.EndDate ?? o.market?.endDate ?? o.Market?.endDate ?? null);
            const formattedEndDate = endDate ? new Date(endDate).toLocaleDateString() : '';
            return `
                <tr class="opportunity-row" data-id="${o.id ?? o.Id}" data-market-id="${o.marketId ?? o.MarketId}" data-question="${question}" data-category="${category}" data-slug="${o.marketSlug ?? o.MarketSlug}" data-enddate="${formattedEndDate}">
                    <td>${question}</td>
                    <td>${category}</td>
                    <td>${formatPercent(marketProb)}</td>
                    <td>${formattedEndDate}</td>
                    <td>${formatPercent(aiProb)}</td>
                    <td>${formatPercent(gap)}</td>
                    <td>${direction}</td>
                    <td><a href="${url}" target="_blank">Open</a></td>
                    <td>${detected}</td>
                </tr>`;

        }).join('');
        // Attach click handlers for detail modal
        tbody.querySelectorAll('.opportunity-row').forEach(row => {
            row.addEventListener('click', () => {
                document.getElementById('modal-opportunity-id').textContent = row.dataset.id;
                document.getElementById('modal-market-id').textContent = row.dataset.marketId;
                document.getElementById('modal-question').textContent = row.dataset.question;
                document.getElementById('modal-category').textContent = row.dataset.category;
                document.getElementById('modal-slug').textContent = row.dataset.slug;
                document.getElementById('details-modal').style.display = 'flex';
            });
        });
    };
    // Modal close handler
    document.getElementById('modal-close').addEventListener('click', () => {
        document.getElementById('details-modal').style.display = 'none';
    });

  const renderPagination = (total) => {
    const totalPages = Math.ceil(total / pageSize);
    let html = '';
    for (let i = 1; i <= totalPages; i++) {
      html += `<button ${i === currentPage ? 'disabled' : ''} data-page="${i}">${i}</button>`;
    }
    pagination.innerHTML = html;
    pagination.querySelectorAll('button').forEach(b => {
      b.addEventListener('click', () => {
        currentPage = Number(b.dataset.page);
        load();
      });
    });
  };

  // Sorting headers
  document.querySelectorAll('#opportunities-table th[data-sort]').forEach(th => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', () => {
      const field = th.dataset.sort;
      if (sortBy === field) sortDesc = !sortDesc; else { sortBy = field; sortDesc = false; }
      load();
    });
  });

  applyBtn.addEventListener('click', () => { currentPage = 1; load(); });

  load();
});

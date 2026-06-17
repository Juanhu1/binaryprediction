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
  const sourceFilter = document.getElementById('source-filter');
  const minGapInput = document.getElementById('mingap-input');
  const maxGapInput = document.getElementById('maxgap-input');
  const applyBtn = document.getElementById('apply-filters');

  let currentPage = 1;
  const pageSize = 10;
  let sortBy = 'edgescore';
  let sortDesc = true;

  const countOpen = document.getElementById('count-open');
  const countActive = document.getElementById('count-active');
  const countExpired = document.getElementById('count-expired');
  const countIgnored = document.getElementById('count-ignored');
  const countResolved = document.getElementById('count-resolved');
 
  const metricTotalRecords = document.getElementById('metric-total-records');
  const metricUniqueMarkets = document.getElementById('metric-unique-markets');
  const metricActiveOpportunities = document.getElementById('metric-active-opportunities');

  const updateCardSelection = () => {
    const currentStatus = statusFilter.value;
    document.querySelectorAll('#opportunity-counts .card').forEach(card => {
      if (card.dataset.status === currentStatus) {
        card.style.border = '2px solid var(--primary-color)';
        card.style.backgroundColor = 'rgba(255, 255, 255, 0.12)';
      } else {
        card.style.border = '1px solid transparent';
        card.style.backgroundColor = '';
      }
    });
  };

  document.querySelectorAll('#opportunity-counts .card').forEach(card => {
    card.addEventListener('click', () => {
      const selectedStatus = card.dataset.status;
      if (statusFilter.value === selectedStatus) {
        statusFilter.value = '';
      } else {
        statusFilter.value = selectedStatus;
      }
      currentPage = 1;
      load();
    });
  });

  const load = async () => {
    const params = new URLSearchParams({ page: currentPage, pageSize });
    if (searchInput.value) params.append('search', searchInput.value);
    if (statusFilter.value) params.append('status', statusFilter.value);
    if (sourceFilter.value) params.append('source', sourceFilter.value);
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
      
      // Update status counts in the header cards
      if (countOpen) countOpen.textContent = data.openCount ?? 0;
      if (countActive) countActive.textContent = data.activeCount ?? 0;
      if (countExpired) countExpired.textContent = data.expiredCount ?? 0;
      if (countIgnored) countIgnored.textContent = data.ignoredCount ?? 0;
      if (countResolved) countResolved.textContent = data.resolvedCount ?? 0;
 
      if (metricTotalRecords) metricTotalRecords.textContent = data.totalOpportunityRecords ?? 0;
      if (metricUniqueMarkets) metricUniqueMarkets.textContent = data.uniqueMarketsWithOpportunities ?? 0;
      if (metricActiveOpportunities) metricActiveOpportunities.textContent = data.currentActiveOpportunities ?? 0;

      updateCardSelection();
      render(data.items);
      renderPagination(data.totalCount);
    } catch (e) {
      tbody.innerHTML = `<tr><td colspan="12" class="error-message">${e.message}</td></tr>`;
    }
  };

    const render = (items) => {
        if (!items.length) {
            tbody.innerHTML = `<tr><td colspan="12">No opportunities found.</td></tr>`;
            return;
        }
        tbody.innerHTML = items.map(o => {
            const formatPercent = v => {
              const num = Number(v);
              if (isNaN(num)) return '';
              return num.toFixed(2) + '%';
            };
            const marketProb = (o.marketProbability ?? o.MarketProbability ?? 0);
            const aiProb = (o.aiProbability ?? o.AiProbability ?? 0);
            const confidence = (o.confidencePercentage ?? o.ConfidencePercentage ?? 0);
            const gap = o.probabilityGap ?? o.ProbabilityGap ?? 0;
            const edgeScore = o.edgeScore ?? o.EdgeScore ?? 0;
            const direction = o.direction ?? o.Direction ?? '';
            let directionText = direction;
            if (direction === 'AIHigher') directionText = 'AI Higher';
            else if (direction === 'AILower') directionText = 'AI Lower';
            const question = o.question ?? o.Question ?? '';
            const category = o.category ?? o.Category ?? '';
            const sourceVal = o.marketSource ?? o.MarketSource ?? 1;
            const sourceText = (sourceVal === 2 || sourceVal === 'Kalshi' || sourceVal === '2') ? 'Kalshi' : 'Polymarket';
            const sourceBadgeClass = sourceText.toLowerCase() === 'kalshi' ? 'badge-kalshi' : 'badge-polymarket';
            const sourceBadge = `<span class="badge ${sourceBadgeClass}">${sourceText}</span>`;
            const url = o.sourceUrl ?? o.SourceUrl ?? o.polymarketUrl ?? o.PolymarketUrl ?? '#';
            const detected = (o.detectedAtUtc ?? o.DetectedAtUtc) ? new Date(o.detectedAtUtc ?? o.DetectedAtUtc) .toLocaleDateString() : '';
            const endDate = (o.endDate ?? o.EndDate ?? o.market?.endDate ?? o.Market?.endDate ?? null);
            const formattedEndDate = endDate ? new Date(endDate).toLocaleDateString() : '';
            const formattedEdgeScore = Number(edgeScore).toFixed(2);
            return `
                <tr class="opportunity-row" data-id="${o.id ?? o.Id}" data-market-id="${o.marketId ?? o.MarketId}" data-question="${question}" data-category="${category}" data-slug="${o.marketSlug ?? o.MarketSlug}" data-enddate="${formattedEndDate}">
                    <td>${question}</td>
                    <td>${category}</td>
                    <td>${sourceBadge}</td>
                    <td>${formatPercent(marketProb)}</td>
                    <td>${formatPercent(aiProb)}</td>
                    <td>${formatPercent(confidence)}</td>
                    <td>${formatPercent(gap)}</td>
                    <td>${directionText}</td>
                    <td>${formattedEdgeScore}</td>
                    <td>${formattedEndDate}</td>
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
    if (totalPages <= 1) {
      pagination.innerHTML = '';
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

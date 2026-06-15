// predictions.js - fetch and render paginated predictions with filters/sorting

  document.addEventListener('DOMContentLoaded', () => {
    // Ensure pagination starts from page 1 even if URL contains a stale ?page= value
    const url = new URL(window.location);
    if (url.searchParams.has('page')) {
      url.searchParams.delete('page');
      window.history.replaceState({}, '', url);
    }
    const tbody = document.getElementById('predictions-body');
    const pagination = document.getElementById('pagination');
    const searchInput = document.getElementById('search-input');
    const categoryFilter = document.getElementById('category-filter');
    const pendingOnly = document.getElementById('pending-only');
    const evaluatedOnly = document.getElementById('evaluated-only');
 
  const applyBtn = document.getElementById('apply-filters');

  let currentPage = 1;
  const pageSize = 10;
  let sortBy = '';
  let sortDesc = false;

  const load = async () => {
    const params = new URLSearchParams({ page: currentPage, pageSize });
    if (searchInput.value) params.append('search', searchInput.value);
    if (categoryFilter.value) params.append('category', categoryFilter.value);
    if (pendingOnly.checked) params.append('pendingOnly', 'true');
    if (evaluatedOnly.checked) params.append('evaluatedOnly', 'true');
    if (sortBy) {
      params.append('sortBy', sortBy);
      params.append('sortDesc', sortDesc);
    }
    try {
      const res = await fetch(`/api/dashboard/predictions?${params.toString()}`);
      if (!res.ok) throw new Error('Failed to load predictions');
      const data = await res.json();
      render(data.items);
      renderPagination(data.totalCount);
      populateCategory(data.items);
    } catch (e) {
      tbody.innerHTML = `<tr><td colspan="6" class="error-message">${e.message}</td></tr>`;
    }
  };

  const render = (items) => {
    if (!items.length) {
      tbody.innerHTML = '<tr><td colspan="6">No predictions found.</td></tr>';
      return;
    }
    tbody.innerHTML = items.map(p => `
      <tr data-id="${p.id}" class="${p.predictedOutcome && p.actualOutcome ? (p.predictedOutcome === p.actualOutcome ? 'correct' : 'incorrect') : 'pending'}">
        <td>${p.question}</td>
        <td>${p.category}</td>
        <td>${p.predictedOutcome ?? ''}</td>
        <td>${p.confidencePercentage.toFixed(2)}%</td>
        <td>${new Date(p.createdDate).toLocaleDateString()}</td>
        <td>${p.evaluatedDate ? new Date(p.evaluatedDate).toLocaleDateString() : ''}</td>
        <td>${p.actualOutcome ?? ''}</td>
        <td>${p.predictionError != null ? p.predictionError.toFixed(4) : ''}</td>
      </tr>`).join('');

    // Attach click listeners for modal detail view
    tbody.querySelectorAll('tr[data-id]').forEach(row => {
      row.addEventListener('click', async () => {
        const id = row.dataset.id;
        try {
          const res = await fetch(`/api/dashboard/prediction/${id}`);
          if (!res.ok) throw new Error('Failed to load details');
          const detail = await res.json();
          const modal = document.getElementById('prediction-modal');
          const body = document.getElementById('modal-body');
          body.innerHTML = `
            <p><strong>Question:</strong> ${detail.Question}</p>
            <p><strong>Category:</strong> ${detail.Category}</p>
            <p><strong>Predicted Outcome:</strong> ${detail.PredictedOutcome}</p>
            <p><strong>Confidence %:</strong> ${detail.ConfidencePercentage.toFixed(2)}%</p>
            <p><strong>Created:</strong> ${new Date(detail.CreatedDate).toLocaleString()}</p>
            <p><strong>Evaluated:</strong> ${detail.EvaluatedDate ? new Date(detail.EvaluatedDate).toLocaleString() : '—'}</p>
            <p><strong>Actual Outcome:</strong> ${detail.ActualOutcome ?? ''}</p>
            <p><strong>Prediction Error:</strong> ${detail.PredictionError != null ? detail.PredictionError.toFixed(4) : ''}</p>
            <h3>Analysis</h3>
            <p><strong>Market Summary:</strong> ${detail.MarketSummary}</p>
            <p><strong>Supporting Evidence:</strong> ${detail.SupportingEvidence.join(', ')}</p>
            <p><strong>Contradicting Evidence:</strong> ${detail.ContradictingEvidence ? detail.ContradictingEvidence.join(', ') : ''}</p>
            <p><strong>Key Risks:</strong> ${detail.KeyRisks ? detail.KeyRisks.join(', ') : ''}</p>
            <p><strong>Confidence Explanation:</strong> ${detail.ConfidenceExplanation}</p>
            <p><strong>Final Probability:</strong> ${detail.FinalProbability}</p>
          `;
          modal.classList.add('open');
        } catch (e) {
          console.error(e);
        }
      });
    });
  };

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

  const populateCategory = (items) => {
    const seen = new Set();
    items.forEach(p => {
      if (p.category && !seen.has(p.category)) {
        const opt = document.createElement('option');
        opt.value = p.category;
        opt.textContent = p.category;
        categoryFilter.appendChild(opt);
        seen.add(p.category);
      }
    });
  };

  // Sorting headers
  document.querySelectorAll('#predictions-table th[data-sort]').forEach(th => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', () => {
      const field = th.dataset.sort;
      if (sortBy === field) sortDesc = !sortDesc; else { sortBy = field; sortDesc = false; }
      load();
    });
  });

  applyBtn.addEventListener('click', () => { currentPage = 1; load(); });

  // Close modal handler
  document.getElementById('modal-close').addEventListener('click', () => {
    document.getElementById('prediction-modal').classList.remove('open');
  });

  load();
});

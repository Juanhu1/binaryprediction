// system.js - fetch and display system statistics

document.addEventListener('DOMContentLoaded', () => {
  const ids = {
    totalMarkets: 'total-markets',
    marketsAddedToday: 'markets-today',
    totalPredictions: 'total-predictions',
    predictionsGeneratedToday: 'predictions-today',
    predictionsEvaluatedToday: 'predictions-eval-today',
    totalOpportunities: 'total-opportunities',
    opportunitiesDetectedToday: 'opps-today',
    latestAnalyticsSnapshotDate: 'latest-snapshot'
  };

  fetch('/api/dashboard/system')
    .then(res => {
      if (!res.ok) throw new Error('Failed to load system stats');
      return res.json();
    })
    .then(data => {
      Object.entries(ids).forEach(([key, elId]) => {
        const el = document.getElementById(elId);
        if (!el) return;
        let value = data[key];
        if (key === 'latestAnalyticsSnapshotDate' && value) {
          value = new Date(value).toLocaleString();
        }
        el.textContent = value ?? '-';
      });
    })
    .catch(err => {
      const panel = document.querySelector('.panel');
      panel.innerHTML += `<div class='error-message'>${err.message}</div>`;
    });
});

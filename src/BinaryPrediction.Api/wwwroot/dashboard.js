// dashboard.js - fetch overview metrics and render cards
document.addEventListener('DOMContentLoaded', () => {
  const cardsContainer = document.getElementById('overview-cards');
  if (!cardsContainer) return;

  fetch('/api/dashboard/overview')
    .then(res => res.ok ? res.json() : Promise.reject('Failed to load overview'))
    .then(data => {
      const metrics = [
        { label: 'Total Markets', value: data.totalMarkets },
        { label: 'Open Markets', value: data.openMarkets },
        { label: 'Resolved Markets', value: data.resolvedMarkets },
        { label: 'Total Predictions', value: data.totalPredictions },
        { label: 'Pending Predictions', value: data.pendingPredictions },
        { label: 'Evaluated Predictions', value: data.evaluatedPredictions },
        { label: 'Accuracy %', value: data.accuracyPercentage + '%' },
        { label: 'Average Confidence', value: data.averageConfidence },
        { label: 'Avg Prediction Error', value: data.averagePredictionError },
        { label: 'Total Opportunities', value: data.totalOpportunities },
        { label: 'Active Opportunities', value: data.activeOpportunities },
        { label: 'Resolved Opportunities', value: data.resolvedOpportunities }
      ];
      cardsContainer.innerHTML = metrics.map(m => `
        <div class="card">
          <h3>${m.label}</h3>
          <p>${m.value}</p>
        </div>`).join('');
    })
    .catch(err => {
      cardsContainer.innerHTML = `<div class="card"><p class="error-message">${err}</p></div>`;
    });
});

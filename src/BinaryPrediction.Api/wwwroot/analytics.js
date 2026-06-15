// analytics.js - fetch and display analytics metrics

document.addEventListener('DOMContentLoaded', () => {
  const accuracyEl = document.getElementById('accuracy');
  const avgConfEl = document.getElementById('avg-confidence');
  const avgErrorEl = document.getElementById('avg-error');
  const confidenceTbody = document.querySelector('#confidence-table tbody');
  const calibrationTbody = document.querySelector('#calibration-table tbody');

  fetch('/api/dashboard/analytics')
    .then(res => {
      if (!res.ok) throw new Error('Failed to load analytics');
      return res.json();
    })
    .then(data => {
      accuracyEl.textContent = (data.accuracyPercentage * 100).toFixed(2) + '%';
      avgConfEl.textContent = data.averageConfidence.toFixed(2);
        // Populate evaluation summary
        document.getElementById('total-evaluated').textContent = data.totalEvaluated ?? '-';
        document.getElementById('correct-count').textContent = data.correctCount ?? '-';
        document.getElementById('incorrect-count').textContent = data.incorrectCount ?? '-';
        document.getElementById('accuracy-summary').textContent = (data.accuracyPercentage * 100).toFixed(2) + '%';

      // Confidence buckets
      confidenceTbody.innerHTML = data.confidenceBuckets.map(b => `
        <tr><td>${b.rangeStart} - ${b.rangeEnd}</td><td>${b.count}</td></tr>`).join('');

      // Calibration snapshots
      calibrationTbody.innerHTML = data.calibrationSnapshots.map(s => `
        <tr>
          <td>${new Date(s.snapshotDateUtc).toLocaleDateString()}</td>
          <td>${s.confidence.toFixed(2)}</td>
          <td>${(s.observedAccuracy * 100).toFixed(2)}%</td>
        </tr>`).join('');
    })
    .catch(err => {
      const panel = document.querySelector('.panel');
      panel.innerHTML += `<div class='error-message'>${err.message}</div>`;
    });
});

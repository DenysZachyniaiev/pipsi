document.addEventListener('DOMContentLoaded', () => {
    const table = document.querySelector('table.sortable-table');
    if (!table) return;

    const headers = table.querySelectorAll('th');
    const tbody = table.querySelector('tbody');
    const searchInput = document.getElementById('searchInput');

    let currentSortIndex = -1;
    let currentSortDirection = 'asc';

    headers.forEach((th, index) => {
        th.addEventListener('click', () => {
            const direction = (index === currentSortIndex && currentSortDirection === 'asc') ? 'desc' : 'asc';
            currentSortIndex = index;
            currentSortDirection = direction;

            const rows = Array.from(tbody.querySelectorAll('tr'));
            rows.sort((a, b) => {
                const aText = a.children[index].textContent.trim().toLowerCase();
                const bText = b.children[index].textContent.trim().toLowerCase();
                return direction === 'asc' ? aText.localeCompare(bText) : bText.localeCompare(aText);
            });

            rows.forEach(row => tbody.appendChild(row));

            headers.forEach(h => {
                const icon = h.querySelector('.sort-icon');
                if (icon) icon.textContent = '↕';
            });

            const icon = th.querySelector('.sort-icon');
            if (icon) icon.textContent = direction === 'asc' ? '↑' : '↓';
        });
    });

    if (searchInput) {
        searchInput.addEventListener('input', () => {
            const filter = searchInput.value.toLowerCase();
            tbody.querySelectorAll('tr').forEach(row => {
                row.style.display = row.textContent.toLowerCase().includes(filter) ? '' : 'none';
            });
        });
    }
});
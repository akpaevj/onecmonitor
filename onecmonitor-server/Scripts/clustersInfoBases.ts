import {Tab} from "bootstrap";

export function initClustersInfoBases(clustersTable: HTMLTableElement, infoBasesTables: HTMLDivElement) {
    const rows = clustersTable.querySelectorAll<HTMLTableRowElement>('tbody tr');

    rows.forEach(row => {
        row.addEventListener('click', () => {
            selectClusterRow(clustersTable, infoBasesTables, row);
        })
    });
    
    if (rows.length > 0)
        selectClusterRow(clustersTable, infoBasesTables, rows.item(0));
}

function selectClusterRow(clustersTable: HTMLTableElement, infoBasesTables: HTMLDivElement, row: HTMLTableRowElement) {
    unselectClustersRows(clustersTable);
    hideInfoBasesTables(infoBasesTables);

    row.classList.add('table-active');

    const clusterId = row.dataset['clusterId'];
    const table = infoBasesTables.querySelector<HTMLTableElement>(`[id="infoBases-${clusterId}"]`);
    table.hidden = false;
}

function unselectClustersRows(clustersTable: HTMLTableElement) {
    clustersTable.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('table-active');
    })
}

function hideInfoBasesTables(infoBasesTables: HTMLDivElement) {
    infoBasesTables.querySelectorAll('table').forEach(table => {
        table.hidden = true;
    })
}
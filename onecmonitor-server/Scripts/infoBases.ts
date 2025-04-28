class V8InfoBaseInfoDto {
    name: string;
    synonym: string;
    version: string;
    sslVersion: string;
}

export function loadInfoBasesInfo() {
    document.querySelectorAll<HTMLTableRowElement>('tbody tr').forEach(async row => {
        const response = await fetch(`/InfoBases/InfoBaseInfo?id=${row.dataset.infobaseId}`);
        
        if (response.ok) {
            const info = await response.json();
            
            row.querySelector<HTMLTableCellElement>('td[data-type="configuration"]').textContent = info.name;
            row.querySelector<HTMLTableCellElement>('td[data-type="version"]').textContent = info.version;
            row.querySelector<HTMLTableCellElement>('td[data-type="sslVersion"]').textContent = info.sslVersion;
        }
    })
}
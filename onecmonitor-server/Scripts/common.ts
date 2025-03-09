import {Tab} from 'bootstrap'

export function setIndexes(items: NodeListOf<HTMLInputElement>) {
    items.forEach((item, index) => {
        item.name = item.name.replace('[]', `[${index}]`);
    })
}

export function selectTab(item: HTMLElement) {
    const bsTab = new Tab(item)
    bsTab.show();
}

export function setSuccessDangerTextColor(item: HTMLElement, value: boolean) {
    if (value)
        item.classList.add('text-success');
    else
        item.classList.add('text-danger');
}
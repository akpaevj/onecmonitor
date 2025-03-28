import {Tab} from 'bootstrap'

export function setIndexes(items: NodeListOf<HTMLInputElement>) {
    items.forEach((item, index) => {
        setIndex(item, index);
    })
}

export function setExactlyIndex(items: NodeListOf<HTMLInputElement>, index: number) {
    items.forEach((item) => {
        setIndex(item, index);
    })
}

export function setIndex(item: HTMLInputElement, index: number) {
    item.name = item.name.replace('[]', `[${index}]`);
}

export function selectTab(item: HTMLElement) {
    const bsTab = new Tab(item);
    bsTab.show();
}

export function setSuccessDangerTextColor(item: HTMLElement, value: boolean) {
    if (value)
        item.classList.add('text-success');
    else
        item.classList.add('text-danger');
}

export function initShowArchived() {
    const input = document.querySelector<HTMLInputElement>('#showArchived');
    input.addEventListener('change', () => {
        setUrlParameterValue('showArchived', input.checked)
    });
}

export function setUrlParameterValue(param: string, value: any) {
    const params = new URLSearchParams(window.location.search);
    params.set(param, value);
    window.location.search = params.toString();
}
import {Modal} from "bootstrap";
import * as common from './common'

export function setIndexesOnSubmit(listIds: string[]) {
    listIds.forEach((listId: string) => {
        common.setIndexesOnSubmit(document.querySelectorAll<HTMLInputElement>(`#${listId} input`));
    })
}

export function showModal(id: string) {
    const element = document.getElementById(id);
    new Modal(element).show();
}

export function initSelectItemDialog(id: string) {
    const dialogId = `${id}-dialog`;
    const itemsListId = `${id}-list`;

    function hideShowCardBody() {
        const itemsList = document.getElementById(itemsListId);
        const body = itemsList.parentElement;

        body.hidden = itemsList.children.length == 0;
    }

    const form = document.querySelector('form');
    form.addEventListener('submit', (e) => {
        setIndexesOnSubmit([itemsListId]);
    })

    const input = document.querySelector<HTMLInputElement>(`#${dialogId} input[type="search"]`);
    input.onkeyup = _ => {
        const query = input.value.toUpperCase();

        document.querySelectorAll<HTMLButtonElement>(`#${dialogId} .list-group button`).forEach(item => {
            if (item.innerText.toUpperCase().includes(query))
                item.removeAttribute('hidden');
            else
                item.setAttribute('hidden', '');
        })
    }

    document.querySelectorAll<HTMLButtonElement>(`#${dialogId} .list-group button`).forEach((button: HTMLButtonElement) => {
        button.onclick = () => {
            selectItem(dialogId, itemsListId, button);
            hideShowCardBody();
        }
    })

    document.querySelectorAll<HTMLButtonElement>(`#${itemsListId} button`).forEach((button: HTMLButtonElement) => {
        button.onclick = () => {
            removeSelectedItem(dialogId, itemsListId, button);
            hideShowCardBody();
        }
    })

    hideShowCardBody();
}

export function selectItem(dialogId: string, itemsListId: string, button: HTMLButtonElement) {
    const list = document.getElementById(itemsListId) as HTMLDivElement;
    list.append(button);

    button.onclick = () => {
        removeSelectedItem(dialogId, itemsListId, button);
    };

    const modal = new Modal(document.getElementById(dialogId));
    modal.hide();
}

export function removeSelectedItem(dialogId: string, itemsListId: string, button: HTMLButtonElement) {
    const selectList = document.querySelector(`#${dialogId} .list-group`) as HTMLDivElement;
    selectList.append(button);

    button.onclick = () =>
        selectItem(dialogId, itemsListId, button);
}
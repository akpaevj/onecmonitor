import {Modal} from "bootstrap";

export function openDeleteDialog(itemId: string, item: string, modalId: string = "deleteDialog") {
    const elem = document.getElementById(modalId);
    elem.innerHTML = elem.innerHTML.replace('{template_name}', item).replace('deleting_item_id', itemId);

    const modal = new Modal(elem);
    modal.show();
}
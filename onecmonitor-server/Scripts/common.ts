export function setIndexesOnSubmit(items: NodeListOf<HTMLInputElement>) {
    items.forEach((item, index) => {
        item.name = item.name.replace('[]', `[${index}]`);
    })
}
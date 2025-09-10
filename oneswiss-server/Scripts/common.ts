import * as marked from 'marked'

export async function renderMarkdown(containerClass: string, data: string) {
    document.querySelector('.' + containerClass).innerHTML = await marked.parse(data);
}
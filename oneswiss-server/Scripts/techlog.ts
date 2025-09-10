import * as monaco from 'monaco-editor';
import hljs from 'highlight.js/lib/core';
import sql from 'highlight.js/lib/languages/sql';

hljs.registerLanguage('sql', sql);

export function highlight(filter: string): string {
    return hljs.highlight(filter, {language: 'sql'}).value;
}

let editor: monaco.editor.IStandaloneCodeEditor;

export function initTechLogTemplateEditor(content: string) {
    const container = document.getElementById('container');

    if (!container)
        throw new Error("Элемент с id 'container' не найден.");

    editor = monaco.editor.create(container, {
        value: content,
        language: 'xml',
        theme: 'vs-dark',
        automaticLayout: true,
        fontSize: 14,
        minimap: {enabled: true},
        tabSize: 2,
        formatOnType: true,
        formatOnPaste: true
    });
}

export function getTechLogTemplateEditorContent(): string {
    return editor.getValue();
}
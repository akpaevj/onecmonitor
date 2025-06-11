import hljs from "highlight.js";
import '../node_modules/highlight.js/styles/idea.min.css'

export function highlightJson() {
    let codeElement = document.querySelector('code');
    codeElement.textContent = JSON.stringify(JSON.parse(codeElement.textContent), null, 2);
    hljs.highlightElement(codeElement);
}
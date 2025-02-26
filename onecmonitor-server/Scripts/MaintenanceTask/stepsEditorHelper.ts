import Mermaid from "mermaid";

export class StepsEditorHelper {
    private schemaInput: HTMLInputElement;

    constructor(input: HTMLInputElement) {
        this.schemaInput = input;
    }

    public async init() {
        Mermaid.initialize({
            theme: 'dark',
            securityLevel: 'loose',
            startOnLoad: false,
            darkMode: true
        })

        await this.redrawNodes()
    }

    findNode(id: string, node: any): any | undefined {
        if (node.Id == id)
            return node;

        const currentNode = node;
        
        let foundNode = undefined;
        
        if (currentNode.hasOwnProperty('RightNode') && currentNode.RightNode != undefined)
            foundNode = this.findNode(id, currentNode.RightNode);

        if (foundNode == undefined && currentNode.hasOwnProperty('LeftNode') && currentNode.LeftNode != undefined)
            foundNode = this.findNode(id, currentNode.LeftNode);

        return foundNode;
    }

    deleteNode(node: any, removingNode: any) {
        if (node.hasOwnProperty('RightNode') && node.RightNode != undefined)
        {
            if (node.RightNode.Id == removingNode.Id)
            {
                delete node['RightNode'];
                delete node['RightNodeId'];
                
                return;
            }
            else
                this.deleteNode(node.RightNode, removingNode);
        }

        if (node.hasOwnProperty('LeftNode') && node.LeftNode != undefined)
        {
            if (node.LeftNode.Id == removingNode.Id)
            {
                delete node['LeftNode'];
                delete node['LeftNodeId'];

                return;
            }
            else
                this.deleteNode(node.LeftNode, removingNode);
        }
    }

    async addRootNode(node: any) {
        this.saveNode(node);
        await this.redrawNodes();
    }

    async addLeftNode(parentId: string, node: any) {
        const root = this.getRootNode();
        const parent = this.findNode(parentId, root);
        parent.LeftNodeId = node.Id;
        parent.LeftNode = node;

        this.saveNode(root);
        await this.redrawNodes();
    }

    async addRightNode(root: any, parent: any, node: any) {
        parent.RightNodeId = node.Id;
        parent.RightNode = node;

        this.saveNode(root);
        await this.redrawNodes();
    }

    saveNode(node: any | undefined) {
        if (node == undefined)
            this.schemaInput.value = '';
        else
            this.schemaInput.value = JSON.stringify(node);
    }

    getRootNode(): any | undefined {
        if (this.schemaInput.value == "")
            return undefined;
        else
            return JSON.parse(this.schemaInput.value);
    }

    private nodeToGraphDefinition(currentNode: any): string {
        let currentNodeText = "";

        let currentNodeLeft = currentNode.Id;

        // is try/catch node
        if (currentNode.Kind == 1) {
            const node = currentNode;

            currentNodeLeft = `${currentNodeLeft}{${currentNode.Step.title}}`

            let needSetCurrentNodeText = true;

            if (node.hasOwnProperty('LeftNode') && node.LeftNode != undefined) {
                currentNodeText += currentNodeLeft + " -->|Успешно| " + this.nodeToGraphDefinition(node.LeftNode) + '\r';
                needSetCurrentNodeText = false;
            }

            if (node.hasOwnProperty('RightNode') && node.RightNode != undefined) {
                currentNodeText += currentNodeLeft + " -->|Ошибка| " + this.nodeToGraphDefinition(node.RightNode) + '\r';
                needSetCurrentNodeText = false;
            }

            if (needSetCurrentNodeText)
                currentNodeText = currentNodeLeft;
        } else {
            const node = currentNode;

            currentNodeText = `${currentNodeLeft}[${currentNode.Step.title}]`

            if (node.hasOwnProperty('LeftNode') && node.LeftNode != undefined)
                currentNodeText += " --> " + this.nodeToGraphDefinition(node.LeftNode) + '\r';
        }

        if (currentNodeText[currentNodeText.length - 1] != '\r')
            currentNodeText += '\r';

        currentNodeText += `click ${currentNode.Id} OM.clickNode\r`;
        return currentNodeText;
    }

    async redrawNodes() {
        const root = this.getRootNode();
        const nodesDefinition = root == undefined ? "" : this.nodeToGraphDefinition(root);

        let graphDefinition = "flowchart TD\n" + nodesDefinition;
        await this.redrawGraph(graphDefinition);
    }

    private async redrawGraph(definition: string) {
        document.querySelector('.mermaid')?.remove()

        const graphBody = document.querySelector('#graphBody');

        const mermaidContainer = document.createElement('div');
        mermaidContainer.classList.add('mermaid');
        mermaidContainer.classList.add('text-center');
        mermaidContainer.textContent = definition;
        graphBody.appendChild(mermaidContainer);

        await Mermaid.run()
    }
}
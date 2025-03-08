import {EditStepDialog} from "./editStepDialog";
import {NodesGraphActionsDialog} from './nodesGraphActionsDialog'
import Mermaid from "mermaid";

export enum MaintenanceStepNodeKind {
    Simple,
    TryCatch
}

export class MaintenanceStep {
    id: string;
    taskId: string = '';
    kind: number = 0;
    nodeKind: MaintenanceStepNodeKind = MaintenanceStepNodeKind.Simple;
    previousStepId: string | null = null;
    leftStepId: string | null  = null;
    rightStepId: string | null  = null;
    accessCode: string = '';
    message: string = '';
    fileId: string | null  = null;
    title: string = '';
}

export class MaintenanceStepsTreeOptions {
    bodyElement: HTMLDivElement
    nodeGraphActionsElement: HTMLDivElement
    editStepModalElement: HTMLDivElement
    stepsInputElement: HTMLInputElement
}

let nodesGraph: MaintenanceStepsTree = undefined;

export async function initStepsEditor() {
    nodesGraph = new MaintenanceStepsTree({
        stepsInputElement: document.getElementById('steps') as HTMLInputElement,
        editStepModalElement: document.getElementById('edit-step-dialog') as HTMLDivElement,
        nodeGraphActionsElement: document.getElementById('node-actions') as HTMLDivElement,
        bodyElement: document.getElementById('graphBody') as HTMLDivElement
    });
}

export function clickNode(nodeId: string) {
    nodesGraph.handleNodeClick(nodeId);
}

export class MaintenanceStepsTree {
    private steps: Map<string, MaintenanceStep> = new Map();

    private options: MaintenanceStepsTreeOptions;
    private nodesGraphActionsDialog: NodesGraphActionsDialog
    private readonly editStepDialog: EditStepDialog

    constructor(options: MaintenanceStepsTreeOptions) {
        this.options = options;

        Mermaid.initialize({
            theme: 'dark',
            securityLevel: 'loose',
            startOnLoad: false,
            darkMode: true
        })

        this.editStepDialog = new EditStepDialog(this.options.editStepModalElement);
        this.nodesGraphActionsDialog = new NodesGraphActionsDialog(
            this.editStepDialog,
            this,
            this.options.nodeGraphActionsElement
        );

        this.options.bodyElement.addEventListener('click', async e => {
            if (this.steps.size == 0)
                this.nodesGraphActionsDialog.show();
        });
        
        this.inputValueToSteps();
        this.redrawNodes();
    }

    // Находим корневой узел (узел без previousStepId)
    public findRoot(): MaintenanceStep | null {
        for (const step of this.steps.values()) {
            if (!step.previousStepId) {
                return step;
            }
        }
        return null; // Если корневой узел не найден
    }
    
    public findStep(stepId: string): MaintenanceStep | null {
        if (this.steps.has(stepId))
            return this.steps.get(stepId);
        
        return null; // Если узел не найден
    }

    // Добавление нового узла
    public async addStep(newStep: MaintenanceStep, parentId: string | null, isLeft: boolean = true) {
        if (parentId === null) {
            // Если parentId === null, это корневой узел
            if (this.findRoot()) {
                throw new Error("Корневой узел уже существует.");
            }
            newStep.previousStepId = null;
        } else {
            // Проверяем, существует ли родительский узел
            if (!this.steps.has(parentId)) {
                throw new Error("Родительский узел не найден.");
            }
            const parent = this.steps.get(parentId)!;
            newStep.previousStepId = parentId;

            // Привязываем новый узел к родителю
            if (isLeft) {
                if (parent.leftStepId) {
                    throw new Error("Левый узел уже существует.");
                }
                parent.leftStepId = newStep.id;
            } else {
                if (parent.rightStepId) {
                    throw new Error("Правый узел уже существует.");
                }
                parent.rightStepId = newStep.id;
            }
        }

        // Добавляем новый узел в Map
        this.steps.set(newStep.id, newStep);

        this.stepsToInputValue();
        await this.redrawNodes();
    }
    
    public async updateStep(step: MaintenanceStep) {
        if (!this.steps.has(step.id)) {
            throw new Error("Узел не найден.");
        }
        
        this.steps.set(step.id, step);

        this.stepsToInputValue();
        await this.redrawNodes();
    }

    // Удаление узла и его подчиненных
    public async removeStep(stepId: string) {
        if (!this.steps.has(stepId)) {
            throw new Error("Узел не найден.");
        }

        // Рекурсивно удаляем подчиненные узлы
        const step = this.steps.get(stepId)!;
        if (step.leftStepId) {
            await this.removeStep(step.leftStepId);
        }
        if (step.rightStepId) {
            await this.removeStep(step.rightStepId);
        }

        // Удаляем ссылку на узел у родителя
        if (step.previousStepId) {
            const parent = this.steps.get(step.previousStepId)!;
            if (parent.leftStepId === stepId) {
                parent.leftStepId = null;
            } else if (parent.rightStepId === stepId) {
                parent.rightStepId = null;
            }
        }

        // Удаляем узел из Map
        this.steps.delete(stepId);

        this.stepsToInputValue();
        await this.redrawNodes();
    }

    public handleNodeClick(stepId: string) {
        this.nodesGraphActionsDialog.show(stepId);
    } 

    private nodeToGraphDefinition(currentNode: MaintenanceStep): string {
        let currentNodeText = "";

        let currentNodeLeft = currentNode.id;

        // is try/catch node
        if (currentNode.nodeKind == MaintenanceStepNodeKind.TryCatch) {
            const node = currentNode;

            currentNodeLeft = `${currentNodeLeft}{${currentNode.title}}`

            let needSetCurrentNodeText = true;

            if (node.leftStepId != null) {
                currentNodeText += currentNodeLeft + " -->|Успешно| " + this.nodeToGraphDefinition(this.steps.get(node.leftStepId)) + '\r';
                needSetCurrentNodeText = false;
            }

            if (node.rightStepId != null) {
                currentNodeText += currentNodeLeft + " -->|Ошибка| " + this.nodeToGraphDefinition(this.steps.get(node.rightStepId)) + '\r';
                needSetCurrentNodeText = false;
            }

            if (needSetCurrentNodeText)
                currentNodeText = currentNodeLeft;
        } else {
            const node = currentNode;

            currentNodeText = `${currentNodeLeft}(${currentNode.title})`

            if (node.leftStepId != null)
                currentNodeText += " --> " + this.nodeToGraphDefinition(this.steps.get(node.leftStepId)) + '\r';
        }

        if (currentNodeText[currentNodeText.length - 1] != '\r')
            currentNodeText += '\r';

        currentNodeText += `click ${currentNode.id} OM.clickNode\r`;
        return currentNodeText;
    }

    async redrawNodes() {
        const root = this.findRoot();
        const nodesDefinition = root == null ? "" : this.nodeToGraphDefinition(root);

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
    
    private stepsToInputValue() {
        this.options.stepsInputElement.value = JSON.stringify(Array.from(this.steps.values()));
    }

    private inputValueToSteps() {
        if (this.options.stepsInputElement.value?.length > 0)
            JSON.parse(this.options.stepsInputElement.value).forEach((step: MaintenanceStep) => {
                this.steps.set(step.id, step);
            });
    }
}
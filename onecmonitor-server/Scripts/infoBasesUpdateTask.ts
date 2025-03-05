import {MaintenanceStepsTree} from "./MaintenanceTask/maintenanceStepsTree";

let nodesGraph: MaintenanceStepsTree = undefined;

export async function initStepsEditor() {
    nodesGraph = new MaintenanceStepsTree({
        stepsInputElement: getSchemaNodesElement(),
        editStepModalElement: getStepEditDialogElement(),
        nodeGraphActionsElement: getNodeActionsElement(),
        bodyElement: getGraphBodyElement()
    });
}

export function clickNode(nodeId: string) {
    nodesGraph.handleNodeClick(nodeId);
}

function getNodeActionsElement(): HTMLDivElement {
    return document.getElementById('node-actions') as HTMLDivElement;
}

function getGraphBodyElement(): HTMLDivElement {
    return document.getElementById('graphBody') as HTMLDivElement;
}

function getSchemaNodesElement(): HTMLInputElement {
    return document.getElementById('steps') as HTMLInputElement;
}

function getStepEditDialogElement(): HTMLDivElement {
    return document.getElementById('edit-step-dialog') as HTMLDivElement;
}
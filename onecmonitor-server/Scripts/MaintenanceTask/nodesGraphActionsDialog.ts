import {EditStepDialog} from "./editStepDialog";
import {MaintenanceStepNodeKind, MaintenanceStepsTree} from "./maintenanceStepsTree";

export class NodesGraphActionsDialog {
    private editStepDialog: EditStepDialog;
    private tree: MaintenanceStepsTree;
    private dialogElement: HTMLDivElement;

    constructor(editStepDialog: EditStepDialog, tree: MaintenanceStepsTree, popupElement: HTMLDivElement) {
        this.editStepDialog = editStepDialog;
        this.dialogElement = popupElement;
        this.tree = tree;
    }

    public show(nodeId: string | null = null): void {
        const editingStep = nodeId == null ? null : this.tree.findStep(nodeId);
        
        const isFirstStep = nodeId == null;
        
        const editBtn = this.dialogElement.querySelector<HTMLButtonElement>('#edit-step-btn');
        this.showHideButton(editBtn, !isFirstStep);
        editBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            
            this.editStepDialog.open(async step => {
                await this.tree.updateStep(step);
            }, editingStep);
        }

        const deleteStepBtn = this.dialogElement.querySelector<HTMLButtonElement>('#delete-step-btn');
        this.showHideButton(deleteStepBtn, !isFirstStep);
        deleteStepBtn.onclick = async () => {
            this.dialogElement.hidePopover();

            await this.tree.removeStep(nodeId);
        }

        this.dialogElement.querySelector<HTMLButtonElement>('#add-step-btn').onclick = () => {
            this.dialogElement.hidePopover();
            this.addLeftStep(MaintenanceStepNodeKind.Simple, nodeId);
        }

        this.dialogElement.querySelector<HTMLButtonElement>('#add-binary-step-btn').onclick = () => {
            this.dialogElement.hidePopover();
            this.addLeftStep(MaintenanceStepNodeKind.TryCatch, nodeId);
        }

        const addErrorStepBtn = this.dialogElement.querySelector<HTMLButtonElement>('#add-error-step-btn');
        this.showHideButton(addErrorStepBtn, !isFirstStep && editingStep.nodeKind !== MaintenanceStepNodeKind.Simple);
        addErrorStepBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            this.addRightStep(MaintenanceStepNodeKind.Simple, nodeId);
        }

        const addErrorBinaryStepBtn = this.dialogElement.querySelector<HTMLButtonElement>('#add-error-binary-step-btn');
        this.showHideButton(addErrorBinaryStepBtn, !isFirstStep && editingStep.nodeKind !== MaintenanceStepNodeKind.Simple);
        addErrorBinaryStepBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            this.addRightStep(MaintenanceStepNodeKind.TryCatch, nodeId);
        }

        this.dialogElement.showPopover()
    }

    private showHideButton(button: HTMLButtonElement, value: boolean) {
        button.hidden = !value;
    }
    
    private addLeftStep(kind: MaintenanceStepNodeKind, nodeId: string | null = null) {
        this.editStepDialog.open(async step => {
            step.nodeKind = kind;
            await this.tree.addStep(step, nodeId, true);
        });
    }
    
    private addRightStep(kind: MaintenanceStepNodeKind, nodeId: string | null = null) {
        this.editStepDialog.open(async step => {
            step.nodeKind = kind;
            await this.tree.addStep(step, nodeId, false);
        });
    }
}
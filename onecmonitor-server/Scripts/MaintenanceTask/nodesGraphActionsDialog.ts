import {EditStepDialog} from "./editStepDialog";
import * as uuid from "uuid";
import {MaintenanceStepNodeKind} from "./maintenanceStepNodeKind";
import {StepsEditorHelper} from './stepsEditorHelper'

export class NodesGraphActionsDialog {
    private editStepDialog: EditStepDialog;
    private stepsEditorHelper: StepsEditorHelper;
    private dialogElement: HTMLDivElement;

    constructor(editStepDialog: EditStepDialog, stepsEditorHelper: StepsEditorHelper, popupElement: HTMLDivElement) {
        this.editStepDialog = editStepDialog;
        this.stepsEditorHelper = stepsEditorHelper;
        this.dialogElement = popupElement;
    }

    public show(nodeId: string | undefined = undefined): void {
        const root = this.stepsEditorHelper.getRootNode();
        const node = nodeId == undefined ? undefined : this.stepsEditorHelper.findNode(nodeId, root);
        
        const editBtn = this.dialogElement.querySelector<HTMLButtonElement>('#edit-step-btn');
        editBtn.hidden = nodeId == undefined;
        editBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            this.editStepDialog.open(step => {
                node.Step = step;
                node.StepId = step.id;
            }, node.Step);
        }

        const deleteStepBtn = this.dialogElement.querySelector<HTMLButtonElement>('#delete-step-btn');
        deleteStepBtn.hidden = nodeId == undefined;
        deleteStepBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            
            if (root.Id == node.Id)
                this.stepsEditorHelper.saveNode(undefined);
            else {
                this.stepsEditorHelper.deleteNode(root, node);
                this.stepsEditorHelper.saveNode(root);
            }

            await this.stepsEditorHelper.redrawNodes();
        }

        this.dialogElement.querySelector<HTMLButtonElement>('#add-step-btn').onclick = () => {
            this.dialogElement.hidePopover();
            this.addStep(MaintenanceStepNodeKind.Simple, nodeId);
        }

        this.dialogElement.querySelector<HTMLButtonElement>('#add-binary-step-btn').onclick = () => {
            this.dialogElement.hidePopover();
            this.addStep(MaintenanceStepNodeKind.TryCatch, nodeId);
        }

        const addErrorStepBtn = this.dialogElement.querySelector<HTMLButtonElement>('#add-error-step-btn');
        addErrorStepBtn.hidden = nodeId == undefined || node.Kind == MaintenanceStepNodeKind.Simple;
        addErrorStepBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            this.addCatchNode(root, node, MaintenanceStepNodeKind.Simple);
        }

        const addErrorBinaryStepBtn = this.dialogElement.querySelector<HTMLButtonElement>('#add-error-binary-step-btn');
        addErrorBinaryStepBtn.hidden = nodeId == undefined || node.Kind == MaintenanceStepNodeKind.Simple;
        addErrorBinaryStepBtn.onclick = async () => {
            this.dialogElement.hidePopover();
            this.addCatchNode(root, node, MaintenanceStepNodeKind.TryCatch);
        }

        this.dialogElement.showPopover()
    }
    
    private addStep(kind: MaintenanceStepNodeKind, nodeId: string | undefined = undefined) {
        if (nodeId == undefined)
            this.editStepDialog.open(async step => {
                await this.stepsEditorHelper.addRootNode({
                    Id: uuid.v4(),
                    Kind: kind,
                    Step: step,
                    StepId: step.id
                });
            });
        else
            this.editStepDialog.open(async step => {
                await this.stepsEditorHelper.addLeftNode(nodeId, {
                    Id: uuid.v4(),
                    Kind: kind,
                    Step: step,
                    StepId: step.id
                });
            });
    }
    
    private addCatchNode(root: any, parent: any, kind: MaintenanceStepNodeKind) {
        this.editStepDialog.open(async step => {
            await this.stepsEditorHelper.addRightNode(root, parent, {
                Id: uuid.v4(),
                Kind: kind,
                Step: step,
                StepId: step.id
            });
        });
    }
}
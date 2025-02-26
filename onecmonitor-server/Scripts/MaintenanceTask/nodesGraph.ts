import {EditStepDialog} from "./editStepDialog";
import {NodesGraphOptions} from "./nodesGraphOptions";
import {NodesGraphActionsDialog} from './nodesGraphActionsDialog'
import {StepsEditorHelper} from "./stepsEditorHelper";

export class NodesGraph {
    private nodesGraphActionsDialog: NodesGraphActionsDialog
    private editStepDialog: EditStepDialog
    private readonly stepsEditorHelper: StepsEditorHelper

    constructor(options: NodesGraphOptions) {

        this.stepsEditorHelper = new StepsEditorHelper(options.nodesInputElement);
        this.stepsEditorHelper.init().then(() => {
            this.editStepDialog = new EditStepDialog(options.editStepModalElement);
            this.nodesGraphActionsDialog = new NodesGraphActionsDialog(
                this.editStepDialog,
                this.stepsEditorHelper,
                options.nodeGraphActionsElement
            );

            options.bodyElement.addEventListener('click', async e => {
                if (options.nodesInputElement.value == undefined || options.nodesInputElement.value == "")
                    this.nodesGraphActionsDialog.show();
            });
        });
    }
    
    public handleNodeClick(nodeId: string) {
        this.nodesGraphActionsDialog.show(nodeId);
    }
}
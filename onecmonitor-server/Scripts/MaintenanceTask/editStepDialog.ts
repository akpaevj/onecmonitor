import {Modal} from "bootstrap";
import * as uuid from "uuid";
import {MaintenanceStep} from "./maintenanceStepsTree";

export class StepValidationResult {
    isValid: boolean;
    payload: any;
}

export class EditStepDialog {
    private dialogElement: HTMLDivElement;
    private dialogBody: HTMLDivElement;
    private modal: Modal;

    private onSaveCallback: (step: any) => void = undefined;

    constructor(element: HTMLDivElement) {
        this.dialogElement = element;
        this.modal = new Modal(element);
        this.dialogBody = this.dialogElement.querySelector('.modal-body');

        this.dialogElement.querySelector<HTMLButtonElement>('#save-btn').onclick = async () => {
            try {
                const result = await this.validateStepForm();

                if (result.isValid) {
                    this.close();
                    this.onSaveCallback(result.payload);
                } else {
                    this.dialogBody.innerHTML = result.payload;
                    this.setSelectKindEventHandler();
                }
            } catch (e) {
                alert(e);
            }
        };
    }

    public open(onSave: (step: MaintenanceStep) => void, step: MaintenanceStep | null = null) {
        this.onSaveCallback = onSave;
        if (step == null) {
            step = new MaintenanceStep();
            step.id = uuid.v4();
        }

        this.getEditStepForm(step).then(data => {
            this.dialogBody.innerHTML = data;

            this.setSelectKindEventHandler();
            this.modal.show();
        });
    }

    public close(): void {
        this.modal.hide();
    }
    
    private setSelectKindEventHandler() {
        const select = this.dialogBody.querySelector<HTMLSelectElement>("select[name='Kind']");
        select.onchange = async () => {
            await this.updateStepForm();
        };
    }

    private async updateStepForm() {
        const formData = new FormData(this.dialogBody.querySelector<HTMLFormElement>('form'));

        const response = await fetch('/MaintenanceTasks/UpdateStep', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            this.dialogBody.innerHTML = await response.text();

            const select = this.dialogBody.querySelector<HTMLSelectElement>("select[name='Kind']");
            select.onchange = async () => {
                await this.updateStepForm();
            };
        } else
            alert(`Failed to update step form: ${response.statusText}`);
    }

    private async getEditStepForm(step: any | undefined = undefined): Promise<string> {
        return new Promise<string | undefined>(async (resolve, reject) => {
            const body = step != undefined ? JSON.stringify(step) : "";

            const response = await fetch("/MaintenanceTasks/EditStep", {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: body,
            });

            if (response.ok)
                resolve(await response.text());
            else
                reject(`Failed to get step kind view: ${response.statusText}`);
        });
    }

    private async validateStepForm(): Promise<StepValidationResult> {
        return new Promise<StepValidationResult>(async (resolve, reject) => {
            const formData = new FormData(this.dialogBody.querySelector<HTMLFormElement>('form'));

            const validateResponse = await fetch('/MaintenanceTasks/ValidateStep', {
                method: 'POST',
                body: formData
            });

            if (validateResponse.status == 200) {
                const result = new StepValidationResult();
                result.isValid = true;
                result.payload = await validateResponse.json();

                resolve(result);
            } else if (validateResponse.status == 400) {
                const result = new StepValidationResult();
                result.isValid = false;
                result.payload = await validateResponse.text();

                resolve(result);
            } else
                reject(`Failed to validate step: ${validateResponse.statusText}`);
        });
    }
}
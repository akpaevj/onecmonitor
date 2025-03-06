import * as signalR from "@microsoft/signalr";
import * as signalRMsgPack from '@microsoft/signalr-protocol-msgpack'
import {HubConnectionState} from "@microsoft/signalr";
import {Tab} from "bootstrap";

class TaskLogItem {
    infoBase: InfoBase;
    log: MaintenanceStepLogItem[];
}

class InfoBase {
    id: string;
    name: string;
}

class MaintenanceStepLogItem {
    id: string;
    infoBaseId: string;
    timeStamp: string;
    isError: boolean;
    isFinish: boolean;
    message: string;
}

export async function initLog(taskId: string) {
    let selectedInfoBase = "";

    const tabButtons = document.querySelectorAll<HTMLButtonElement>('#infoBasesTab button');
    tabButtons.forEach(tabBtn => {
        const tabTrigger = new Tab(tabBtn);
        tabBtn.addEventListener('click', event => {
            event.preventDefault()
            tabTrigger.show();

            selectedInfoBase = tabBtn.dataset.infobaseId;
        })
    })

    const tabBtn = document.querySelector<HTMLButtonElement>('#infoBasesTab li:first-child button');
    Tab.getInstance(tabBtn).show();

    selectedInfoBase = tabBtn.dataset.infobaseId;
    
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/MaintenanceTaskLogs")
        .withAutomaticReconnect()
        .build();
    
    try {
        await connection.start();

        connection.on("HandleLogs", async (items: TaskLogItem[]) => {
            items.forEach(item => {
                const infoBaseMenuItem = document.querySelector(`#infoBasesTab li i[data-infobase-id='${item.infoBase.id}']`);

                if (infoBaseMenuItem != null) {
                    infoBaseMenuItem.classList.remove(infoBaseMenuItem.classList.item(infoBaseMenuItem.classList.length - 1));
                    infoBaseMenuItem.classList.add(getDotsStyle(item.log));
                }

                const logsDiv = document.querySelector(`.tab-pane[data-infobase-id='${selectedInfoBase}'] tbody`);
                if (logsDiv != null) {
                    item.log.forEach(i => {
                        const logItem = logsDiv.querySelector(`tr[data-log-item='${i.id}']`);
                        if (logItem == null) {
                            const newLogItem = document.createElement('tr');
                            newLogItem.dataset.logItem = i.id;
                            logsDiv.append(newLogItem);

                            const timeStamp = document.createElement('td');
                            timeStamp.textContent = i.timeStamp;
                            newLogItem.append(timeStamp);

                            const isError = document.createElement('td');
                            isError.textContent = i.isError.toString();
                            if (i.isError)
                                isError.classList.add('text-danger');
                            else
                                isError.classList.add('text-success');
                            newLogItem.append(isError);

                            const message = document.createElement('td');
                            message.textContent = i.message;
                            message.style.whiteSpace = 'pre-wrap';
                            newLogItem.append(message);
                        }
                    });
                }
            });
        });

        setInterval(async () => {
            if (connection.state != HubConnectionState.Connected)
                return;
            
            try {
                await connection.invoke('GetLogs', taskId);
            }
            catch (error) {
                console.log(error);
            }
        }, 2000);
    }
    catch (error) {
        console.error(error);
    }
}

function getDotsStyle(logs: MaintenanceStepLogItem[]) {
    let style = "text-danger";

    if (logs.length > 0)
        style = "text-warning";
    if (logs.find(c => c.isFinish) != undefined)
        style = "text-success";

    return style;
}
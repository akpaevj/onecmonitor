import {Tab} from "bootstrap";

export function initLog(taskId: string) {
    let selectedInfoBase = "";

    const getDotsStyle = (item: any) => {
        let style = "text-danger";

        if (item.started == true)
            style = "text-warning";
        if (item.finished == true)
            style = "text-success";

        return style;
    };

    setInterval(async () => {
        if (selectedInfoBase.length > 0) {
            try {
                const response = await fetch(`/UpdateInfoBaseTasks/LogState/${taskId}?infoBaseId=${selectedInfoBase}`);

                if (response.ok) {
                    const data = await response.json();

                    data.infoBases.forEach(i => {
                        const item = document.querySelector(`#infoBasesTab li i[data-infobase-id='${i.id}']`);
                        if (item != null) {
                            item.classList.remove(item.classList.item(item.classList.length - 1));
                            item.classList.add(getDotsStyle(i));
                        }
                    });

                    if (data.log.length > 0) {
                        const item = document.querySelector(`.tab-pane[data-infobase-id='${selectedInfoBase}'] tbody`);
                        if (item != null) {
                            data.log.forEach(i => {
                                const logItem = item.querySelector(`tr[data-log-item='${i.id}']`);
                                if (logItem == null) {
                                    const newLogItem = document.createElement('tr');
                                    newLogItem.dataset.logItem = i.id;
                                    item.append(newLogItem);

                                    const timeStamp = document.createElement('td');
                                    timeStamp.textContent = i.timeStamp;
                                    newLogItem.append(timeStamp);

                                    const isError = document.createElement('td');
                                    isError.textContent = i.isError;
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
                    }
                }
            }
            catch (error) {
                console.error(error);
            }
        }
    }, 3000);

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
}
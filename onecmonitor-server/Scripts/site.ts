// styles
import '../Styles/site.css';
import '../node_modules/vis-timeline/styles/vis-timeline-graph2d.min.css';
import '../node_modules/bootstrap/dist/css/bootstrap.min.css';
import '../node_modules/bootstrap-icons/font/bootstrap-icons.css';

// scripts
import '../node_modules/bootstrap/dist/js/bootstrap.bundle'
import * as ace from 'ace-builds';
import 'ace-builds/src-noconflict/ext-language_tools';
import 'ace-builds/src-noconflict/theme-twilight';
import 'ace-builds/src-noconflict/mode-xml';
import 'ace-builds/src-noconflict/mode-sql';

import { Timeline, DataItem, TimelineOptions, DataGroup } from 'vis-timeline';
import { CallGraphMember, LockWaitingMember, LockWaitingMemberType, TjEvent } from './models';
//import { Chart, ChartConfiguration, ChartDataset } from 'chart.js';
import * as vis from 'visjs-network';
import {Modal} from "bootstrap";

export function setIndexesOnSubmit(listIds: string[]) {
    listIds.forEach((listId: string) => {
        document.querySelectorAll(`#${listId} input`).forEach((el: HTMLInputElement, index: number) => {
            el.name = el.name.replace('[]', `[${index}]`);
        })
    })
}

export function showModal(id: string) {
    const element = document.getElementById(id);
    new Modal(element).show();
}

export function initSelectItemDialog(id: string) {
    const dialogId = `${id}-dialog`;
    const itemsListId = `${id}-list`;
    
    function hideShowCardBody() {
        const itemsList = document.getElementById(itemsListId);
        const body = itemsList.parentElement;

        body.hidden = itemsList.children.length == 0;
    }
    
    const form = document.querySelector('form');
    form.addEventListener('submit', (e) => {
        setIndexesOnSubmit([itemsListId]);
    })
    
    const input = document.querySelector<HTMLInputElement>(`#${dialogId} input[type="search"]`);
    input.onkeyup = _ => {
        const query = input.value.toUpperCase();
        
        document.querySelectorAll<HTMLButtonElement>(`#${dialogId} .list-group button`).forEach(item => {
            if (item.innerText.toUpperCase().includes(query))
                item.removeAttribute('hidden');
            else
                item.setAttribute('hidden', '');
        })
    }

    document.querySelectorAll<HTMLButtonElement>(`#${dialogId} .list-group button`).forEach((button: HTMLButtonElement) => {
        button.onclick = () => {
            selectItem(dialogId, itemsListId, button);
            hideShowCardBody();
        }
    })

    document.querySelectorAll<HTMLButtonElement>(`#${itemsListId} button`).forEach((button: HTMLButtonElement) => {
        button.onclick = () => {
            removeSelectedItem(dialogId, itemsListId, button);
            hideShowCardBody();
        }
    })

    hideShowCardBody();
}

export function selectItem(dialogId: string, itemsListId: string, button: HTMLButtonElement) {
    const list = document.getElementById(itemsListId) as HTMLDivElement;
    list.append(button);
    
    button.onclick = () => {
        removeSelectedItem(dialogId, itemsListId, button);
    };
    
    const modal = new Modal(document.getElementById(dialogId));
    modal.hide();
}

export function removeSelectedItem(dialogId: string, itemsListId: string, button: HTMLButtonElement) {
    const selectList = document.querySelector(`#${dialogId} .list-group`) as HTMLDivElement;
    selectList.append(button);

    button.onclick = () =>
        selectItem(dialogId, itemsListId, button);
}

export function openDeleteDialog(itemId: string, item: string, modalId: string = "deleteDialog") {
    const elem = document.getElementById(modalId);
    elem.innerHTML = elem.innerHTML.replace('{template_name}', item).replace('deleting_item_id', itemId);
    
    const modal = new Modal(elem);
    modal.show();
}

export function initAceEditor(editorContainer: Element, contentContainer: HTMLTextAreaElement, mode: string, heightToContent: Boolean = false) {
    let editor = ace.edit(editorContainer, {
        mode: `ace/mode/${mode}`,
        selectionStyle: "text",
        theme: 'ace/theme/twilight',
        enableLiveAutocompletion: true,
        autoScrollEditorIntoView: false
    });

    if (mode === 'sql') {
        editor.setOption('enableSnippets', true);
        let langTools = ace.require('ace/ext/language_tools');
        langTools.addCompleter(getClickHouseCompleter());
    }

    if (heightToContent) {
        editor.container.style.minHeight = 'inherit';
        editor.setOption('maxLines', 1);
        editor.setOption('showLineNumbers', false);
        editor.setOption('highlightActiveLine', false);
    }

    editor.setOptions({
        autoScrollEditorIntoView: true,
        copyWithEmptySelection: true
    });

    // set value from input to code editor
    editor.setValue(contentContainer.value);

    // set code editor value to input when it's being changed
    editor.on('change', function () {
        contentContainer.innerHTML = editor.getValue();
    });
}

function getClickHouseCompleter() {
    let properties = [
        'Id',
        'StartDateTime',
        'DateTime',
        'Duration',
        'EventName',
        'Level',
        'SessionId',
        'CallId',
        'TClientId',
        'TConnectId',
        'PProcessName',
        'WaitConnections',
        'Locks',
        'AgentId',
        'SeanceId',
        'Folder',
        'File',
        'EndPosition',
        'Properties[\'\']'
    ];

    return {
        getCompletions: function (editor, session, pos, prefix, callback) {
            callback(null, properties.map(function (name) {
                return {
                    value: name,
                    meta: "Property"
                };
            }));
        }
    };
}

export function initCallTimeline(chainStr: string) {
    let chain: Array<CallGraphMember> = JSON.parse(chainStr);

    let container = document.querySelector('#timeline-container') as HTMLCanvasElement;

    let options: TimelineOptions = {
        format: {
            minorLabels: {
                millisecond: 's.SSSSSS'
            }
        },
        editable: false,
        multiselect: true
    };

    let groups: DataGroup[] = new Array<DataGroup>();
    let items: DataItem[] = new Array<DataItem>();

    chain.forEach(value => {
        let groupId = "";
        let item: DataItem;

        if (value.IsCombinedEvent) {
            let start = new Date(value.CombinedEventStartDateTime + "Z");
            let end = new Date(value.CombinedEventFinishDateTime + "Z");
            groupId = value.CombinedEventGroup;

            item = {
                id: value.CombinedEvents[0],
                content: value.CombinedEventName,
                start: start,
                end: end,
                group: groupId
            };
        } else {
            let start = new Date(value.Event.start_date_time + "Z");
            let end = new Date(value.Event.date_time + "Z");
            groupId = value.Event.props['process'];

            item = {
                id: value.Event.id,
                content: value.Event.event_name,
                start: start,
                end: end,
                group: groupId
            };
        }

        if (groups.find(c => c.id == groupId) == undefined) {
            groups.push({
                id: groupId,
                content: groupId
            });
        }

        items.push(item);
    });

    const timeline = new Timeline(container, items, groups, options);
}

export function initLockWaitingGraph(graphStr: string) {
    let graph = new Map<string, LockWaitingMember>(Object.entries(JSON.parse(graphStr)));

    let container = document.querySelector('#graph-container') as HTMLElement;

    let nodes = [];
    let edges = [];

    let minNodeSize = 10;
    let maxNodeSize = 30;
    let minEventDuration = Number.MAX_VALUE;
    let maxEventDuration = 0;

    graph.forEach(c => {
        minEventDuration = Math.min(minEventDuration, c.Event.real_duration);
        maxEventDuration = Math.max(maxEventDuration, c.Event.real_duration);
    });

    graph.forEach(value => {
        let nodeColor = getLockWaitingNodeColor(value.MemberType);

        nodes.push({
            id: value.Event.id,
            label: value.Event.event_name,
            color: nodeColor,
            shape: 'circle',
            size: getSize(minNodeSize, maxNodeSize, minEventDuration, maxEventDuration, value.Event.real_duration)
        });

        value.DirectCulprits.forEach(culprit => {
            edges.push({ from: value.Event.id, to: culprit });
        });

        value.IndirectCulprits.forEach(culprit => {
            edges.push({ from: value.Event.id, to: culprit });
        });
    });

    let data = {
        nodes: nodes,
        edges: edges,
    };

    var options = {
        height: "400px"
    };

    var network = new vis.Network(container, data, options);
}

export function initLockWaitingTimeline(graphStr: string) {
    let graph = new Map<string, LockWaitingMember>(Object.entries(JSON.parse(graphStr)));

    let container = document.querySelector('#timeline-container') as HTMLElement;

    let options: TimelineOptions = {
        format: {
            minorLabels: {
                millisecond: 's.SSSSSS'
            }
        },
        editable: false,
        multiselect: true
    };

    let items: DataItem[] = new Array<DataItem>();

    graph.forEach(value => {
        let start = new Date(value.Event.start_date_time + "Z");
        let end = new Date(value.LockAffectEndDateTime + "Z");

        items.push({
            id: value.Event.id,
            content: `${value.Event.event_name} (${value.Event.t_connect_id})`,
            start: start,
            end: end,
            className: getLockWaitingMemberClass(value.MemberType)
        });
    });

    const timeline = new Timeline(container, items, options);

    timeline.on('select', properties => {
        let items: Array<string> = properties.items;
        document.querySelectorAll('[id^="details-"]').forEach(value => {
            if (items.includes(value.id.replace('details-', ''))) {
                value.removeAttribute('hidden');
            } else {
                value.setAttribute('hidden', '');
            }
        });
    });
}

function getLockWaitingMemberClass(type: LockWaitingMemberType) {
    if (type === LockWaitingMemberType.victim) {
        return 'om-victim-bg';
    } else if (type === LockWaitingMemberType.directCulprit) {
        return 'om-direct-culprit-bg';
    } else {
        return 'om-indirect-culprit-bg';
    }
}

function getLockWaitingNodeColor(type: LockWaitingMemberType) {
    if (type === LockWaitingMemberType.victim) {
        return '#ffb9b9';
    } else if (type === LockWaitingMemberType.directCulprit) {
        return '#b3d3ff';
    } else {
        return '#e2b2ff';
    }
}

function getSize(minValue: number, maxValue: number, minNatValue: number, maxNatValue: number, natValue: number): number {
    var valueRange = maxValue - minValue;
    var realValueRange = maxNatValue - minNatValue;
    let percents = (100 / realValueRange) * (natValue - minNatValue);

    return minValue + (valueRange * (percents / 100));
}